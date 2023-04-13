using System;
using System.Collections;
using System.Diagnostics;
using TensorFlowLite;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FILM
{

    /// <summary>
    /// FILM Controller, based on https://github.com/asus4/tf-lite-unity-sample/blob/master/Assets/Samples/SSD/SsdSample.cs
    /// </summary>

    public class FILM_Controller : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader compute = null;

        private ComputeBuffer outputBuffer;

        public Texture inputTexture1, inputTexture2;

        public RenderTexture OutputTexture;

        [SerializeField]
        private FILM.Options options = default;

        private FILM film;

        [SerializeField]
        Text elapsedTimeText;

        public class FilmOperationCompleteEvent : UnityEvent<FILM_Output> {}

        public FilmOperationCompleteEvent OnFilmOperationComplete = new FilmOperationCompleteEvent();

        private void Awake()
        {
            film = new FILM(options);
            outputBuffer = new ComputeBuffer(options.inputShape[0] * options.inputShape[1], sizeof(float) * options.inputShape[2]);

            //Invoke(inputTexture1, inputTexture2, 1);
        }

        private void OnDestroy()
        {
            film?.Dispose();
            outputBuffer?.Dispose();
        }

        public void Invoke(Texture texture1, Texture texture2, int interpCoeff)
        {
            int width = options.inputShape[0];
            int height = options.inputShape[1];
            //int channels = options.inputShape[2];

            if (width != texture2.width || height != texture2.height)
            {
                throw new Exception("Image shapes do not match the provided shape.");
            }

            FILM_Output filmOutput = new FILM_Output(interpCoeff);
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (interpCoeff == 1)
                {
                    var middle = PerformFILM(texture1, texture2);
                    filmOutput.Set(0, texture1);
                    filmOutput.Set(1, middle);
                    filmOutput.Set(2, texture2);
                }
                else if(interpCoeff > 1)
                {
                    PerformFILMRecursively(texture1, texture2, ref filmOutput, 0, filmOutput.size - 1, 0);
                }

                stopwatch.Stop();

                string elapsedTimeTextContent = stopwatch.Elapsed.ToString(@"mm\:ss\.fff");
                elapsedTimeText.text = $"Elapsed time: {elapsedTimeTextContent} - mm:s.ms";
            }
            OnFilmOperationComplete.Invoke(filmOutput);
        }

        private RenderTexture PerformFILM(Texture texture1, Texture texture2)
        {
            film.Invoke(texture1, texture2);
            FILM.Result result = film.GetResult();
            RenderTexture output_rt = new RenderTexture(texture1.width, texture1.height, 3, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            output_rt.enableRandomWrite = true;
            output_rt.Create();
            outputBuffer.SetData(result.data);
            compute.SetBuffer(0, "InputTensor", outputBuffer);
            compute.SetTexture(0, "OutputTexture", output_rt);

            compute.Dispatch(0, texture1.width / 8, texture1.height / 8, 1);

            return output_rt;

            //Graphics.Blit(output_rt, OutputTexture);
            //output_rt.Release();
        }

        private void PerformFILMRecursively(Texture texture1, Texture texture2, ref FILM_Output filmOutput, int lIndex, int rIndex, int depth)
        {
            if(depth == filmOutput.interpCoeff)
            {
                filmOutput.Set(lIndex, texture1);
                filmOutput.Set(rIndex, texture2);
                return;
            }
            var middle = PerformFILM(texture1, texture2); // .ToRenderTexture();
            var middleIndex = (lIndex + rIndex) / 2;

            PerformFILMRecursively(texture1, middle, ref filmOutput, lIndex, middleIndex, depth + 1);
            PerformFILMRecursively(middle, texture2, ref filmOutput, middleIndex, rIndex, depth + 1);
        }
    }

    public class FILM_Output
    {
        RenderTexture[] outputs;
        public int interpCoeff;
        public int size;

        public RenderTexture this[int i]
        {
            get { return outputs[i]; }
        }

        public FILM_Output(int timesToInterpolate)
        {
            this.interpCoeff = timesToInterpolate;
            size = 0;

            if (timesToInterpolate <= 0)
            {
                throw new Exception("Size parameter should be larger than 0.");
            }
            //if (timesToInterpolate == 1)
            //{
            //    size = 3;
            //}
            //else if (timesToInterpolate > 1)
            //{
                size = (int)Mathf.Pow(2, timesToInterpolate) + 1;
            //}

            outputs = new RenderTexture[size];
        }

        public void Set(int index, Texture value)
        {
            Set(index, value.ToRenderTexture());
        }

        public void Set(int index, RenderTexture value)
        {
            if (outputs == null)
            {
                throw new Exception("Not properly initialized.");
            }

            if (index < 0 || index >= outputs.Length)
            {
                throw new Exception("Index out of bounds.");
            }

            outputs[index] = value;
        }
        ~FILM_Output()
        {
            foreach (var output in outputs)
            {
                output.Release();
            }
        }
    }

    public static class TextureExtensions
    {
        public static RenderTexture ToRenderTexture(this Texture texture)
        {
            RenderTexture newRT = new RenderTexture(texture.width, texture.height, 3, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, newRT);
            return newRT;
        }

        public static RenderTexture ToRenderTexture(this RenderTexture texture)
        {
            RenderTexture newRT = new RenderTexture(texture.width, texture.height, 3, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, newRT);
            return newRT;
        }
    }
}
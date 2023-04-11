using System;
using System.Diagnostics;
using TensorFlowLite;
using UnityEngine;
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

            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                film.Invoke(texture1, texture2);

                stopwatch.Stop();

                string elapsedTimeTextContent = stopwatch.Elapsed.ToString(@"ss\.fff");
                elapsedTimeText.text = $"Elapsed time: {elapsedTimeTextContent} - s.ms";
            }

            FILM.Result result = film.GetResult();

            RenderTexture output_rt = new RenderTexture(width, height, 3, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            output_rt.enableRandomWrite = true;
            output_rt.Create();
            outputBuffer.SetData(result.data);
            compute.SetBuffer(0, "InputTensor", outputBuffer);
            compute.SetTexture(0, "OutputTexture", output_rt);

            compute.Dispatch(0, width / 8, height / 8, 1);

            Graphics.Blit(output_rt, OutputTexture);
            output_rt.Release();
        }
    }
}
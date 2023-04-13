using System;
using TensorFlowLite;
using UnityEngine;

namespace FILM
{
    /// <summary>
    /// FILM predictor, based on https://github.com/asus4/tf-lite-unity-sample/blob/master/Assets/Samples/SSD/SSD.cs
    /// See https://film-net.github.io/
    /// </summary>
    public class FILM : FILM_Interpolator<float>
    {
        [System.Serializable]
        public class Options
        {
            [FilePopup("*.tflite")]
            public string modelPath = string.Empty;
            public AspectMode aspectMode = AspectMode.Fit;
            public Accelerator accelerator = Accelerator.GPU;

            public int[] inputShape = new int[] { 512, 512, 3 };
        }

        public struct Result
        {
            public float[,,] data;

            public Result(float[,,] output)
            {
                data = output;
            }
        }

        public FILM(Options options)
            : base(options.modelPath, options.accelerator, options.inputShape)
        {
            resizeOptions.aspectMode = options.aspectMode;
        }

        public override void Invoke(Texture texture1, Texture texture2, float time = 0.5f)
        {
            if(texture1 == null || texture2 == null)
            {
                throw new Exception("One of the given textures is null.");
            }

            if (texture1.width != texture2.width || texture1.height != texture2.height)
            {
                throw new Exception("Image shapes should be equal.");
            }

            inputTensorTime = new float[1, 1] { { time } };
            Console.WriteLine("toTensor1");
            ToTensor(texture1, inputTensorImage1);
            Console.WriteLine("toTensor2");
            ToTensor(texture2, inputTensorImage2);
            Console.WriteLine("End");

            interpreter.SetInputTensorData(0, inputTensorTime);
            interpreter.SetInputTensorData(1, inputTensorImage1);
            interpreter.SetInputTensorData(2, inputTensorImage2);

            interpreter.Invoke();

            outputTensorImage = new float[width, height, 3];
            interpreter.GetOutputTensorData(1, outputTensorImage);
        }

        public Result GetResult()
        {
            return new Result(outputTensorImage);
        }
    }
}
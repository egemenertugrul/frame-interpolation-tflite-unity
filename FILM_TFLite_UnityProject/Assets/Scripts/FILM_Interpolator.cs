using System.Threading;
using TensorFlowLite;
using UnityEngine;

#if TFLITE_UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif // TFLITE_UNITASK_ENABLED

namespace FILM
{
    public abstract class FILM_Interpolator<T> : System.IDisposable
        where T : struct
    {
        /// <summary>
        /// Accelerator options
        /// </summary>
        public enum Accelerator
        {
            NONE = 0,
            NNAPI = 1,
            GPU = 2,
            // HEXAGON = 3,
            XNNPACK = 4,
            // The EdgeTpu in Pixel devices.
            // EDGETPU = 5,
            // The Coral EdgeTpu Dev Board / USB accelerator.
            // EDGETPU_CORAL = 6,
        }

        protected readonly Interpreter interpreter;
        protected readonly int width;
        protected readonly int height;
        protected readonly int channels;
        protected float[,] inputTensorTime;
        protected readonly T[,,] inputTensorImage1, inputTensorImage2;
        protected T[,,] outputTensorImage;

        protected readonly TextureToTensor tex2tensor;
        protected readonly TextureResizer resizer;
        protected TextureResizer.ResizeOptions resizeOptions;

        public Texture inputTex
        {
            get
            {
                return (tex2tensor.texture != null)
                    ? tex2tensor.texture as Texture
                    : resizer.texture as Texture;
            }
        }
        public Material transformMat => resizer.material;

        public TextureResizer.ResizeOptions ResizeOptions
        {
            get => resizeOptions;
            set => resizeOptions = value;
        }

        public FILM_Interpolator(string modelPath, Accelerator accelerator, int[] imageShape)
        {
            var options = new InterpreterOptions();

            switch (accelerator)
            {
                case Accelerator.NONE:
                    options.threads = SystemInfo.processorCount;
                    break;
                case Accelerator.NNAPI:
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        options.useNNAPI = true;
                        //#if UNITY_ANDROID && !UNITY_EDITOR
                        //string cacheDir = Application.persistentDataPath;
                        //string modelToken = "film-token";
                        //var nnapiOptions = NNAPIDelegate.DefaultOptions;
                        //nnapiOptions.AllowFp16 = true;
                        //nnapiOptions.CacheDir = cacheDir;
                        //nnapiOptions.ModelToken = modelToken;
                        //options.AddDelegate(new NNAPIDelegate(nnapiOptions));
                        //#endif
                    }
                    else
                    {
                        Debug.LogError("NNAPI is only supported on Android");
                    }
                    break;
                case Accelerator.GPU:
                    options.AddGpuDelegate();
                    break;
                case Accelerator.XNNPACK:
                    options.threads = SystemInfo.processorCount;
                    options.AddDelegate(XNNPackDelegate.DelegateForType(typeof(T)));
                    break;
                default:
                    options.Dispose();
                    throw new System.NotImplementedException();
            }

            try
            {
                interpreter = new Interpreter(FileUtil.LoadFile(modelPath), options);
            }
            catch (System.Exception e)
            {
                interpreter?.Dispose();
                throw e;
            }

            interpreter.LogIOInfo();
            // Initialize inputs
            {
                height = imageShape[0];
                width = imageShape[1];
                //channels = imageShape[2];

                inputTensorTime = new float[1, 1];
                inputTensorImage1 = new T[width, height, 3];
                inputTensorImage2 = new T[width, height, 3];

                interpreter.ResizeInputTensor(1, new int[] { 1, width, height, 3 });
                interpreter.ResizeInputTensor(2, new int[] { 1, width, height, 3 });

                interpreter.AllocateTensors();
            }

            tex2tensor = new TextureToTensor();
            resizer = new TextureResizer();
            resizeOptions = new TextureResizer.ResizeOptions()
            {
                aspectMode = AspectMode.Fill,
                rotationDegree = 0,
                mirrorHorizontal = false,
                mirrorVertical = false,
                width = width,
                height = height,
            };
        }

        public virtual void Dispose()
        {
            interpreter?.Dispose();
            tex2tensor?.Dispose();
            resizer?.Dispose();
        }

        public abstract void Invoke(Texture texture1, Texture texture2, float time);

        protected void ToTensor(Texture inputTex, float[,,] inputs)
        {
            RenderTexture tex = resizer.Resize(inputTex, resizeOptions);
            tex2tensor.ToTensor(tex, inputs);
        }

        protected void ToTensor(RenderTexture inputTex, float[,,] inputs, bool resize)
        {
            RenderTexture tex = resize ? resizer.Resize(inputTex, resizeOptions) : inputTex;
            tex2tensor.ToTensor(tex, inputs);
        }

        protected void ToTensor(Texture inputTex, float[,,] inputs, float offset, float scale)
        {
            RenderTexture tex = resizer.Resize(inputTex, resizeOptions);
            tex2tensor.ToTensor(tex, inputs, offset, scale);
        }

        protected void ToTensor(Texture inputTex, int[,,] inputs)
        {
            RenderTexture tex = resizer.Resize(inputTex, resizeOptions);
            tex2tensor.ToTensor(tex, inputs);
        }

        // ToTensorAsync methods are only available when UniTask is installed via Unity Package Manager.
        // TODO: consider using native Task or Unity Coroutine
#if TFLITE_UNITASK_ENABLED
        protected async UniTask<bool> ToTensorAsync(Texture inputTex, float[,,] inputs, CancellationToken cancellationToken)
        {
            RenderTexture tex = resizer.Resize(inputTex, resizeOptions);
            await tex2tensor.ToTensorAsync(tex, inputs, cancellationToken);
            return true;
        }

        protected async UniTask<bool> ToTensorAsync(RenderTexture inputTex, float[,,] inputs, bool resize, CancellationToken cancellationToken)
        {
            RenderTexture tex = resize ? resizer.Resize(inputTex, resizeOptions) : inputTex;
            await tex2tensor.ToTensorAsync(tex, inputs, cancellationToken);
            return true;
        }

        protected async UniTask<bool> ToTensorAsync(Texture inputTex, sbyte[,,] inputs, CancellationToken cancellationToken)
        {
            RenderTexture tex = resizer.Resize(inputTex, resizeOptions);
            await tex2tensor.ToTensorAsync(tex, inputs, cancellationToken);
            return true;
        }

        protected async UniTask<bool> ToTensorAsync(Texture inputTex, int[,,] inputs, CancellationToken cancellationToken)
        {
            RenderTexture tex = resizer.Resize(inputTex, resizeOptions);
            await tex2tensor.ToTensorAsync(tex, inputs, cancellationToken);
            return true;
        }
#endif // TFLITE_UNITASK_ENABLED
    }
}

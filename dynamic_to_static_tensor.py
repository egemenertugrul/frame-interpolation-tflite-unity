import os

import tensorflow as tf

if __name__ == '__main__':
    ## Save the model with 512,512,3 as input shape
    loaded_model = tf.keras.models.load_model("pretrained_models/film_net/Style/saved_model/")

    image_shape = (1, 512, 512, 3) # (None, 512, 512, 3)
    time_shape = (1, 1) # (None, 512, 512, 3)
    loaded_model.input['x0'].set_shape(image_shape)
    loaded_model.input['x1'].set_shape(image_shape)
    loaded_model.input['time'].set_shape(time_shape)
    loaded_model.compile()

    path = "film_net_fixed"
    isExist = os.path.exists(path)
    if not isExist:
        os.makedirs(path)

    fixed_model_name = f"fixed_{image_shape[1]}_{image_shape[2]}_{image_shape[3]}"
    fixed_model_path = os.path.join(path, fixed_model_name)
    loaded_model.save(fixed_model_path)

    ## Then convert to tflite:
    converter = tf.lite.TFLiteConverter.from_saved_model(fixed_model_path)
    converter.optimizations = [tf.lite.Optimize.DEFAULT]
    converter.target_spec.supported_ops = [
      tf.lite.OpsSet.TFLITE_BUILTINS,
      tf.lite.OpsSet.SELECT_TF_OPS
    ]
    # converter.target_spec.supported_types = [tf.float16]

    # converter.inference_input_type = tf.float32
    # converter.inference_output_type = tf.float32

    tflite_model = converter.convert()

    # # Save the model.
    tflite_path = os.path.join(path, f'{fixed_model_name}_fixed.tflite')
    with open(tflite_path, 'wb') as f:
      f.write(tflite_model)

# for onnx (Not working): python -m tf2onnx.convert --tflite model_512.tflite --output model_512_inputs.onnx --inputs-as-nchw serving_default_x1:0,serving_default_x0:0_2 --inputs serving_default_x1:0[1,512,512,3],serving_default_x0:0[1,512,512,3],serving_default_time:0[1,1]

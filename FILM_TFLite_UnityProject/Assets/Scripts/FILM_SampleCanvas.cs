using FILM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FILM_SampleCanvas : MonoBehaviour
{
    public FILM_Controller filmController;

    private int interpCoeff = 1;

    public Button runButton;
    public Slider timesToInterpolateSlider, outputIndexSlider;
    public Image Input1, Input2;
    public RawImage OutputImage;
    public Text timesToInterpolateText, outputIndexText;
    private FILM_Output filmOutputs;

    void Start()
    {
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        yield return new WaitForEndOfFrame();

        var tex1 = filmController.inputTexture1;
        var tex2 = filmController.inputTexture2;

        Input1.sprite = Sprite.Create(CreateTexture2D(tex1), new Rect(0.0f, 0.0f, tex1.width, tex1.height), new Vector2(0.5f, 0.5f), 100.0f);
        Input2.sprite = Sprite.Create(CreateTexture2D(tex2), new Rect(0.0f, 0.0f, tex2.width, tex2.height), new Vector2(0.5f, 0.5f), 100.0f);
        OutputImage.texture = null;

        timesToInterpolateText.text = ((int)timesToInterpolateSlider.value).ToString();
        timesToInterpolateSlider.onValueChanged.AddListener((val) =>
        {
            interpCoeff = ((int)val);
            timesToInterpolateText.text = interpCoeff.ToString();
        });

        runButton.onClick.AddListener(() => {
            //OutputImage.texture = filmController.OutputTexture;
            filmController.Invoke(tex1, tex2, interpCoeff);
        });

        outputIndexSlider.onValueChanged.AddListener((val) => {
            OutputImage.texture = filmOutputs?[(int)val];
        });

        filmController.OnFilmOperationComplete.AddListener((filmOutputs) => {
            outputIndexSlider.wholeNumbers = true;
            outputIndexSlider.minValue = 0;
            outputIndexSlider.maxValue = filmOutputs.size - 1;
            outputIndexSlider.SetValueWithoutNotify(0);

            this.filmOutputs = filmOutputs;
            OutputImage.texture = filmOutputs[0];

        });
    }

    private Texture2D CreateTexture2D(Texture texture)
    {
        return Texture2D.CreateExternalTexture(
                         texture.width,
                         texture.height,
                         TextureFormat.RGB24,
                         false, false,
                         texture.GetNativeTexturePtr());
    }

    private void OnDestroy()
    {
        OutputImage.texture = null;
    }
}

using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class LUI_BrightnessAdjustment : MonoBehaviour {
	
	public Slider Brightness;
	float brightnessValue;

	public void bAdj (float brightnessValue)
	{
		brightnessValue = Brightness.value;
		//rgbValue = GUI.HorizontalSlider (new Rect (Screen.width / 2 - 50, 90, 100, 30), rgbValue, 0f, 1.0f);
		//RenderSettings.ambientLight = brightnessValue;
		RenderSettings.ambientLight = new Color (brightnessValue, brightnessValue, brightnessValue, 1);
	}
}

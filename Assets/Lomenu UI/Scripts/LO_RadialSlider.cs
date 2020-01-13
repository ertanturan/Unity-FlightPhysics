using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LO_RadialSlider : MonoBehaviour {

    [Header("OBJECTS")]
    public Image sliderImage;
    public Slider baseSlider;

    private float currentValue = 0;

    void Update ()
    {
      //  baseSlider.value = currentValue / 100;
        currentValue = baseSlider.value / 1;
       // currentValue = Mathf.Round(sliderImage.fillAmount * 100) / 1f;
        sliderImage.fillAmount = currentValue;
    }
}

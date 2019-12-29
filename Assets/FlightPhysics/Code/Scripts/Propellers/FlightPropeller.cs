using UnityEngine;

public class FlightPropeller : MonoBehaviour
{

    [Header("Propeller Properties")]
    public float MinQuadRPMs = 300f;

    private float _minRotationRPM = 40f;
    public float MinSwapRPM = 600f;

    public GameObject MainProperty;
    public GameObject BlurredProperty;

    public Texture2D SmoothBlur;
    public Texture2D HardBlur;

    private Renderer _propellerRenderer;

    private void Awake()
    {
        _propellerRenderer = BlurredProperty.GetComponent<Renderer>();
    }

    private void Start()
    {
        HandleSwapping(0);
    }

    public void HandlePropeller(float currentRPM)
    {
        //Degrees Per Second = (RPM*360)/60
        float dps = ((currentRPM * 360) / 60) * Time.deltaTime;

        dps = Mathf.Clamp(dps, 25f, _minRotationRPM);

        transform.Rotate(Vector3.forward, dps);

        HandleSwapping(currentRPM);

    }

    private void HandleSwapping(float currentRPM)
    {
        if (BlurredProperty && MainProperty && SmoothBlur && HardBlur)
        {
            if (currentRPM > MinQuadRPMs && currentRPM < MinSwapRPM)
            {
                BlurredProperty.SetActive(true);
                MainProperty.SetActive(false);
                _propellerRenderer.material.SetTexture("_MainTex", SmoothBlur);
            }
            else if (currentRPM > MinSwapRPM)
            {
                BlurredProperty.SetActive(true);
                MainProperty.SetActive(false);
                _propellerRenderer.material.SetTexture("_MainTex", HardBlur);
            }
            else
            {
                BlurredProperty.SetActive(false);
                MainProperty.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("Some properties are missing !. " +
                             "Swapping propeller's visuals may not work correctly !" +
                             "Check all the variables attached and re-run the game .");
        }
    }

}

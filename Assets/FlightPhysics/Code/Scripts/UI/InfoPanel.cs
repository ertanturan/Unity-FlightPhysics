using FlightPhysics.Input;
using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI Pitch;
    public TextMeshProUGUI Yaw;
    public TextMeshProUGUI Roll;
    public TextMeshProUGUI Throttle;
    public TextMeshProUGUI StickyThrottle;
    public TextMeshProUGUI Flaps;
    public TextMeshProUGUI Brake;

    [Header("Input")]
    public BaseFlightInput Input;

    private void Update()
    {
        Pitch.text = Input.Pitch.ToString();
        Yaw.text = Input.Yaw.ToString();
        Roll.text = Input.Roll.ToString();
        Throttle.text = Input.Throttle.ToString();
        StickyThrottle.text = Input.StickyThrottle.ToString();
        Flaps.text = Input.Flaps.ToString();
        Brake.text = Input.Brake.ToString();
    }
}

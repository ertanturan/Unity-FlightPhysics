using UnityEngine;


[CreateAssetMenu(fileName = "New Flight Character", menuName = "Ertan/Data/Flight/Create New Flight Character")]
public class FlightCharacteristic : ScriptableObject
{
    [Header("Characteristics")]
    public float MaxMPS = 200f; //200 mps is for f4u corsair only (718 km/h )

    [Header("Lift")]
    public float MaxLiftPower = 800f;
    public float FlapLiftPower = 100f;

    [Header("Drag")]
    public float DragFactor = .01f; // how much drag do we add as we go faster and faster
    public float FlapDragFactor = .005f;

    [Header("Controls")]
    public float PitchSpeed = 100f;
    public float RollSpeed = 100f;
    public float YawSpeed = 100f;
    public float BankingSpeed = 100f;
}

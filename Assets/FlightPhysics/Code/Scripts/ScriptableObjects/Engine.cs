using UnityEngine;

[CreateAssetMenu(fileName = "New Engine", menuName = "Ertan/Data/Engine/Create New Engine", order = 1)]
public class Engine : ScriptableObject
{
    public float MaxForce = 200f;
    public float MaxRPM = 2550f;
    public float ShutOffSpeed = 2f;
    public AnimationCurve PowerCurve = AnimationCurve.Linear(0f,
        0f, 1f, 1f);
}

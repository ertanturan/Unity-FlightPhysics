using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 Euler;
    public float Angle;
    void Update()
    {
        transform.Rotate(Euler, Angle);
    }
}

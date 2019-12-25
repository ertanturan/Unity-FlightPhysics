using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AircraftFuel : MonoBehaviour
{
    [Header("Fuel Properties")]
    public float FuelCapacity = 26f;
    public float FuelBurnRate = 6.1f;

    private float _currentFuel;

    public float CurrentFuel
    {
        get { return _currentFuel; }
    }

    private float _normalizedFuel;

    public float NormalizedFuel
    {
        get { return _normalizedFuel; }
    }

    public void InitFuel()
    {
        _currentFuel = FuelCapacity;
    }

    public void UpdateFuel(float percentage)
    {
        percentage = Mathf.Clamp(percentage, 0.05f, 1);
        float currentBurn = ((FuelBurnRate * percentage) / 3600) * Time.deltaTime;
        _currentFuel -= currentBurn;

        _currentFuel = Mathf.Clamp(_currentFuel, 0f, FuelCapacity);

        _normalizedFuel = CurrentFuel / FuelCapacity;
    }
}

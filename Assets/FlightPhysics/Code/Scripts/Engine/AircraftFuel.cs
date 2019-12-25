using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AircraftFuel : MonoBehaviour
{
    #region Fields

    [Header("Fuel Properties")]
    public float FuelCapacity = 26f;
    public float FuelBurnRate = 6.1f;

    [Header("Events")]
    public UnityEvent OnFuelFull = new UnityEvent();

    private float _currentFuel;
    private float _normalizedFuel;

    #endregion

    #region Properties

    public float CurrentFuel
    {
        get { return _currentFuel; }
        private set { _currentFuel = value; }
    }


    #endregion

    #region Custom Methods

    public float NormalizedFuel
    {
        get { return _normalizedFuel; }
    }

    public void InitFuel()
    {
        _currentFuel = FuelCapacity;
    }

    public void AddFuel(float amount)
    {
        if (CurrentFuel == FuelCapacity)
        {
            OnFuelFull.Invoke();
            Debug.LogWarning("Fuel is full !. Can't add more.");
        }
        else
        {
            CurrentFuel += amount;
        }

        CurrentFuel = Mathf.Clamp(CurrentFuel, 0f, FuelCapacity);
    }

    public void ResetFuel()
    {
        CurrentFuel = FuelCapacity;
    }

    public void UpdateFuel(float percentage)
    {
        percentage = Mathf.Clamp(percentage, 0.05f, 1);
        float currentBurn = ((FuelBurnRate * percentage) / 3600) * Time.deltaTime;
        _currentFuel -= currentBurn;

        _currentFuel = Mathf.Clamp(_currentFuel, 0f, FuelCapacity);

        _normalizedFuel = CurrentFuel / FuelCapacity;
    }

    #endregion

}

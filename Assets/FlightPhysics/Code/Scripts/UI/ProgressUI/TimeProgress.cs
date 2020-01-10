using System.Diagnostics;
using TMPro;
using UnityEngine;

namespace FlightPhysics.UI
{
    public class TimeProgress : MonoBehaviour
    {
        private Stopwatch _sw = new Stopwatch();
        private TextMeshProUGUI _txtElapsed;

        private void Awake()
        {
            _txtElapsed = GetComponentsInChildren<TextMeshProUGUI>()[1];
        }

        private void Start()
        {
            _sw.Start();
        }

        private void Update()
        {
            _txtElapsed.text = _sw.Elapsed.Minutes.ToString("00") + ":"
                + _sw.Elapsed.TotalSeconds.ToString("00");
        }
    }

}


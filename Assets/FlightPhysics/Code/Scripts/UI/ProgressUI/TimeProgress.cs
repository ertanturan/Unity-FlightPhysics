using System.Diagnostics;

namespace FlightPhysics.UI
{
    public class TimeProgress : ProgressTrackerBase
    {
        private Stopwatch _sw = new Stopwatch();

        private void Start()
        {
            _sw.Start();
        }

        private void Update()
        {
            SetValues(_sw.Elapsed.Minutes.ToString(), _sw.Elapsed.TotalSeconds.ToString("0.00"));
        }
    }

}


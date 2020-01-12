using FlightPhysics.Gameplay;

namespace FlightPhysics.UI
{
    public class GateProgress : ProgressTrackerBase
    {
        public void HandleGateUI()
        {

            ProgressManager.Instance.GateP.SetValues(TrackManager.Instance.CurrentGateIndex.ToString()
                , TrackManager.Instance.TotalGates.ToString());
        }
    }
}

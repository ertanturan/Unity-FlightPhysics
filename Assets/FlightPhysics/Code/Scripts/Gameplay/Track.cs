using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


namespace FlightPhysics.Gameplay
{
    public class Track : MonoBehaviour
    {


        public int CurrentGateIndex
        {
            get { return _currrentGateIndex; }
        }

        public Gate[] Gates
        {
            get { return _gates.ToArray(); }
        }

        private List<Gate> _gates = new List<Gate>();
        private int _currrentGateIndex = 0;

        [Header("Track Events")]
        public UnityEvent OnTrackCompleted = new UnityEvent();
        public bool IsCompleted = false;

        private void Awake()
        {
            FindGates();
            InitGates();

            StartTrack();
        }

        private void InitGates()
        {
            if (_gates.Count > 0)
            {
                foreach (Gate gate in _gates)
                {
                    gate.DeActivateGate();
                    gate.OnGateCleared.AddListener(SelectNextGate);
                }
            }
        }


        private void FindGates()
        {
            _gates.Clear();
            _gates = ReturnGates();
        }

        private List<Gate> ReturnGates()
        {
            return GetComponentsInChildren<Gate>(true).ToList();
        }

        private void SelectNextGate()
        {
            _currrentGateIndex++;
            Debug.Log(_currrentGateIndex + " " + _gates.Count);
            if (_currrentGateIndex == _gates.Count)
            {
                //Track finished.
                if (OnTrackCompleted != null)
                {
                    OnTrackCompleted.Invoke();
                    IsCompleted = true;
                }
            }
            else
            {

                StartTrack();
            }

        }

        public void StartTrack()
        {
            if (_gates.Count > 0)
            {
                _gates[_currrentGateIndex].ActivateGate();
            }
        }
    }
}

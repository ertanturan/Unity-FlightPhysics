using FlightPhysics.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FlightPhysics.Gameplay
{
    public class TrackManager : Singleton<TrackManager>
    {
        [Header("Manager Properties")]
        public List<Track> Tracks = new List<Track>();
        public FlightController Controller;
        [Header("Manager Events")]
        public UnityEvent OnCompletedRace = new UnityEvent();

        public int CurrentTrackIndex
        {
            get { return _currentTrackIndex; }
        }

        private int _currentTrackIndex = 0;
        public int CurrentGateIndex
        {
            get
            {
                int currentIndex = 0;
                for (int i = 0; i < Tracks.Count; i++)
                {
                    currentIndex += Tracks[i].CurrentGateIndex;
                }

                return currentIndex;
            }
        }

        public int TotalGates
        {
            get
            {
                int gatesCount = 0;
                for (int i = 0; i < Tracks.Count; i++)
                {
                    for (int j = 0; j < Tracks[i].Gates.Length; j++)
                    {
                        gatesCount++;
                    }
                }

                return gatesCount;
            }
        }

        private void Start()
        {
            FindTracks();
            InitializeTracks();
            StartTrack(0);


            for (int i = 0; i < Tracks.Count; i++)
            {
                for (int j = 0; j < Tracks[i].Gates.Length; j++)
                {

                    Tracks[i].Gates[j].OnGateCleared.AddListener(
                        delegate
                        {
                            ProgressManager.Instance.GateP.HandleGateUI();
                        }

                    );
                }
            }

        }

        private void StartTrack(int trackID)
        {
            if (trackID >= 0 && trackID < Tracks.Count)
            {
                for (int i = 0; i < Tracks.Count; i++)
                {
                    if (i != trackID)
                    {
                        Tracks[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        Tracks[i].gameObject.SetActive(true);
                        Tracks[i].StartTrack();
                    }
                    ProgressManager.Instance.TrackP.SetValues((trackID + 1).ToString()
                        , Tracks.Count.ToString());


                }

                ProgressManager.Instance.GateP.SetValues(0.ToString(),
                    TrackManager.Instance.TotalGates.ToString());
            }
        }

        private void FindTracks()
        {
            Tracks.Clear();
            Tracks = GetComponentsInChildren<Track>(true).ToList();

        }

        private void InitializeTracks()
        {
            if (Tracks.Count > 0)
            {
                foreach (Track track in Tracks)
                {
                    track.OnTrackCompleted.AddListener(TrackCompleted);
                }
            }
        }

        private void TrackCompleted()
        {
            if (Controller)
            {
                if (IsAllTracksCompleted())
                {
                    StartCoroutine(WaitForLanding());
                    Debug.Log("Tracks completed");
                    Debug.Log("Waiting for landing");

                }
                else
                {
                    _currentTrackIndex++;
                    StartTrack(_currentTrackIndex);
                }
            }
        }

        private bool IsAllTracksCompleted()
        {
            int trackCompleted = 1;

            foreach (Track track in Tracks)
            {
                if (track.IsCompleted)
                {
                    trackCompleted++;
                }

                if (trackCompleted == Tracks.Count)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator WaitForLanding()
        {
            while (Controller.State != PlaneState.LANDED)
            {
                yield return null;
            }

            if (OnCompletedRace != null)
            {
                OnCompletedRace.Invoke();
            }
        }

    }
}

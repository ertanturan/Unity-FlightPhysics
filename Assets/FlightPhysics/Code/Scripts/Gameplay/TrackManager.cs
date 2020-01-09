using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FlightPhysics.Gameplay
{
    public class TrackManager : MonoBehaviour
    {
        [Header("Manager Properties")]
        public List<Track> Tracks = new List<Track>();
        public FlightController Controller;
        [Header("Manager Events")]
        public UnityEvent OnCompletedRace = new UnityEvent();

        private void Start()
        {
            FindTracks();
            InitializeTracks();
            StartTrack(0);
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
                    Tracks[i].gameObject.SetActive(true);
                    Tracks[i].StartTrack();
                }
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
                StartCoroutine(WaitForLanding());
                Debug.Log("Tracks completed");
                Debug.Log("Waiting for landing");
            }
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

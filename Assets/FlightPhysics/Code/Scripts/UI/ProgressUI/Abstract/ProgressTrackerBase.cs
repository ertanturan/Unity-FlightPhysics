using TMPro;
using UnityEngine;

namespace FlightPhysics.UI
{
    public class ProgressTrackerBase : MonoBehaviour
    {
        private TextMeshProUGUI Current;
        private TextMeshProUGUI Total;

        public virtual void Awake()
        {
            TextMeshProUGUI[] children = GetComponentsInChildren<TextMeshProUGUI>();
            Current = children[1]; Total = children[3];
        }

        public virtual void SetValues(string current, string total)
        {
            Current.text = current; Total.text = total;
        }
    }
}
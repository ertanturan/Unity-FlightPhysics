using TMPro;
using UnityEngine;

public class ProgressTrackerBase : MonoBehaviour
{
    private TextMeshProUGUI Current;
    private TextMeshProUGUI Total;

    public virtual void Awake()
    {
        TextMeshProUGUI[] children = GetComponentsInChildren<TextMeshProUGUI>();
        Current = children[0]; Total = children[2];
    }

    public virtual void SetValues(string current, string total)
    {
        Current.text = current; Total.text = total;
    }
}

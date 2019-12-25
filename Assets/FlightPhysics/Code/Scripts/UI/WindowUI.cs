using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Animator))]
public class WindowUI : MonoBehaviour
{

    [SerializeField]
    protected Animator animator;

    public bool IsShown
    {
        get
        {
            return animator.GetBool("Shown");
        }
        set
        {
            if (value != IsShown)
            {
                if (value)
                    onShowing.Invoke();
                else
                    onHiding.Invoke();
            }
            animator.SetBool("Shown", value);
        }
    }

    public bool startHidden = true;

    public UnityEvent onShowing;
    public UnityEvent onHiding;

    protected virtual void Start()
    {
        if (!startHidden)
            Show();
    }

    public void Show()
    {
        IsShown = true;
    }

    public void Hide()
    {
        IsShown = false;
    }
}

using UnityEngine;

public class LUI_PUBG_Button : MonoBehaviour {

    [Header("ANIMATOR")]
    public Animator buttonAnimator;

    public void HoverButton()
    {
        if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("TP Open"))
        {
            // do nothing because it's clicked
        }

        else
        {
            buttonAnimator.Play("TP Hover");
        }
    }

    public void NormalizeButton()
    {
        if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("TP Open"))
        {
            // do nothing because it's clicked
        }

        else
        {
            buttonAnimator.Play("TP Normalize");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LUI_UIAnimManager : MonoBehaviour {

	[Header("ANIMATORS")]
	public Animator oldAnimator;
	public Animator newAnimator;

	[Header("OBJECTS")]
	public Button animButton;
	public GameObject newPanel;

	[Header("ANIM NAMES")]
	public string oldAnimText;
	public string newAnimText;

	void Start ()
	{
		oldAnimator.GetComponent<Animator>();
		newAnimator.GetComponent<Animator>();
		Button btn = animButton.GetComponent<Button>();
		animButton.onClick.AddListener(TaskOnClick);
	}

	void TaskOnClick()
	{
		newPanel.SetActive(true);
		oldAnimator.Play(oldAnimText);
		newAnimator.Play(newAnimText);
	}
}
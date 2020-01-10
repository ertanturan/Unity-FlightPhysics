using UnityEngine;
using System.Collections;

public class LUI_PUBG : MonoBehaviour {

    [Header("BUTTON ANIMATORS")]
    public Animator playButton;
    public Animator customizeButton;
    public Animator rewardsButton;
    public Animator careerButton;
    public Animator leaderButton;
    public Animator replaysButton;

    [Header("PANEL ANIMATORS")]
    public Animator playPanel;
    public Animator customizePanel;
    public Animator rewardsPanel;
    public Animator careerPanel;
    public Animator leaderPanel;
    public Animator replaysPanel;
    public Animator settingsPanel;

    [Header("OBJECTS")]
    public GameObject particles;

    [Header("SETTINGS")]
    public CurrentPanel currentPanel;

    bool isSettingsOpen = false;

    public enum CurrentPanel
    {
        PLAY,
        CUSTOMIZE,
        REWARDS,
        CAREER,
        LEADER,
        REPLAYS
    }

    void Start ()
    {
        // Click Play button at start
        PlayClick();
        playButton.Play("TP Open");
    }

	public void PlayClick ()
    {
        if (currentPanel == CurrentPanel.CUSTOMIZE)
        {
            customizePanel.Play("Panel Close");
            playPanel.Play("Panel Open");

            customizeButton.Play("TP Close");
            playButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REWARDS)
        {
            rewardsPanel.Play("Panel Close");
            playPanel.Play("Panel Open");

            rewardsButton.Play("TP Close");
            playButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CAREER)
        {
            careerPanel.Play("Panel Close");
            playPanel.Play("Panel Open");

            careerButton.Play("TP Close");
            playButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.LEADER)
        {
            leaderPanel.Play("Panel Close");
            playPanel.Play("Panel Open");

            leaderButton.Play("TP Close");
            playButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REPLAYS)
        {
            replaysPanel.Play("Panel Close");
            playPanel.Play("Panel Open");

            replaysButton.Play("TP Close");
            playButton.Play("TP Open");
        }
        currentPanel = CurrentPanel.PLAY;
        particles.SetActive(false);
    }

    public void CustomizeClick()
    {
        if (currentPanel == CurrentPanel.PLAY)
        {
            playPanel.Play("Panel Close");
            customizePanel.Play("Panel Open");

            playButton.Play("TP Close");
            customizeButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REWARDS)
        {
            rewardsPanel.Play("Panel Close");
            customizePanel.Play("Panel Open");

            rewardsButton.Play("TP Close");
            customizeButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CAREER)
        {
            careerPanel.Play("Panel Close");
            customizePanel.Play("Panel Open");

            careerButton.Play("TP Close");
            customizeButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.LEADER)
        {
            leaderPanel.Play("Panel Close");
            customizePanel.Play("Panel Open");

            leaderButton.Play("TP Close");
            customizeButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REPLAYS)
        {
            replaysPanel.Play("Panel Close");
            customizePanel.Play("Panel Open");

            replaysButton.Play("TP Close");
            customizeButton.Play("TP Open");
        }
        currentPanel = CurrentPanel.CUSTOMIZE;
        particles.SetActive(true);
    }

    public void RewardsClick()
    {
        if (currentPanel == CurrentPanel.PLAY)
        {
            playPanel.Play("Panel Close");
            rewardsPanel.Play("Panel Open");

            playButton.Play("TP Close");
            rewardsButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CUSTOMIZE)
        {
            customizePanel.Play("Panel Close");
            rewardsPanel.Play("Panel Open");

            customizeButton.Play("TP Close");
            rewardsButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CAREER)
        {
            careerPanel.Play("Panel Close");
            rewardsPanel.Play("Panel Open");

            careerButton.Play("TP Close");
            rewardsButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.LEADER)
        {
            leaderPanel.Play("Panel Close");
            rewardsPanel.Play("Panel Open");

            leaderButton.Play("TP Close");
            rewardsButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REPLAYS)
        {
            replaysPanel.Play("Panel Close");
            rewardsPanel.Play("Panel Open");

            replaysButton.Play("TP Close");
            rewardsButton.Play("TP Open");
        }
        currentPanel = CurrentPanel.REWARDS;
        particles.SetActive(false);
    }

    public void CareerClick()
    {
        if (currentPanel == CurrentPanel.PLAY)
        {
            playPanel.Play("Panel Close");
            careerPanel.Play("Panel Open");

            playButton.Play("TP Close");
            careerButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CUSTOMIZE)
        {
            customizePanel.Play("Panel Close");
            careerPanel.Play("Panel Open");

            customizeButton.Play("TP Close");
            careerButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REWARDS)
        {
            rewardsPanel.Play("Panel Close");
            careerPanel.Play("Panel Open");

            rewardsButton.Play("TP Close");
            careerButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.LEADER)
        {
            leaderPanel.Play("Panel Close");
            careerPanel.Play("Panel Open");

            leaderButton.Play("TP Close");
            careerButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REPLAYS)
        {
            replaysPanel.Play("Panel Close");
            careerPanel.Play("Panel Open");

            replaysButton.Play("TP Close");
            careerButton.Play("TP Open");
        }
        currentPanel = CurrentPanel.CAREER;
        particles.SetActive(false);
    }

    public void LeaderClick()
    {
        if (currentPanel == CurrentPanel.PLAY)
        {
            playPanel.Play("Panel Close");
            leaderPanel.Play("Panel Open");

            playButton.Play("TP Close");
            leaderButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CUSTOMIZE)
        {
            customizePanel.Play("Panel Close");
            leaderPanel.Play("Panel Open");

            customizeButton.Play("TP Close");
            leaderButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REWARDS)
        {
            rewardsPanel.Play("Panel Close");
            leaderPanel.Play("Panel Open");

            rewardsButton.Play("TP Close");
            leaderButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CAREER)
        {
            careerPanel.Play("Panel Close");
            leaderPanel.Play("Panel Open");

            careerButton.Play("TP Close");
            leaderButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REPLAYS)
        {
            replaysPanel.Play("Panel Close");
            leaderPanel.Play("Panel Open");

            replaysButton.Play("TP Close");
            leaderButton.Play("TP Open");
        }
        currentPanel = CurrentPanel.LEADER;
        particles.SetActive(false);
    }

    public void ReplaysClick()
    {
        if (currentPanel == CurrentPanel.PLAY)
        {
            playPanel.Play("Panel Close");
            replaysPanel.Play("Panel Open");

            playButton.Play("TP Close");
            replaysButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CUSTOMIZE)
        {
            customizePanel.Play("Panel Close");
            replaysPanel.Play("Panel Open");

            customizeButton.Play("TP Close");
            replaysButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.REWARDS)
        {
            rewardsPanel.Play("Panel Close");
            replaysPanel.Play("Panel Open");

            rewardsButton.Play("TP Close");
            replaysButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.CAREER)
        {
            careerPanel.Play("Panel Close");
            replaysPanel.Play("Panel Open");

            careerButton.Play("TP Close");
            replaysButton.Play("TP Open");
        }

        else if (currentPanel == CurrentPanel.LEADER)
        {
            leaderPanel.Play("Panel Close");
            replaysPanel.Play("Panel Open");

            leaderButton.Play("TP Close");
            replaysButton.Play("TP Open");
        }
        currentPanel = CurrentPanel.REPLAYS;
        particles.SetActive(false);
    }

    public void SettingsClick()
    {
        if (isSettingsOpen == false)
        {
            settingsPanel.Play("Panel Open");
            isSettingsOpen = true;
        }

        else
        {
            settingsPanel.Play("Panel Close");
            isSettingsOpen = false;
        }
    }
}

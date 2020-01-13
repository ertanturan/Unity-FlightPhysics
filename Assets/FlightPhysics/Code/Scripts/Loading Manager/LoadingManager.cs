using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public Text PercentageText;

    public void LoadFirstLevel()
    {
        StartCoroutine(LoadAsync());
    }

    private IEnumerator LoadAsync()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);

        while (load.progress != 1)
        {
            PercentageText.text = (Mathf.InverseLerp(0f, 0.9f, load.progress) * 10) + "%";
            yield return null;

        }
    }


}

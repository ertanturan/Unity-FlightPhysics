using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public Text PercentageText;

    public void LoadFirstLevel(int sceneIndex)
    {
        StartCoroutine(LoadAsync(sceneIndex));
    }

    private IEnumerator LoadAsync(int sceneIndex)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single);

        while (load.progress != 1)
        {
            PercentageText.text = (Mathf.InverseLerp(0f, 0.9f, load.progress) * 10) + "%";
            yield return null;

        }
    }


}

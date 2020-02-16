using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    [Header("Singletion Properties")]
    public bool DontDestroyOnLoading = false;

    public static T Instance
    {
        get
        {

            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    obj.AddComponent<T>();
                }
            }

            return _instance;

        }
    }

    private static T _instance;

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            _instance = this as T;
            if (DontDestroyOnLoading)
                DontDestroyOnLoad(gameObject);

        }
        else
        {
            if (Instance.GetInstanceID() != this.GetInstanceID())
            {
                Destroy(gameObject);
            }
        }
    }

}
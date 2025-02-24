using UnityEngine;

public abstract class SingletonMonoBehavior<T> : MonoBehaviour where T :SingletonMonoBehavior<T>
{
    private static T _instance;
    [SerializeField] protected bool _DDOL = false;
    
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();

                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        if(_DDOL)
            DontDestroyOnLoad(gameObject);
    }
}

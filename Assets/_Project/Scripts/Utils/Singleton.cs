using UnityEngine;
/// <summary>
/// Generic base class for MonoBehaviour singletons.
/// </summary>
/// <typeparam name="T">The type of the singleton class.</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    private static readonly object SyncRoot = new object();
    private static bool applicationIsQuitting = false;
    protected virtual bool PersistAcrossScenes => false;
    protected virtual bool DetachFromParentWhenPersistent => true;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton<{typeof(T)}>] Instance won't be returned because the app is quitting.");
                return null;
            }

            if (instance != null)
            {
                return instance;
            }

            lock (SyncRoot)
            {
                if (instance == null)
                {
                    T[] instances = Object.FindObjectsByType<T>(FindObjectsSortMode.None);

                    if (instances.Length > 1)
                    {
                        Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Multiple instances detected. Using the first one found.");
                    }

                    instance = instances.Length > 0 ? instances[0] : null;
                }

                return instance;
            }
        }
    }

    protected virtual void Awake()
    {
        T current = this as T;
        if (current == null)
        {
            Debug.LogError($"[Singleton<{typeof(T).Name}>] '{GetType().Name}' does not match singleton type.");
            return;
        }

        if (instance != null && instance != current)
        {
            Destroy(gameObject);
            return;
        }

        instance = current;
        applicationIsQuitting = false;

        if (!PersistAcrossScenes || !Application.isPlaying)
        {
            return;
        }

        if (DetachFromParentWhenPersistent && transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (instance == this as T)
        {
            instance = null;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}

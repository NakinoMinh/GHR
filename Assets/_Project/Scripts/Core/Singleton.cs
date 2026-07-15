using UnityEngine;

namespace GanhHangRong.Core
{
    /// <summary>
    /// Lớp cơ sở Singleton generic cho MonoBehaviour.
    /// Sử dụng: public class MyManager : Singleton<MyManager> { }
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        [Header("Singleton Settings")]
        [SerializeField] protected bool dontDestroyOnLoad = true;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' đã bị hủy khi thoát. Trả về null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();

                        if (_instance == null)
                        {
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[{typeof(T).Name}]";
                            Debug.Log($"[Singleton] Đã tạo instance mới: {typeof(T).Name}");
                        }
                    }
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            // Enter Play Mode can keep static fields when domain reload is disabled.
            _applicationIsQuitting = false;

            if (_instance == null)
            {
                _instance = this as T;
                if (dontDestroyOnLoad)
                {
                    if (transform.parent != null)
                        transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
                OnSingletonAwake();
            }
            else if (_instance != this)
            {
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                bool isSharedHost = false;
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] != null && behaviours[i] != this)
                    {
                        isSharedHost = true;
                        break;
                    }
                }

                // Scene manager hosts contain several unrelated systems. Removing the
                // whole host here would silently destroy the rest of the gameplay loop.
                if (isSharedHost)
                {
                    Debug.Log($"[Singleton] Duplicate {typeof(T).Name} bị loại khỏi host dùng chung {gameObject.name}");
                    Destroy(this);
                }
                else
                {
                    Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} bị hủy trên {gameObject.name}");
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Gọi khi singleton được khởi tạo lần đầu. Override thay vì Awake().
        /// </summary>
        protected virtual void OnSingletonAwake() { }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}

using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Monitor ligero de FPS y modelo de dispositivo activo unicamente en Development Builds
    /// para registrar metricas de rendimiento en Android (I-43).
    /// </summary>
    public class AndroidPerformanceMonitor : MonoBehaviour
    {
#if DEVELOPMENT_BUILD
        const float SampleIntervalSeconds = 0.5f;
        const float WarmupSeconds = 1.5f;

        float _windowTime;
        int _windowFrames;
        float _currentFps;
        float _totalTime;
        int _totalFrames;
        float _minFps = float.MaxValue;
        float _sceneStartedAt;
        string _cachedLabel = "";
        GUIStyle _boxStyle;
        GUIStyle _textStyle;
        Texture2D _bgTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindFirstObjectByType<AndroidPerformanceMonitor>() != null) return;
            var go = new GameObject("AndroidPerformanceMonitor");
            DontDestroyOnLoad(go);
            go.AddComponent<AndroidPerformanceMonitor>();
        }

        void Awake()
        {
            _sceneStartedAt = Time.unscaledTime;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_bgTex != null) Destroy(_bgTex);
        }

        void OnSceneLoaded(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.LoadSceneMode __)
        {
            _sceneStartedAt = Time.unscaledTime;
            _totalTime = 0f;
            _totalFrames = 0;
            _minFps = float.MaxValue;
            _windowTime = 0f;
            _windowFrames = 0;
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f || dt > 0.5f) return;

            _windowTime += dt;
            _windowFrames++;

            if (Time.unscaledTime - _sceneStartedAt >= WarmupSeconds)
            {
                _totalTime += dt;
                _totalFrames++;
            }

            if (_windowTime < SampleIntervalSeconds) return;

            _currentFps = _windowFrames / _windowTime;
            if (Time.unscaledTime - _sceneStartedAt >= WarmupSeconds && _currentFps > 0f && _currentFps < _minFps)
                _minFps = _currentFps;

            float avgFps = _totalTime > 0f ? _totalFrames / _totalTime : _currentFps;
            float minDisplay = _minFps < float.MaxValue ? _minFps : _currentFps;
            _cachedLabel = $"FPS: {_currentFps:F0} (avg {avgFps:F0} / min {minDisplay:F0}) | {SystemInfo.deviceModel} | {Screen.width}x{Screen.height}";

            _windowTime = 0f;
            _windowFrames = 0;
        }

        void EnsureStyles()
        {
            if (_boxStyle != null && _textStyle != null) return;
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0.08f, 0.06f, 0.04f, 0.72f));
            _bgTex.Apply();

            int fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 14, 28);
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 6, 6)
            };
            _boxStyle.normal.background = _bgTex;

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft
            };
            _textStyle.normal.textColor = _currentFps >= 30f || _currentFps <= 0f
                ? new Color(0.92f, 0.85f, 0.71f, 1f)
                : new Color(1f, 0.45f, 0.35f, 1f);
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(_cachedLabel)) return;
            EnsureStyles();
            _textStyle.normal.textColor = _currentFps >= 30f
                ? new Color(0.92f, 0.85f, 0.71f, 1f)
                : new Color(1f, 0.45f, 0.35f, 1f);

            GUIContent content = new GUIContent(_cachedLabel);
            Vector2 size = _textStyle.CalcSize(content);
            Rect safe = Screen.safeArea;
            float x = safe.xMin + 12f;
            float y = Screen.height - safe.yMax + 12f;
            Rect boxRect = new Rect(x, y, size.x + 20f, size.y + 12f);
            GUI.Box(boxRect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(boxRect.x + 10f, boxRect.y + 6f, size.x, size.y), content, _textStyle);
        }
#endif
    }
}

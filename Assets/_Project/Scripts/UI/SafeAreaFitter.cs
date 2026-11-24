using UnityEngine;

namespace Reloj.UI
{
    /// <summary>
    /// Ajusta este RectTransform al Screen.safeArea para respetar notches, esquinas redondeadas
    /// y barras de gestos en Android (16:9, 20:9, rotacion y Device Simulator).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rectTransform;
        Rect lastSafeArea;
        int lastWidth;
        int lastHeight;
        ScreenOrientation lastOrientation;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            Apply(force: true);
        }

        void Update()
        {
            Apply(force: false);
        }

        void Apply(bool force)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            Rect safe = UnityEngine.Device.Screen.safeArea;
            int w = UnityEngine.Device.Screen.width;
            int h = UnityEngine.Device.Screen.height;
            ScreenOrientation orientation = UnityEngine.Device.Screen.orientation;

            if (!force
                && safe == lastSafeArea
                && w == lastWidth
                && h == lastHeight
                && orientation == lastOrientation)
            {
                return;
            }

            lastSafeArea = safe;
            lastWidth = w;
            lastHeight = h;
            lastOrientation = orientation;

            if (w <= 0 || h <= 0) return;

            Vector2 min = new Vector2(Mathf.Clamp01(safe.xMin / w), Mathf.Clamp01(safe.yMin / h));
            Vector2 max = new Vector2(Mathf.Clamp01(safe.xMax / w), Mathf.Clamp01(safe.yMax / h));

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}

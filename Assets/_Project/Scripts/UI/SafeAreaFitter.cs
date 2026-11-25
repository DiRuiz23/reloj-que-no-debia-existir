using UnityEngine;

namespace Reloj.UI
{
    /// <summary>
    /// Ajusta este RectTransform al Screen.safeArea para respetar notches, esquinas redondeadas
    /// y barras de gestos en Android (16:9, 20:9, rotacion y Device Simulator).
    /// Va en el panel raiz ("SafeArea") de cada Canvas: todo el contenido importante cuelga de el.
    /// Los fondos y fundidos a pantalla completa se quedan fuera, directos bajo el Canvas.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Lados que respetan el area segura")]
        [SerializeField] bool left = true;
        [SerializeField] bool right = true;
        [SerializeField] bool top = true;
        [SerializeField] bool bottom = true;

        [Header("Compensacion de escala (opcional)")]
        [Tooltip("Hijo que contiene el contenido. Solo se usa si 'contentScale' es distinto de 1.")]
        [SerializeField] RectTransform content;
        [Tooltip("Para interfaces maquetadas en 1280x720 sobre un Canvas de 1920x1080: 1.5 mantiene su tamano visual.")]
        [SerializeField, Min(0.1f)] float contentScale = 1f;

        /// <summary>
        /// SOLO PRUEBAS: area segura simulada, normalizada (0..1). Sustituye a Screen.safeArea mientras no sea null.
        /// Sirve para comprobar un notch en el Game view, donde el area segura siempre es la pantalla completa.
        /// </summary>
        public static Rect? DebugNormalizedSafeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { DebugNormalizedSafeArea = null; }

        RectTransform rectTransform;
        Vector2 lastMin, lastMax, lastCanvasSize;
        bool hasLast;

        void Awake() { rectTransform = GetComponent<RectTransform>(); }

        void OnEnable() { Apply(true); }

        void Update() { Apply(false); }

        bool applying;
        void OnRectTransformDimensionsChange()
        {
            if (applying || !isActiveAndEnabled) return;
            applying = true;
            try { Apply(false); } finally { applying = false; }
        }

        /// <summary>Recalcula ahora mismo (lo usan tambien las herramientas del Editor).</summary>
        public void Apply(bool force)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            int w = UnityEngine.Device.Screen.width;
            int h = UnityEngine.Device.Screen.height;
            if (w <= 0 || h <= 0) return;

            Vector2 min, max;
            if (DebugNormalizedSafeArea.HasValue)
            {
                var d = DebugNormalizedSafeArea.Value;
                min = new Vector2(Mathf.Clamp01(d.xMin), Mathf.Clamp01(d.yMin));
                max = new Vector2(Mathf.Clamp01(d.xMax), Mathf.Clamp01(d.yMax));
            }
            else
            {
                Rect safe = UnityEngine.Device.Screen.safeArea;
                min = new Vector2(Mathf.Clamp01(safe.xMin / w), Mathf.Clamp01(safe.yMin / h));
                max = new Vector2(Mathf.Clamp01(safe.xMax / w), Mathf.Clamp01(safe.yMax / h));
            }
            // Si ya hay un SafeAreaFitter arriba (p. ej. el SafeArea del Canvas), este no vuelve a recortar.
            if (transform.parent != null && transform.parent.GetComponentInParent<SafeAreaFitter>() != null)
            {
                min = Vector2.zero; max = Vector2.one;
            }
            else
            {
                if (!left) min.x = 0f;
                if (!right) max.x = 1f;
                if (!bottom) min.y = 0f;
                if (!top) max.y = 1f;
            }

            var canvas = GetComponentInParent<Canvas>();
            Vector2 canvasSize = canvas != null && canvas.rootCanvas.transform is RectTransform crt ? crt.rect.size : Vector2.zero;

            if (!force && hasLast && min == lastMin && max == lastMax && canvasSize == lastCanvasSize) return;
            lastMin = min; lastMax = max; lastCanvasSize = canvasSize; hasLast = true;

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            if (content != null && !Mathf.Approximately(contentScale, 1f) && canvasSize.x > 0f && canvasSize.y > 0f)
            {
                // El contenido se maqueto para un canvas mas chico: se dibuja a 'contentScale' y su rectangulo
                // se reduce en la misma proporcion para que siga ocupando exactamente el area segura.
                Vector2 size = new Vector2(canvasSize.x * (max.x - min.x), canvasSize.y * (max.y - min.y));
                content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = size / contentScale;
                content.localScale = new Vector3(contentScale, contentScale, 1f);
            }
        }

        /// <summary>Lo usa la herramienta del Editor al armar la jerarquia.</summary>
        public void Configure(RectTransform contentRoot, float scale)
        {
            content = contentRoot;
            contentScale = Mathf.Max(0.1f, scale);
        }
    }
}

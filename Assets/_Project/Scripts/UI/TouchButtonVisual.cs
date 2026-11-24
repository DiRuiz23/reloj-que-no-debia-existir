using UnityEngine;
using UnityEngine.EventSystems;

namespace Reloj.UI
{
    /// <summary>
    /// Feedback tactil visual al presionar un boton en pantalla (escala suave sin interferir con OnScreenButton).
    /// </summary>
    public class TouchButtonVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] RectTransform targetRect;
        [SerializeField, Range(0.75f, 1f)] float pressedScale = 0.92f;
        [SerializeField, Min(1f)] float lerpSpeed = 18f;

        Vector3 targetScale = Vector3.one;

        void Awake()
        {
            if (targetRect == null) targetRect = transform as RectTransform;
        }

        void OnDisable()
        {
            targetScale = Vector3.one;
            if (targetRect != null) targetRect.localScale = Vector3.one;
        }

        void Update()
        {
            if (targetRect == null) return;
            if ((targetRect.localScale - targetScale).sqrMagnitude > 0.0001f)
            {
                targetRect.localScale = Vector3.Lerp(
                    targetRect.localScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = Vector3.one * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = Vector3.one;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Reloj.UI
{
    /// <summary>
    /// Pantalla de créditos. Genera el texto desde <see cref="CreditsData"/> y permite desplazarlo con
    /// rueda y arrastre del ratón, flechas / W-S del teclado, y D-pad o sticks del gamepad.
    /// Cancelar (Esc / B) equivale a pulsar Volver.
    /// </summary>
    public class CreditsView : MonoBehaviour, ICancelHandler
    {
        [SerializeField] CreditsData data;
        [SerializeField] UIPalette palette;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] UnityEngine.UI.ScrollRect scrollRect;
        [SerializeField] RectTransform content;
        [SerializeField] UnityEngine.UI.Button backButton;
        [SerializeField, Min(100f)] float keyboardScrollSpeed = 700f;

        void Awake() => Build();

        void OnEnable()
        {
            if (content != null) content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
            if (scrollRect != null) scrollRect.velocity = Vector2.zero;
        }

        // ---------------------------------------------------------------- Construcción del contenido
        [ContextMenu("Reconstruir créditos")]
        public void Build()
        {
            if (data == null || content == null || palette == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            AddText(data.gameTitle, 76, palette.Pergamino);
            AddText(data.studio, 38, palette.Arena);

            AddSection("Autores");
            foreach (var a in data.authors) AddEntry(a.name, a.role);

            AddSection("Herramientas");
            foreach (var t in data.tools)
                AddEntry(t.name, string.IsNullOrEmpty(t.url) ? t.usage : t.usage + "\n" + t.url);

            AddSection("Recursos externos");
            foreach (var r in data.assets)
                AddEntry(r.name, "Autor: " + r.author + "\nFuente: " + r.source + "\nLicencia: " + r.license);

            AddSpace(30);
        }

        void AddSection(string title)
        {
            AddSpace(44);
            AddText(title, 48, palette.Pergamino);
            AddSpace(6);
        }

        void AddEntry(string title, string detail)
        {
            AddSpace(12);
            AddText(title, 34, palette.Pergamino);
            if (!string.IsNullOrEmpty(detail)) AddText(detail, 28, palette.Arena);
        }

        void AddSpace(float height)
        {
            var go = new GameObject("Espacio", typeof(RectTransform));
            go.transform.SetParent(content, false);
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
        }

        void AddText(string text, float size, Color color)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(content, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Top;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
        }

        // ---------------------------------------------------------------- Desplazamiento con teclado / gamepad
        void Update()
        {
            if (scrollRect == null || content == null || scrollRect.viewport == null) return;

            float axis = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) axis += 1f;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) axis -= 1f;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                axis += gp.dpad.y.ReadValue();
                axis += gp.leftStick.y.ReadValue();
                axis += gp.rightStick.y.ReadValue();
            }
            axis = Mathf.Clamp(axis, -1f, 1f);
            if (Mathf.Abs(axis) < 0.1f) return;

            float scrollable = Mathf.Max(0f, content.rect.height - scrollRect.viewport.rect.height);
            var p = content.anchoredPosition;
            p.y = Mathf.Clamp(p.y - axis * keyboardScrollSpeed * Time.unscaledDeltaTime, 0f, scrollable);
            scrollRect.velocity = Vector2.zero;
            content.anchoredPosition = p;
        }

        // Cancelar (Esc / B) = Volver.
        public void OnCancel(BaseEventData eventData)
        {
            if (backButton != null && backButton.interactable) backButton.onClick.Invoke();
            eventData.Use();
        }
    }
}

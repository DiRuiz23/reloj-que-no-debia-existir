using TMPro;
using UnityEngine;
using Reloj.UI;

namespace Reloj.Clues
{
    /// <summary>
    /// Texto del objetivo actual en el HUD (I-27). Va en el prefab HUD_Game. Si no se asigna 'label',
    /// crea uno arriba al centro. Se actualiza solo cuando <see cref="LevelObjectives"/> cambia de objetivo.
    /// </summary>
    public class ObjectiveHUD : MonoBehaviour
    {
        [SerializeField] UIPalette palette;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] TMP_Text label;
        [SerializeField] string prefix = "Objetivo: ";
        [Tooltip("Duracion (s) del destello al cumplir un objetivo.")]
        [SerializeField, Min(0f)] float flashSeconds = 1.2f;

        LevelObjectives objectives;
        float flashUntil;
        Color baseColor = new Color(0.92f, 0.85f, 0.71f, 1f);

        void Awake()
        {
            if (palette != null) baseColor = palette.Pergamino;
            if (label == null) label = BuildLabel();
        }

        void Start()
        {
            objectives = LevelObjectives.Instance;
            if (objectives == null)
            {
                Show("");   // nivel sin objetivos definidos: no se muestra nada
                return;
            }
            objectives.OnObjectiveChanged += OnChanged;
            Show(objectives.CurrentText);
        }

        void OnDestroy()
        {
            if (objectives != null) objectives.OnObjectiveChanged -= OnChanged;
        }

        void OnChanged(string text, bool advanced)
        {
            Show(text);
            if (advanced) flashUntil = Time.unscaledTime + flashSeconds;
        }

        void Show(string text)
        {
            if (label == null) return;
            bool has = !string.IsNullOrEmpty(text);
            label.gameObject.SetActive(has);
            if (has) label.text = prefix + text;
        }

        void Update()
        {
            if (label == null || !label.gameObject.activeSelf) return;
            if (flashUntil > Time.unscaledTime && palette != null)
            {
                // Destello suave de "objetivo nuevo": del color de acento a pergamino. Solo mientras dura.
                float t = 1f - (flashUntil - Time.unscaledTime) / Mathf.Max(0.01f, flashSeconds);
                label.color = Color.Lerp(palette.Arena, baseColor, t);
            }
            else label.color = baseColor;
        }

        TMP_Text BuildLabel()
        {
            var go = new GameObject("ObjetivoLabel", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(760f, 60f);

            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = 24;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            t.color = baseColor;
            return t;
        }
    }
}

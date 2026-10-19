using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reloj.UI
{
    [RequireComponent(typeof(Button))]
    [ExecuteAlways]
    public class PaletteButtonStyle : MonoBehaviour
    {
        [SerializeField] UIPalette palette;
        [SerializeField] TMP_Text label;

        void OnEnable() => Apply();

        [ContextMenu("Aplicar paleta")]
        public void Apply()
        {
            if (palette == null) return;
            var button = GetComponent<Button>();
            var c = button.colors;

            c.normalColor = palette.TierraOscura;
            c.highlightedColor = palette.SepiaMedio;
            c.selectedColor = palette.SepiaMedio;
            c.pressedColor = palette.Tinta;

            Color off = palette.TierraOscura; off.a = 0.4f;
            c.disabledColor = off;

            c.colorMultiplier = 1f;
            c.fadeDuration = 0.08f;
            button.colors = c;

            if (label != null) label.color = palette.Pergamino;
        }
    }
}
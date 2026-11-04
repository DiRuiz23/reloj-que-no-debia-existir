using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloj.Tutorial
{
    /// <summary>
    /// Mantiene un texto con la tecla/boton real del dispositivo activo (p. ej. el "[E] Hablar" que flota
    /// sobre un NPC). En <c>format</c>, "{0}" es la tecla.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class InputPromptLabel : MonoBehaviour
    {
        [SerializeField] InputActionReference action;
        [SerializeField] string format = "{0} Examinar";
        [Tooltip("Lo que se muestra en {0} con pantalla tactil (hasta I-37).")]
        [SerializeField] string touchLabel = "Toca";

        TMP_Text text;

        void Awake() { text = GetComponent<TMP_Text>(); }

        void OnEnable()
        {
            InputDeviceTracker.Changed += OnSchemeChanged;
            if (action != null) action.action.Enable();
            Refresh();
        }

        void OnDisable() { InputDeviceTracker.Changed -= OnSchemeChanged; }

        void OnSchemeChanged(InputScheme _) => Refresh();

        void Refresh()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            string key = action != null
                ? InputPrompt.Label(action.action, InputDeviceTracker.Current, touchLabel, brackets: true, highlight: false)
                : touchLabel;
            text.text = string.Format(format, key);
        }
    }
}

using System;
using System.Collections;
using Reloj.Core;
using Reloj.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Reloj.UI
{
    /// <summary>
    /// Panel de Opciones. I-35: interfaz y foco. I-36: lo conecta con SettingsService
    /// (volumen por AudioMixer, calidad, reasignacion de controles, Restablecer y guardado).
    /// </summary>
    public class OptionsPanelUI : MonoBehaviour, ICancelHandler
    {
        [Serializable]
        public class RebindRow
        {
            [Tooltip("Nombre de la accion en PlayerControls: Move, Jump, Interact, Timestop, Pause.")]
            public string actionName;
            public TMP_Text keyLabel;
            public UnityEngine.UI.Button button;
        }

        [Header("Audio (0-100 %)")]
        [SerializeField] UnityEngine.UI.Slider musicSlider;
        [SerializeField] UnityEngine.UI.Slider sfxSlider;
        [SerializeField] TMP_Text musicValue;
        [SerializeField] TMP_Text sfxValue;

        [Header("Gráficos")]
        [SerializeField] TMP_Dropdown qualityDropdown;

        [Header("Controles (I-36)")]
        [SerializeField] RebindRow[] rows;
        [Tooltip("Mensajes al jugador: tecla repetida, valores restablecidos...")]
        [SerializeField] TMP_Text feedbackText;
        [SerializeField] Color warningColor = new Color(0.95f, 0.55f, 0.35f);
        [SerializeField] Color infoColor = new Color(0.79f, 0.66f, 0.47f);

        [Header("Navegación")]
        [SerializeField] UnityEngine.UI.Button resetButton;
        [SerializeField] UnityEngine.UI.Button backButton;

        public UnityEngine.UI.Slider MusicSlider => musicSlider;
        public UnityEngine.UI.Slider SfxSlider => sfxSlider;
        public TMP_Dropdown QualityDropdown => qualityDropdown;
        public UnityEngine.UI.Button BackButton => backButton;

        GameObject _previousSelection;
        bool _syncing;
        bool _rebinding;
        string[] _qualityNames;
        Coroutine _feedbackRoutine;
        SettingsService Settings => SettingsService.Instance;

        void Awake()
        {
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
            if (rows != null)
                foreach (var r in rows)
                {
                    var row = r;
                    if (row.button != null) row.button.onClick.AddListener(() => OnRebindClicked(row));
                }
        }

        void OnEnable()
        {
            if (Settings != null)
            {
                Settings.Changed += RefreshAll;
                Settings.BindingsChanged += RefreshControls;
            }
            else
            {
                Debug.LogWarning("[OptionsPanelUI] No hay SettingsService activo. Inicia desde Boot o agrega GameService a la escena.", this);
            }
            InputDeviceTracker.Changed += OnSchemeChanged;

            ClearFeedback();
            PopulateQuality();
            RefreshAll();

            var es = EventSystem.current;
            _previousSelection = es != null ? es.currentSelectedGameObject : null;
            StartCoroutine(SelectFirst());
        }

        void OnDisable()
        {
            if (Settings != null)
            {
                Settings.Changed -= RefreshAll;
                Settings.BindingsChanged -= RefreshControls;
                Settings.CancelRebind();
                Settings.Flush();   // guarda al cerrar
            }
            InputDeviceTracker.Changed -= OnSchemeChanged;

            _rebinding = false;
            if (resetButton != null) resetButton.interactable = true;
            if (backButton != null) backButton.interactable = true;
            var es = EventSystem.current;
            if (es != null)
            {
                es.sendNavigationEvents = true;
                if (_previousSelection != null && _previousSelection.activeInHierarchy)
                    es.SetSelectedGameObject(_previousSelection);
            }
            _previousSelection = null;
        }

        // Espera un frame: así gana al foco que ponga quien abre el panel (menú o pausa).
        IEnumerator SelectFirst()
        {
            yield return null;
            var es = EventSystem.current;
            if (es != null && musicSlider != null) es.SetSelectedGameObject(musicSlider.gameObject);
        }

        // ---------------------------------------------------------------- audio y calidad

        void OnMusicChanged(float v)
        {
            RefreshLabels();
            if (!_syncing && Settings != null) Settings.SetMusicVolume(v / 100f);
        }

        void OnSfxChanged(float v)
        {
            RefreshLabels();
            if (!_syncing && Settings != null) Settings.SetSfxVolume(v / 100f);
        }

        void OnQualityChanged(int index)
        {
            if (!_syncing && Settings != null) Settings.SetQuality(index);
        }

        void PopulateQuality()
        {
            if (qualityDropdown == null) return;
            _qualityNames = QualitySettings.names;
            _syncing = true;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(_qualityNames));
            _syncing = false;
        }

        void RefreshAll()
        {
            _syncing = true;
            if (Settings != null)
            {
                if (musicSlider != null) musicSlider.SetValueWithoutNotify(Mathf.Round(Settings.MusicVolume * 100f));
                if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(Mathf.Round(Settings.SfxVolume * 100f));
                if (qualityDropdown != null)
                {
                    qualityDropdown.SetValueWithoutNotify(Settings.QualityLevel);
                    qualityDropdown.RefreshShownValue();
                }
            }
            _syncing = false;
            RefreshLabels();
            RefreshControls();
        }

        void RefreshLabels()
        {
            if (musicValue != null && musicSlider != null)
                musicValue.text = Mathf.RoundToInt(musicSlider.value) + " %";
            if (sfxValue != null && sfxSlider != null)
                sfxValue.text = Mathf.RoundToInt(sfxSlider.value) + " %";
        }

        // ---------------------------------------------------------------- controles

        void OnSchemeChanged(InputScheme _) { if (!_rebinding) RefreshControls(); }

        void RefreshControls()
        {
            if (resetButton != null) resetButton.interactable = !_rebinding;
            if (backButton != null) backButton.interactable = !_rebinding;
            if (rows == null || Settings == null || Settings.Controls == null) return;
            var scheme = InputDeviceTracker.Current;
            foreach (var r in rows)
            {
                var action = Settings.Controls.FindAction(r.actionName, false);
                if (action == null) continue;

                if (r.keyLabel != null)
                {
                    string kb = InputPrompt.Label(action, InputScheme.Keyboard, null, false, false);
                    string pad = InputPrompt.Label(action, InputScheme.Gamepad, null, false, false);
                    r.keyLabel.text = kb + "  |  " + pad;
                }
                if (r.button != null) r.button.interactable = !_rebinding && Settings.CanRebind(r.actionName, scheme);
            }
        }

        void OnRebindClicked(RebindRow row)
        {
            if (Settings == null || _rebinding) return;
            var scheme = InputDeviceTracker.Current;
            if (!Settings.CanRebind(row.actionName, scheme))
            {
                Show(scheme == InputScheme.Touch
                    ? "Los controles táctiles no se pueden reasignar."
                    : "Esta acción no se puede reasignar con este dispositivo.", true);
                return;
            }

            _rebinding = true;
            ClearFeedback();
            var es = EventSystem.current;
            if (es != null) es.sendNavigationEvents = false;   // flechas/Enter/Esc no deben mover el menú
            if (rows != null)
                foreach (var r in rows) if (r.button != null) r.button.interactable = false;
            if (resetButton != null) resetButton.interactable = false;
            if (backButton != null) backButton.interactable = false;

            Settings.StartRebind(row.actionName, scheme,
                result => OnRebindDone(row, result),
                text => { if (row.keyLabel != null) row.keyLabel.text = text; });
        }

        void OnRebindDone(RebindRow row, RebindResult result)
        {
            _rebinding = false;
            switch (result.outcome)
            {
                case RebindOutcome.Applied:
                    Show($"{SettingsService.DisplayName(row.actionName)}: {result.key}", false);
                    break;
                case RebindOutcome.Conflict:
                    Show($"«{result.key}» ya está asignada a «{result.conflictAction}». Elige otra tecla.", true);
                    break;
                case RebindOutcome.Cancelled:
                    ClearFeedback();
                    break;
            }
            RefreshControls();
            if (isActiveAndEnabled) StartCoroutine(RestoreNavigation(row));
        }

        // Un instante despues: la tecla usada para reasignar no debe activar el menu.
        IEnumerator RestoreNavigation(RebindRow row)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            var es = EventSystem.current;
            if (es == null) yield break;
            es.sendNavigationEvents = true;
            if (row.button != null) es.SetSelectedGameObject(row.button.gameObject);
        }

        // ---------------------------------------------------------------- restablecer y avisos

        void OnResetClicked()
        {
            if (Settings == null) return;
            Settings.ResetToDefaults();   // dispara Changed -> RefreshAll
            Show("Valores restablecidos.", false);
        }

        void Show(string message, bool warning)
        {
            if (feedbackText == null) return;
            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            feedbackText.color = warning ? warningColor : infoColor;
            feedbackText.text = message;
            _feedbackRoutine = StartCoroutine(HideFeedbackLater());
        }

        IEnumerator HideFeedbackLater()
        {
            yield return new WaitForSecondsRealtime(5f);
            if (feedbackText != null) feedbackText.text = "";
            _feedbackRoutine = null;
        }

        void ClearFeedback()
        {
            if (_feedbackRoutine != null) { StopCoroutine(_feedbackRoutine); _feedbackRoutine = null; }
            if (feedbackText != null) feedbackText.text = "";
        }

        /// <summary>Cierra el panel. El botón Volver lo llama; el menú principal además llama a ClosePanels().</summary>
        public void Close() => gameObject.SetActive(false);

        // Cancelar (Esc / B del gamepad) equivale a pulsar Volver.
        public void OnCancel(BaseEventData eventData)
        {
            if (_rebinding) { eventData.Use(); return; }
            if (backButton != null && backButton.interactable) backButton.onClick.Invoke();
            else Close();
            eventData.Use();
        }
    }
}

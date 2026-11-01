using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Reloj.UI
{
    /// <summary>
    /// Panel de Opciones (I-35). Solo interfaz: muestra los controles, actualiza los porcentajes
    /// y gestiona el foco para ratón, teclado y gamepad.
    /// La conexión real con audio, reasignación de controles y calidad gráfica se hace en I-36
    /// usando las propiedades públicas de esta clase.
    /// </summary>
    public class OptionsPanelUI : MonoBehaviour, ICancelHandler
    {
        [Header("Audio (0-100 %)")]
        [SerializeField] UnityEngine.UI.Slider musicSlider;
        [SerializeField] UnityEngine.UI.Slider sfxSlider;
        [SerializeField] TMP_Text musicValue;
        [SerializeField] TMP_Text sfxValue;

        [Header("Gráficos")]
        [SerializeField] TMP_Dropdown qualityDropdown;

        [Header("Navegación")]
        [SerializeField] UnityEngine.UI.Button backButton;

        // Puntos de enganche para I-36.
        public UnityEngine.UI.Slider MusicSlider => musicSlider;
        public UnityEngine.UI.Slider SfxSlider => sfxSlider;
        public TMP_Dropdown QualityDropdown => qualityDropdown;
        public UnityEngine.UI.Button BackButton => backButton;

        GameObject _previousSelection;

        void Awake()
        {
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(_ => RefreshLabels());
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(_ => RefreshLabels());
        }

        void OnEnable()
        {
            RefreshLabels();
            var es = EventSystem.current;
            _previousSelection = es != null ? es.currentSelectedGameObject : null;
            StartCoroutine(SelectFirst());
        }

        void OnDisable()
        {
            var es = EventSystem.current;
            if (es != null && _previousSelection != null && _previousSelection.activeInHierarchy)
                es.SetSelectedGameObject(_previousSelection);
            _previousSelection = null;
        }

        // Espera un frame: así gana al foco que ponga quien abre el panel (menú o pausa).
        IEnumerator SelectFirst()
        {
            yield return null;
            var es = EventSystem.current;
            if (es != null && musicSlider != null) es.SetSelectedGameObject(musicSlider.gameObject);
        }

        void RefreshLabels()
        {
            if (musicValue != null && musicSlider != null)
                musicValue.text = Mathf.RoundToInt(musicSlider.value) + " %";
            if (sfxValue != null && sfxSlider != null)
                sfxValue.text = Mathf.RoundToInt(sfxSlider.value) + " %";
        }

        /// <summary>Cierra el panel. El botón Volver lo llama; el menú principal además llama a ClosePanels().</summary>
        public void Close() => gameObject.SetActive(false);

        // Cancelar (Esc / B del gamepad) equivale a pulsar Volver.
        public void OnCancel(BaseEventData eventData)
        {
            if (backButton != null && backButton.interactable) backButton.onClick.Invoke();
            else Close();
            eventData.Use();
        }
    }
}

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Reloj.Core;
using Reloj.Progression;

namespace Reloj.UI
{
    /// <summary>
    /// Menu simple de asignacion de puntos de habilidad (I-38). Se abre desde la pantalla de Fin del Nivel y
    /// desde la Pausa (GameStateUI.OpenSkills). Muestra los puntos disponibles y, por cada categoria, su nivel (0 a 3),
    /// el efecto actual y el siguiente, y un boton "Mejorar" que gasta 1 punto. No deja gastar mas de lo ganado:
    /// SkillPointManager.TryUpgrade lo valida y aqui el boton se desactiva sin puntos o al llegar al maximo.
    /// Se cierra solo si cambia el estado del juego (p. ej. Reanudar con la tecla de pausa).
    /// </summary>
    public class SkillMenuPanel : MonoBehaviour, ICancelHandler
    {
        [Serializable]
        public class Row
        {
            public SkillCategory category;
            public TMP_Text nameLabel;
            public TMP_Text levelLabel;
            public TMP_Text effectLabel;
            public Button upgradeButton;
            public TMP_Text upgradeButtonLabel;
        }

        [Header("Textos")]
        [Tooltip("Puntos disponibles y ganados.")]
        [SerializeField] TMP_Text pointsLabel;
        [Tooltip("Mensaje breve tras mejorar (opcional).")]
        [SerializeField] TMP_Text feedbackLabel;

        [Header("Categorias (una fila por mejora)")]
        [SerializeField] Row[] rows;

        [Header("Navegacion")]
        [SerializeField] Button backButton;

        /// <summary>true mientras el menu esta abierto. GameStateUI lo usa para no robarle el foco.</summary>
        public static bool IsOpen { get; private set; }

        GameObject _previousSelection;
        GameStateManager _state;

        static SkillPointManager Skills => SkillPointManager.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { IsOpen = false; }

        // ---------------------------------------------------------------- ciclo de vida

        void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(Close);
            if (rows == null) return;
            foreach (var r in rows)
            {
                var row = r;
                if (row.upgradeButton != null) row.upgradeButton.onClick.AddListener(() => OnUpgradeClicked(row));
            }
        }

        void OnEnable()
        {
            IsOpen = true;

            if (Skills != null) Skills.Changed += Refresh;
            else Debug.LogWarning("[SkillMenuPanel] No hay SkillPointManager. Inicia desde Boot o agrega GameService a la escena.", this);

            var gs = GameServices.Instance;
            _state = gs != null ? gs.State : null;
            if (_state != null) _state.OnStateChanged += OnStateChanged;

            var es = EventSystem.current;
            _previousSelection = es != null ? es.currentSelectedGameObject : null;

            if (feedbackLabel != null) feedbackLabel.text = "";
            Refresh();
            StartCoroutine(SelectFirst());
        }

        void OnDisable()
        {
            IsOpen = false;

            if (Skills != null) Skills.Changed -= Refresh;
            if (_state != null) _state.OnStateChanged -= OnStateChanged;
            _state = null;

            var es = EventSystem.current;
            if (es != null && _previousSelection != null && _previousSelection.activeInHierarchy)
                es.SetSelectedGameObject(_previousSelection);
            _previousSelection = null;
        }

        void OnStateChanged(GameState previous, GameState next) => Close();

        // Espera un frame: asi gana al foco que ponga quien abre el panel (pausa o fin del nivel).
        IEnumerator SelectFirst()
        {
            yield return null;
            EnsureSelection();
        }

        // ---------------------------------------------------------------- API

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // por encima de los paneles de estado

            bool hasActiveChild = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeInHierarchy) { hasActiveChild = true; break; }
            }
            if (backButton == null || !hasActiveChild)
            {
                Debug.LogError("[SkillMenuPanel] Panel sin contenido o sin boton Volver; se cierra para evitar bloqueo.", this);
                Close();
            }
        }

        public void Close() => gameObject.SetActive(false);

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame)
                || (pad != null && pad.buttonEast.wasPressedThisFrame))
            {
                Close();
            }
        }

        // Cancelar (Esc / B del gamepad) equivale a pulsar Volver.
        public void OnCancel(BaseEventData eventData)
        {
            Close();
            eventData.Use();
        }

        // ---------------------------------------------------------------- logica

        void OnUpgradeClicked(Row row)
        {
            var sp = Skills;
            if (sp == null) return;

            if (sp.TryUpgrade(row.category))   // dispara Changed -> Refresh
            {
                if (feedbackLabel != null)
                    feedbackLabel.text = $"{SkillPointManager.DisplayName(row.category)}: {sp.DescribeEffect(row.category, sp.GetLevel(row.category))}";
            }
            else if (feedbackLabel != null)
            {
                feedbackLabel.text = sp.IsMaxed(row.category) ? "Esa mejora ya está al máximo." : "No tienes puntos disponibles.";
            }
            EnsureSelection();
        }

        void Refresh()
        {
            var sp = Skills;

            if (pointsLabel != null)
                pointsLabel.text = sp == null
                    ? "Sin datos de progreso"
                    : $"Puntos disponibles: {sp.Available}   (ganados: {sp.TotalEarned})";

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    int level = sp != null ? sp.GetLevel(r.category) : 0;
                    bool maxed = level >= SkillPointManager.MaxLevel;

                    if (r.nameLabel != null) r.nameLabel.text = SkillPointManager.DisplayName(r.category);
                    if (r.levelLabel != null) r.levelLabel.text = $"Nivel {level}/{SkillPointManager.MaxLevel}";
                    if (r.effectLabel != null)
                    {
                        string now = sp != null ? sp.DescribeEffect(r.category, level) : "-";
                        r.effectLabel.text = maxed || sp == null
                            ? now
                            : $"{now}  |  Siguiente: {sp.DescribeEffect(r.category, level + 1)}";
                    }
                    if (r.upgradeButton != null) r.upgradeButton.interactable = sp != null && sp.CanUpgrade(r.category);
                    if (r.upgradeButtonLabel != null) r.upgradeButtonLabel.text = maxed ? "MÁX" : "Mejorar (1 pt)";
                }
            }

            if (isActiveAndEnabled) EnsureSelection();
        }

        // Si el foco quedo en un boton desactivado (p. ej. mejora al maximo) o se perdio, pasa al primero util.
        void EnsureSelection()
        {
            var es = EventSystem.current;
            if (es == null) return;

            var current = es.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy && current.transform.IsChildOf(transform))
            {
                var sel = current.GetComponent<Selectable>();
                if (sel != null && sel.IsInteractable()) return;
            }

            GameObject target = null;
            if (rows != null)
                foreach (var r in rows)
                    if (r.upgradeButton != null && r.upgradeButton.IsInteractable()) { target = r.upgradeButton.gameObject; break; }
            if (target == null && backButton != null) target = backButton.gameObject;
            if (target != null) es.SetSelectedGameObject(target);
        }
    }
}

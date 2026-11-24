using Reloj.Clues;
using Reloj.Core;
using Reloj.Dialogue;
using Reloj.TimeStop;
using Reloj.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Reloj.UI
{
    /// <summary>
    /// Administra los controles tactiles en pantalla (I-37):
    /// - Solo visibles cuando el dispositivo activo es Touch y el juego esta en Jugando (sin dialogo ni menu de pistas).
    /// - Sincroniza el estado visual de Parada Temporal e Interactuar.
    /// </summary>
    public class TouchControlsUI : MonoBehaviour
    {
        [Header("Contenedor")]
        [SerializeField] GameObject controlsRoot;
        [SerializeField] RectTransform stickKnob;
        [SerializeField] UIPalette palette;

        [Header("Parada Temporal")]
        [SerializeField] Image timestopFillRing;
        [SerializeField] Image timestopIcon;

        [Header("Interactuar")]
        [SerializeField] Image interactBackground;
        [SerializeField] Image interactIcon;

        [Header("Pruebas en Editor")]
        [SerializeField] bool forceShowInEditor;

        GameStateManager stateManager;
        TimeStopController timeStop;
        bool hasLastVisibility;
        bool lastVisible;

        Color Tinta => palette != null ? palette.Tinta : new Color(0.17f, 0.11f, 0.08f, 1f);
        Color Sepia => palette != null ? palette.SepiaMedio : new Color(0.55f, 0.42f, 0.28f, 1f);
        Color Arena => palette != null ? palette.Arena : new Color(0.79f, 0.66f, 0.47f, 1f);
        Color Pergamino => palette != null ? palette.Pergamino : new Color(0.92f, 0.85f, 0.71f, 1f);
        Color Acento => palette != null ? palette.Acento : new Color(1f, 0.65f, 0.17f, 1f);

        void Awake()
        {
            EnsureEventSystem();
            EnsureCanvasRaycaster();
            RefreshVisibility(force: true);
        }

        void OnEnable()
        {
            InputDeviceTracker.Changed += OnSchemeChanged;
            BindStateManager();
            RefreshVisibility(force: true);
        }

        void Start()
        {
            BindStateManager();
            timeStop = TimeStopController.Instance;
            RefreshVisibility(force: true);
        }

        void OnDisable()
        {
            InputDeviceTracker.Changed -= OnSchemeChanged;
            if (stateManager != null)
            {
                stateManager.OnStateChanged -= OnGameStateChanged;
                stateManager = null;
            }
        }

        void LateUpdate()
        {
            RefreshVisibility(force: false);
            if (controlsRoot != null && controlsRoot.activeSelf)
                UpdateActionFeedback();
        }

        void BindStateManager()
        {
            var current = GameServices.Instance != null ? GameServices.Instance.State : null;
            if (current == null || current == stateManager) return;
            if (stateManager != null) stateManager.OnStateChanged -= OnGameStateChanged;
            stateManager = current;
            stateManager.OnStateChanged += OnGameStateChanged;
        }

        void OnSchemeChanged(InputScheme _) => RefreshVisibility(force: false);
        void OnGameStateChanged(GameState _, GameState __) => RefreshVisibility(force: false);

        void RefreshVisibility(bool force)
        {
            if (controlsRoot == null) return;

            bool isTouch =
#if UNITY_EDITOR
                forceShowInEditor ||
#endif
                InputDeviceTracker.Current == InputScheme.Touch;

            bool playing = stateManager == null || stateManager.CurrentState == GameState.Jugando;
            bool inDialogue = DialogueRunner.IsRunning;
            bool inClues = ClueMenuUI.Instance != null && ClueMenuUI.Instance.IsOpen;
            bool shouldShow = isTouch && playing && !inDialogue && !inClues;

            if (!force && hasLastVisibility && shouldShow == lastVisible) return;

            hasLastVisibility = true;
            lastVisible = shouldShow;

            if (stickKnob != null) stickKnob.anchoredPosition = Vector2.zero;
            controlsRoot.SetActive(shouldShow);
        }

        void UpdateActionFeedback()
        {
            if (timeStop == null) timeStop = TimeStopController.Instance;
            if (timeStop != null)
            {
                if (timestopFillRing != null)
                {
                    switch (timeStop.State)
                    {
                        case TimeStopState.Ready:
                            timestopFillRing.fillAmount = 1f;
                            timestopFillRing.color = Acento;
                            break;
                        case TimeStopState.Active:
                            timestopFillRing.fillAmount = timeStop.Duration > 0f
                                ? Mathf.Clamp01(timeStop.RemainingActiveTime / timeStop.Duration)
                                : 0f;
                            timestopFillRing.color = Acento;
                            break;
                        default:
                            timestopFillRing.fillAmount = timeStop.CooldownNormalized;
                            timestopFillRing.color = Sepia;
                            break;
                    }
                }
                if (timestopIcon != null)
                    timestopIcon.color = timeStop.State == TimeStopState.Recharging ? Arena : Acento;
            }
            else if (timestopFillRing != null)
            {
                timestopFillRing.fillAmount = 1f;
                timestopFillRing.color = Acento;
            }

            bool canInteract = DialogueNPC.Current != null || ClueExaminable.Current != null;
            if (interactBackground != null)
            {
                Color c = canInteract ? Tinta : new Color(Tinta.r, Tinta.g, Tinta.b, 0.45f);
                if (canInteract) c.a = 0.85f;
                interactBackground.color = c;
            }
            if (interactIcon != null)
                interactIcon.color = canInteract ? Pergamino : new Color(Arena.r, Arena.g, Arena.b, 0.65f);
        }

        void EnsureCanvasRaycaster()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            if (transform.parent != null && transform.parent.GetComponentInParent<Canvas>() != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 6;
            }
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}

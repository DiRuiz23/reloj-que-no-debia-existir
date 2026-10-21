using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Dialogue
{
    /// <summary>
    /// Reproduce un DialogueFile: texto progresivo, opciones, flags y bloqueo del jugador.
    /// Interact salta la animacion del texto (o cierra un nodo final).
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        /// <summary>true mientras hay una conversacion abierta.</summary>
        public static bool IsRunning => Instance != null && Instance.running;

        [Header("UI")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] RectTransform choicesContainer;
        [Tooltip("Prefab de boton para las opciones (Button_Base).")]
        [SerializeField] Button choiceButtonPrefab;
        [Tooltip("Fondo clicable/tocable del panel: sirve para saltar el texto en pantallas tactiles (Android).")]
        [SerializeField] Button advanceButton;

        [Header("Entrada")]
        [SerializeField] InputActionReference interactAction;

        [Header("Texto")]
        [SerializeField, Min(1f)] float charsPerSecond = 40f;

        [Header("Jugador")]
        [Tooltip("Si se deja vacio se busca el PlayerMotor de la escena.")]
        [SerializeField] PlayerMotor player;

        DialogueFile file;
        DialogueNode node;
        bool running;
        bool typing;
        float visibleChars;
        int totalChars;
        int startFrame = -1;
        int endFrame = -1;

        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;

        /// <summary>Se puede abrir otra conversacion (no hay una activa ni se cerro en este mismo frame).</summary>
        public bool CanStart => !running && Time.frameCount != endFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("[Dialogue] Hay mas de un DialogueRunner en la escena.", this);
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (interactAction != null)
            {
                interactAction.action.performed += OnInteract;
                interactAction.action.Enable();
            }
            if (advanceButton != null) advanceButton.onClick.AddListener(Advance);
        }

        void OnDisable()
        {
            // No se desactiva la accion Interact: la comparten los NPC.
            if (interactAction != null) interactAction.action.performed -= OnInteract;
            if (advanceButton != null) advanceButton.onClick.RemoveListener(Advance);
            if (running) EndDialogue();
        }

        // ---------------------------------------------------------------- API

        public bool StartDialogue(DialogueFile dialogue)
        {
            if (!CanStart || dialogue == null) return false;

            var first = ResolveNode(dialogue, dialogue.StartNode);
            if (first == null)
            {
                Debug.LogError($"[Dialogue] '{dialogue.id}': nodo inicial inexistente.");
                return false;
            }

            file = dialogue;
            running = true;
            startFrame = Time.frameCount;

            if (player == null) player = FindFirstObjectByType<PlayerMotor>();
            if (player != null) player.InputLocked = true;

            if (panel != null) panel.SetActive(true);
            OnDialogueStarted?.Invoke();
            ShowNode(first);
            return true;
        }

        /// <summary>Equivale a presionar Interact: salta el texto o cierra un nodo final.</summary>
        public void Advance()
        {
            if (!running || Time.frameCount == startFrame) return;

            if (typing) FinishTyping();
            else if (IsTerminal(node)) EndDialogue();
            // Con opciones visibles se avanza eligiendo una.
        }

        public void EndDialogue()
        {
            if (!running) return;
            running = false;
            typing = false;
            node = null;
            file = null;
            endFrame = Time.frameCount;

            ClearChoices();
            if (panel != null) panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (player != null) player.InputLocked = false;
            OnDialogueEnded?.Invoke();
        }

        // ---------------------------------------------------------------- flujo

        void OnInteract(InputAction.CallbackContext ctx) => Advance();

        void Update()
        {
            if (!typing) return;

            visibleChars += charsPerSecond * Time.deltaTime;
            int shown = Mathf.Min(totalChars, Mathf.FloorToInt(visibleChars));
            bodyLabel.maxVisibleCharacters = shown;
            if (shown >= totalChars) FinishTyping();
        }

        void ShowNode(DialogueNode n)
        {
            node = n;
            ClearChoices();

            if (speakerLabel != null)
            {
                speakerLabel.text = n.speaker ?? string.Empty;
                speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(n.speaker));
            }

            bodyLabel.text = n.text ?? string.Empty;
            bodyLabel.maxVisibleCharacters = 0;
            bodyLabel.ForceMeshUpdate();
            totalChars = bodyLabel.textInfo.characterCount;
            visibleChars = 0f;
            typing = totalChars > 0;

            if (!typing) FinishTyping();
        }

        void FinishTyping()
        {
            typing = false;
            bodyLabel.maxVisibleCharacters = int.MaxValue;
            if (node == null) return;

            if (IsTerminal(node))
            {
                if (!node.end)
                    Debug.LogWarning($"[Dialogue] El nodo '{node.id}' no tiene opciones ni 'end': se cierra al terminar.");
                return; // Interact (o tocar el panel) lo cierra.
            }
            BuildChoices(node);
        }

        static bool IsTerminal(DialogueNode n) =>
            n != null && (n.end || n.choices == null || n.choices.Count == 0);

        void BuildChoices(DialogueNode n)
        {
            if (choicesContainer == null || choiceButtonPrefab == null)
            {
                Debug.LogError("[Dialogue] Falta choicesContainer o choiceButtonPrefab en el DialogueRunner.", this);
                return;
            }

            GameObject first = null;
            for (int i = 0; i < n.choices.Count; i++)
            {
                int index = i;
                var button = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = n.choices[i].text;
                button.onClick.AddListener(() => Choose(index));
                if (first == null) first = button.gameObject;
            }

            // Permite elegir con teclado o gamepad.
            if (first != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(first);
        }

        void ClearChoices()
        {
            if (choicesContainer == null) return;
            for (int i = choicesContainer.childCount - 1; i >= 0; i--)
                Destroy(choicesContainer.GetChild(i).gameObject);
        }

        void Choose(int index)
        {
            if (!running || node == null || typing) return;
            if (index < 0 || index >= node.choices.Count) return;

            var choice = node.choices[index];
            if (!string.IsNullOrEmpty(choice.setFlag))
                DialogueFlagManager.SetFlag(choice.setFlag, true);

            var next = ResolveNode(file, file.GetNode(choice.next));
            if (next == null)
            {
                Debug.LogError($"[Dialogue] '{file.id}': la opcion '{choice.text}' apunta a un nodo inexistente ('{choice.next}'). Se cierra el dialogo.");
                EndDialogue();
                return;
            }
            ShowNode(next);
        }

        /// <summary>Aplica altIf: si el flag ya esta activo, salta al nodo alternativo.</summary>
        static DialogueNode ResolveNode(DialogueFile f, DialogueNode n)
        {
            for (int guard = 0; n != null && guard < 8; guard++)
            {
                if (n.altIf == null || !n.altIf.IsSet || !DialogueFlagManager.GetFlag(n.altIf.flag)) return n;
                var alt = f.GetNode(n.altIf.next);
                if (alt == null)
                {
                    Debug.LogError($"[Dialogue] '{f.id}': altIf del nodo '{n.id}' apunta a '{n.altIf.next}', que no existe.");
                    return n;
                }
                n = alt;
            }
            return n;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Reloj.Player;

namespace Reloj.Dialogue
{
    /// <summary>
    /// NPC con el que se puede hablar. Va en un objeto con un Collider en modo Trigger
    /// (la "zona de interaccion"). Al estar el jugador dentro y presionar Interact,
    /// abre su dialogo en el DialogueRunner.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogueNPC : MonoBehaviour
    {
        /// <summary>NPC cuya zona ocupa el jugador ahora (para un boton tactil en Android).</summary>
        public static DialogueNPC Current { get; private set; }

        [Header("Dialogo")]
        [Tooltip("Arrastra aqui el .json (TextAsset). Tiene prioridad sobre Resources Id.")]
        [SerializeField] TextAsset dialogueAsset;
        [Tooltip("Alternativa: nombre del archivo en Resources/Dialogues, sin .json")]
        [SerializeField] string resourcesId;

        [Header("Entrada")]
        [SerializeField] InputActionReference interactAction;

        [Header("Indicador (opcional)")]
        [Tooltip("Objeto que se muestra cuando el jugador puede hablar (ej. un '[E] Hablar').")]
        [SerializeField] GameObject prompt;

        DialogueFile dialogue;
        bool playerInRange;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void Awake()
        {
            if (dialogueAsset != null) dialogue = DialogueLoader.Parse(dialogueAsset);
            else if (!string.IsNullOrEmpty(resourcesId)) dialogue = DialogueLoader.LoadFromResources(resourcesId);

            if (dialogue == null)
                Debug.LogWarning($"[Dialogue] '{name}' no tiene un dialogo valido asignado.", this);

            if (prompt != null) prompt.SetActive(false);
        }

        void OnDisable() => SetInRange(false);

        void Update()
        {
            if (prompt != null)
                prompt.SetActive(playerInRange && !DialogueRunner.IsRunning);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() != null) SetInRange(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() != null) SetInRange(false);
        }

        void SetInRange(bool value)
        {
            if (playerInRange == value) return;
            playerInRange = value;

            if (interactAction != null)
            {
                if (value)
                {
                    interactAction.action.performed += OnInteract;
                    interactAction.action.Enable();
                }
                else
                {
                    interactAction.action.performed -= OnInteract;
                }
            }

            if (value) Current = this;
            else if (Current == this) Current = null;
        }

        void OnInteract(InputAction.CallbackContext ctx) => Interact();

        /// <summary>Abre el dialogo. Tambien sirve para un boton tactil.</summary>
        public bool Interact()
        {
            if (!playerInRange || dialogue == null) return false;

            var runner = DialogueRunner.Instance;
            if (runner == null)
            {
                Debug.LogError("[Dialogue] No hay un DialogueRunner en la escena.", this);
                return false;
            }
            // CanStart es false si ya hay una conversacion o si una acaba de cerrarse en este frame:
            // asi el mismo toque de Interact que cierra un dialogo no abre otro.
            return runner.CanStart && runner.StartDialogue(dialogue);
        }

        /// <summary>Para enlazar al OnClick de un boton "Interactuar" en pantalla tactil.</summary>
        public static void InteractWithCurrent()
        {
            if (Current != null) Current.Interact();
        }
    }
}
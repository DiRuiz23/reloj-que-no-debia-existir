using UnityEngine;
using UnityEngine.InputSystem;
using Reloj.Core;
using Reloj.Level;
using Reloj.Player;

namespace Reloj.Clues
{
    /// <summary>
    /// Objeto examinable que da una pista (I-27). Va en un objeto con Collider en modo Trigger:
    /// con el jugador dentro y al presionar Interact, la pista entra al inventario y se avisa en pantalla.
    /// Activa el flag de la pista (fuente unica de verdad); el inventario reacciona al flag.
    /// Si la pista ya se tiene, no hace nada (no se agrega dos veces).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ClueExaminable : MonoBehaviour
    {
        /// <summary>Pista examinable en cuya zona esta el jugador (para un boton tactil).</summary>
        public static ClueExaminable Current { get; private set; }

        [SerializeField] ClueData clue;
        [SerializeField] InputActionReference interactAction;
        [Tooltip("Objeto que se muestra cuando se puede examinar (ej. '[E] Examinar'). Se oculta al obtener la pista.")]
        [SerializeField] GameObject prompt;
        [Tooltip("Objetos que se apagan al obtener la pista (ej. el brillo del objeto).")]
        [SerializeField] GameObject[] hideWhenCollected;

        bool inRange;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        bool AlreadyHave
        {
            get
            {
                if (clue == null) return false;
                var inv = ClueInventory.Current;
                return inv != null ? inv.Has(clue) : DialogueFlagManager.GetFlag(clue.Flag);
            }
        }

        void Start() { Refresh(); }
        void OnDisable() { SetInRange(false); }

        void Update()
        {
            if (prompt != null) prompt.SetActive(inRange && !AlreadyHave);
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
            if (inRange == value) return;
            inRange = value;

            if (interactAction != null)
            {
                if (value) { interactAction.action.performed += OnInteract; interactAction.action.Enable(); }
                else interactAction.action.performed -= OnInteract;
            }

            if (value) Current = this;
            else if (Current == this) Current = null;
        }

        void OnInteract(InputAction.CallbackContext ctx) => Examine();

        /// <summary>Examina el objeto. Tambien sirve para un boton tactil (<see cref="ExamineCurrent"/>).</summary>
        public bool Examine()
        {
            if (!inRange || clue == null || AlreadyHave) return false;

            // El flag es la fuente de verdad: el inventario (y otros sistemas) reaccionan a el.
            DialogueFlagManager.SetFlag(clue.Flag, true);

            // Escena de prueba sin GameServices: no hay inventario que reaccione, se avisa directo.
            if (ClueInventory.Current == null && HintBanner.Instance != null)
                HintBanner.Instance.Flash("Pista guardada: " + clue.Title, 3f);

            Refresh();
            return true;
        }

        public static void ExamineCurrent() { if (Current != null) Current.Examine(); }

        void Refresh()
        {
            bool have = AlreadyHave;
            if (hideWhenCollected != null)
                foreach (var go in hideWhenCollected) if (go != null) go.SetActive(!have);
            if (have && prompt != null) prompt.SetActive(false);
        }
    }
}

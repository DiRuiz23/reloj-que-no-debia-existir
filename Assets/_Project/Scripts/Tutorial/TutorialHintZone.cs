using UnityEngine;
using UnityEngine.InputSystem;
using Reloj.Dialogue;
using Reloj.Level;
using Reloj.Player;
using Reloj.TimeStop;

namespace Reloj.Tutorial
{
    public enum TutorialTask
    {
        Move,       // se cumple al caminar un rato dentro de la zona
        Jump,       // se cumple al saltar dentro de la zona
        Interact,   // se cumple cuando una interaccion abre un dialogo dentro de la zona
        TimeStop,   // se cumple al usar la Parada Temporal (en cualquier lugar)
        Read,       // sin accion: se muestra unos segundos (p. ej. el objetivo) aunque la persona se aleje
    }

    /// <summary>
    /// Indicacion contextual del tutorial: aparece al llegar a la zona y desaparece cuando la persona
    /// hace la accion. El texto usa "{0}" para la tecla/boton real del dispositivo activo.
    /// No vuelve a aparecer si el paso ya se cumplio o si el tutorial ya fue completado.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialHintZone : MonoBehaviour
    {
        const int BannerPriority = 1;   // por encima de las pistas normales (HintZone)

        [Header("Paso")]
        [SerializeField] string stepId = "mover";
        [SerializeField] TutorialTask task = TutorialTask.Move;

        [Header("Texto")]
        [Tooltip("Accion cuyo binding se muestra en {0} (en Read puede quedar vacia).")]
        [SerializeField] InputActionReference action;
        [TextArea(1, 3)]
        [SerializeField] string message = "Pulsa {0}";
        [Tooltip("Lo que se muestra en {0} con pantalla tactil (hasta que I-37 agregue bindings tactiles).")]
        [SerializeField] string touchLabel = "el control en pantalla";

        [Header("Ajustes por tipo de paso")]
        [Tooltip("Move: segundos caminando para darlo por aprendido.")]
        [SerializeField, Min(0.1f)] float moveSeconds = 0.6f;
        [Tooltip("Read: segundos que permanece el texto visible.")]
        [SerializeField, Min(1f)] float readSeconds = 7f;

        PlayerMotor motor;
        bool inside;
        bool triggered;      // Read: ya se mostro y sigue hasta cumplirse, aunque se salga de la zona
        bool done;
        float progress;      // Move / Read: segundos acumulados
        bool dialogueWasRunning;
        TimeStopController timeStop;
        InputScheme shownScheme;
        float nextRefresh;
        string cachedText;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void Awake()
        {
            if (TutorialProgress.Completed || TutorialProgress.IsStepDone(stepId))
            {
                done = true;
                GetComponent<Collider>().enabled = false;
                enabled = false;
                return;
            }
            TutorialProgress.Register(stepId);
        }

        void OnDestroy()
        {
            if (!done) TutorialProgress.Unregister(stepId);
        }

        void OnEnable()
        {
            if (done) return;
            if (task == TutorialTask.TimeStop && timeStop == null && TimeStopController.Instance != null)
            {
                timeStop = TimeStopController.Instance;
                timeStop.OnTimeStopStart += OnTimeStopUsed;
            }
        }

        void Start()
        {
            // TimeStopController puede nacer despues que esta zona: si no se pudo en OnEnable, se hace aqui.
            if (!done && task == TutorialTask.TimeStop && timeStop == null && TimeStopController.Instance != null)
            {
                timeStop = TimeStopController.Instance;
                timeStop.OnTimeStopStart += OnTimeStopUsed;
            }
        }

        void OnDisable()
        {
            inside = false;
            if (HintBanner.Instance != null) HintBanner.Instance.Hide(this);
            if (timeStop != null) { timeStop.OnTimeStopStart -= OnTimeStopUsed; timeStop = null; }
            if (motor != null) motor.Jumped -= OnJumped;
        }

        void OnTriggerEnter(Collider other) => Enter(other);

        // Stay cubre al jugador que ya nace dentro de la zona (p. ej. el spawn).
        void OnTriggerStay(Collider other) { if (!inside) Enter(other); }

        void Enter(Collider other)
        {
            if (done) return;
            var m = other.GetComponentInParent<PlayerMotor>();
            if (m == null) return;
            motor = m;
            inside = true;
            if (action != null) action.action.Enable();   // para que el binding muestre el nombre real del mando
            if (task == TutorialTask.Read) triggered = true;
            if (task == TutorialTask.Jump) { motor.Jumped -= OnJumped; motor.Jumped += OnJumped; }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            inside = false;
            if (motor != null && task == TutorialTask.Jump) motor.Jumped -= OnJumped;
            if (!(task == TutorialTask.Read && triggered) && HintBanner.Instance != null)
                HintBanner.Instance.Hide(this);
        }

        void OnJumped()
        {
            if (inside && !done) Complete();
        }

        void OnTimeStopUsed()
        {
            if (!done) Complete();
        }

        void Update()
        {
            if (done) return;

            bool visibleZone = inside || (task == TutorialTask.Read && triggered);
            if (!visibleZone) return;

            var banner = HintBanner.Instance;
            if (banner == null) return;

            bool dialogue = DialogueRunner.IsRunning;

            // Interactuar: se cumple cuando la accion abre un dialogo estando en la zona.
            if (task == TutorialTask.Interact && inside && dialogue && !dialogueWasRunning)
            {
                dialogueWasRunning = true;
                Complete();
                return;
            }
            dialogueWasRunning = dialogue;

            // Durante un dialogo no se tapa la pantalla ni corre el tiempo de lectura.
            if (dialogue) { banner.Hide(this); return; }

            switch (task)
            {
                case TutorialTask.Move:
                    if (inside && motor != null && motor.NormalizedSpeed > 0.3f)
                    {
                        progress += Time.deltaTime;
                        if (progress >= moveSeconds) { Complete(); return; }
                    }
                    break;

                case TutorialTask.Read:
                    progress += Time.deltaTime;
                    if (progress >= readSeconds) { Complete(); return; }
                    break;
            }

            banner.Show(this, CurrentText(), BannerPriority);
        }

        string CurrentText()
        {
            var scheme = InputDeviceTracker.Current;
            // Se recalcula si cambia el dispositivo, y cada segundo (el nombre del boton puede resolverse
            // tras habilitar la accion: "Button South" -> "A").
            if (cachedText == null || scheme != shownScheme || Time.unscaledTime >= nextRefresh)
            {
                shownScheme = scheme;
                nextRefresh = Time.unscaledTime + 1f;
                string label = action != null
                    ? InputPrompt.Label(action.action, scheme, touchLabel)
                    : touchLabel;
                cachedText = string.Format(message, label);
            }
            return cachedText;
        }

        void Complete()
        {
            if (done) return;
            done = true;
            inside = false;
            if (HintBanner.Instance != null) HintBanner.Instance.Hide(this);
            GetComponent<Collider>().enabled = false;
            TutorialProgress.MarkStepDone(stepId);   // si era el ultimo pendiente, marca tutorial_completado
            enabled = false;                         // OnDisable desuscribe los eventos
        }
    }
}

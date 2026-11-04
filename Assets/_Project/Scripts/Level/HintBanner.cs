using TMPro;
using UnityEngine;
using Reloj.TimeStop;

namespace Reloj.Level
{
    // Banner de avisos en pantalla: pistas persistentes (mientras una zona las pide)
    // y avisos breves ("Punto de control guardado").
    [RequireComponent(typeof(CanvasGroup))]
    public class HintBanner : MonoBehaviour
    {
        public static HintBanner Instance { get; private set; }
        public static bool TimeStopUsed { get; private set; }

        [SerializeField] TMP_Text label;
        [SerializeField, Min(0.01f)] float fadeSeconds = 0.25f;

        CanvasGroup group;
        object owner;
        int ownerPriority;
        int ownerFrame = -10;
        string persistentText;
        string flashText;
        float flashUntil = -1f;
        TimeStopController controller;

        void Awake()
        {
            Instance = this;
            TimeStopUsed = false;
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        void Start()
        {
            controller = TimeStopController.Instance;
            if (controller != null) controller.OnTimeStopStart += HandleUsed;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (controller != null) controller.OnTimeStopStart -= HandleUsed;
        }

        void HandleUsed() { TimeStopUsed = true; }

        /// <summary>
        /// Pide mostrar un texto mientras la zona siga llamando cada frame. Si otra zona ya lo esta usando,
        /// solo se lo quita con mayor prioridad (a igual prioridad manda quien llego primero: sin parpadeo).
        /// </summary>
        public void Show(object who, string text, int priority = 0)
        {
            bool ownerAlive = owner != null && Time.frameCount - ownerFrame <= 1;
            if (ownerAlive && owner != who && priority <= ownerPriority) return;

            owner = who;
            ownerPriority = priority;
            ownerFrame = Time.frameCount;
            persistentText = text;
        }

        public void Hide(object who)
        {
            if (owner == who) owner = null;
        }

        public void Flash(string text, float seconds)
        {
            flashText = text;
            flashUntil = Time.unscaledTime + seconds;
        }

        void Update()
        {
            bool flash = Time.unscaledTime < flashUntil;
            bool show = flash || owner != null;
            if (show && label != null)
            {
                string txt = flash ? flashText : persistentText;
                if (label.text != txt) label.text = txt;
            }
            float target = show ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / fadeSeconds);
        }
    }
}

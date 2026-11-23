using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reloj.Core;
using Reloj.Dialogue;
using Reloj.Sound;

namespace Reloj.Endings
{
    /// <summary>
    /// Escena de decision final. El texto y las dos opciones salen de Resources/Dialogues/nivel4_decision.json.
    /// Las opciones son botones de UI: funcionan con raton, teclado (flechas + Enter/Espacio), gamepad y toque.
    /// Cada opcion activa su flag, muestra una ultima linea y carga su final.
    /// </summary>
    public class DecisionSceneController : MonoBehaviour
    {
        public const string FlagCorregir = "n4_final_corregir";
        public const string FlagProteger = "n4_final_proteger";
        public const string SessionChoiceKey = "final_choice";

        [Header("Datos")]
        [SerializeField] string dialogueId = "nivel4_decision";
        [Tooltip("Nodo al que lleva la opcion de dejar que la historia se corrija.")]
        [SerializeField] string nodeIdEndingA = "ir_final_a";
        [SerializeField] string sceneEndingA = "Ending_A";
        [Tooltip("Nodo al que lleva la opcion de proteger la vida que conozco.")]
        [SerializeField] string nodeIdEndingB = "ir_final_b";
        [SerializeField] string sceneEndingB = "Ending_B";

        [Header("UI")]
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] CanvasGroup optionsGroup;
        [SerializeField] Button optionAButton;
        [SerializeField] Button optionBButton;
        [SerializeField] CanvasGroup fade;
        [SerializeField, Min(1f)] float charsPerSecond = 38f;

        DialogueFile file;
        DialogueChoice choiceA, choiceB;
        bool chosen;

        void Start()
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null) AudioManager.Instance.SetSuspicious(true);   // tic-tac mientras se decide
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            if (fade != null) fade.alpha = 1f;
            EndingFlow.SetInteractable(optionsGroup, false);
            if (optionsGroup != null) optionsGroup.alpha = 0f;

            file = DialogueLoader.LoadFromResources(dialogueId);
            var node = file != null ? DialogueRunner.ResolveNode(file, file.StartNode) : null;
            if (node == null || node.choices == null || node.choices.Count < 2)
            {
                Debug.LogError($"[Decision] '{dialogueId}' debe tener un nodo inicial con 2 opciones.");
                yield break;
            }

            foreach (var c in node.choices)
            {
                if (c.next == nodeIdEndingA) choiceA = c;
                else if (c.next == nodeIdEndingB) choiceB = c;
            }
            if (choiceA == null || choiceB == null)
            {
                Debug.LogError($"[Decision] Las opciones deben llevar a '{nodeIdEndingA}' y '{nodeIdEndingB}'.");
                yield break;
            }

            SetLabel(optionAButton, choiceA.text);
            SetLabel(optionBButton, choiceB.text);
            optionAButton.onClick.AddListener(() => Choose(choiceA));
            optionBButton.onClick.AddListener(() => Choose(choiceB));

            if (speakerLabel != null)
            {
                speakerLabel.text = node.speaker ?? string.Empty;
                speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(node.speaker));
            }

            yield return EndingFlow.Fade(fade, 0f, 1.2f);
            yield return EndingFlow.TypeText(bodyLabel, node.text, charsPerSecond);

            yield return EndingFlow.Fade(optionsGroup, 1f, 0.4f);
            EndingFlow.SetInteractable(optionsGroup, true);
            EndingFlow.Select(optionAButton.gameObject);
        }

        static void SetLabel(Button button, string text)
        {
            var label = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
            if (label != null) label.text = text;
        }

        void Choose(DialogueChoice choice)
        {
            if (chosen || choice == null) return;
            chosen = true;
            StartCoroutine(ChooseRoutine(choice));
        }

        IEnumerator ChooseRoutine(DialogueChoice choice)
        {
            EndingFlow.Click();
            EndingFlow.SetInteractable(optionsGroup, false);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

            bool isA = choice == choiceA;

            // La decision queda como flag (y no se mezcla con una partida anterior).
            DialogueFlagManager.SetFlag(isA ? FlagProteger : FlagCorregir, false);
            DialogueFlagManager.SetFlag(isA ? FlagCorregir : FlagProteger, true);
            if (!string.IsNullOrEmpty(choice.setFlag)) DialogueFlagManager.SetFlag(choice.setFlag, true);

            var services = GameServices.Instance;
            if (services != null && services.Session != null)
                services.Session.SetData(SessionChoiceKey, isA ? "corregir" : "proteger");

            yield return EndingFlow.Fade(optionsGroup, 0f, 0.3f);

            // Ultima linea de Juan antes de cerrar la escena.
            var closing = DialogueRunner.ResolveNode(file, file.GetNode(choice.next));
            if (closing != null)
            {
                if (speakerLabel != null)
                {
                    speakerLabel.text = closing.speaker ?? string.Empty;
                    speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(closing.speaker));
                }
                yield return EndingFlow.TypeText(bodyLabel, closing.text, charsPerSecond);
                yield return EndingFlow.WaitPress(1.2f);
            }

            EndingFlow.FadeOutMusic(1f);
            if (AudioManager.Instance != null) AudioManager.Instance.SetSuspicious(false);
            yield return EndingFlow.Fade(fade, 1f, 1f);
            EndingFlow.LoadScene(isA ? sceneEndingA : sceneEndingB);
        }
    }
}

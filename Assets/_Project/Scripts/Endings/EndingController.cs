using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Reloj.Core;
using Reloj.Dialogue;

namespace Reloj.Endings
{
    /// <summary>
    /// Pantalla de cierre. Lee su texto de Resources/Dialogues/{dialogueId}.json: cada nodo es una pagina
    /// (con una opcion "Continuar" que lleva a la siguiente) y el ultimo lleva "end": true.
    /// Los altIf del JSON hacen que el final refleje decisiones previas (flags).
    /// Marca la partida como completada y, al terminar, ofrece volver al menu principal.
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [Header("Datos")]
        [Tooltip("Id con el que se registra este final en el guardado (ej. Ending_A).")]
        [SerializeField] string endingId = "Ending_A";
        [SerializeField] string dialogueId = "ending_a";
        [SerializeField] string menuSceneName = EndingFlow.MenuScene;

        [Header("Tarjeta de titulo")]
        [SerializeField] CanvasGroup introGroup;
        [SerializeField, Min(0f)] float introSeconds = 3f;

        [Header("Paginas")]
        [SerializeField] CanvasGroup pageGroup;
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] CanvasGroup hintGroup;
        [SerializeField, Min(1f)] float charsPerSecond = 34f;

        [Header("Cierre")]
        [Tooltip("Segundos que se queda la ultima pagina en pantalla antes de mostrar FIN y el boton de volver al menu.")]
        [SerializeField, Min(0f)] float finalDelaySeconds = 5f;
        [SerializeField, Min(0.1f)] float finalFadeSeconds = 2f;
        [SerializeField] CanvasGroup finalGroup;
        [SerializeField] Button menuButton;
        [SerializeField] CanvasGroup fade;

        [Header("Fondo (cambia lentamente)")]
        [SerializeField] Image backdrop;
        [SerializeField] Color backdropFrom = new Color(0.17f, 0.11f, 0.08f);
        [SerializeField] Color backdropTo = new Color(0.31f, 0.21f, 0.15f);
        [SerializeField, Min(1f)] float backdropSeconds = 45f;

        bool leaving;
        float startTime;

        void Start()
        {
            Time.timeScale = 1f;
            startTime = Time.unscaledTime;
            if (backdrop != null) backdrop.color = backdropFrom;
            if (fade != null) fade.alpha = 1f;
            if (introGroup != null) introGroup.alpha = 0f;
            if (pageGroup != null) pageGroup.alpha = 0f;
            if (hintGroup != null) hintGroup.alpha = 0f;
            if (finalGroup != null) { finalGroup.alpha = 0f; EndingFlow.SetInteractable(finalGroup, false); }
            if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);

            MarkGameCompleted();
            StartCoroutine(Run());
        }

        void Update()
        {
            if (backdrop == null) return;
            float p = Mathf.Clamp01((Time.unscaledTime - startTime) / backdropSeconds);
            backdrop.color = Color.Lerp(backdropFrom, backdropTo, Mathf.SmoothStep(0f, 1f, p));
        }

        // I-34: la partida queda marcada como completada. Si no hay SaveService (escena abierta directo), se escribe igual.
        void MarkGameCompleted()
        {
            var services = GameServices.Instance;
            if (services != null && services.Save != null) services.Save.MarkGameCompleted(endingId);
            else SaveService.WriteCompletion(endingId);

            if (services != null && services.Session != null)
            {
                services.Session.SetData("final_ending", endingId);
                services.Session.SetData("game_completed", "1");
            }
        }

        IEnumerator Run()
        {
            yield return EndingFlow.Fade(fade, 0f, 1.2f);

            // Tarjeta de titulo
            if (introGroup != null)
            {
                yield return EndingFlow.Fade(introGroup, 1f, 0.9f);
                yield return EndingFlow.WaitPress(introSeconds);
                yield return EndingFlow.Fade(introGroup, 0f, 0.6f);
            }

            var file = DialogueLoader.LoadFromResources(dialogueId);
            var node = file != null ? DialogueRunner.ResolveNode(file, file.StartNode) : null;
            if (node == null)
            {
                Debug.LogError($"[Ending] No se pudo cargar '{dialogueId}'.");
                yield return ShowFinal();
                yield break;
            }

            if (pageGroup != null) yield return EndingFlow.Fade(pageGroup, 1f, 0.4f);

            for (int guard = 0; node != null && guard < 64; guard++)
            {
                ShowSpeaker(node);
                if (hintGroup != null) hintGroup.alpha = 0f;
                yield return EndingFlow.TypeText(bodyLabel, node.text, charsPerSecond);

                bool last = node.end || node.choices == null || node.choices.Count == 0;
                if (last) break;

                if (hintGroup != null) yield return EndingFlow.Fade(hintGroup, 1f, 0.3f);
                yield return EndingFlow.WaitPress(0.3f);
                if (hintGroup != null) hintGroup.alpha = 0f;

                var choice = node.choices[0];
                if (!string.IsNullOrEmpty(choice.setFlag)) DialogueFlagManager.SetFlag(choice.setFlag, true);
                var next = DialogueRunner.ResolveNode(file, file.GetNode(choice.next));
                if (next == null)
                {
                    Debug.LogError($"[Ending] '{dialogueId}': '{choice.next}' no existe.");
                    break;
                }

                // Cambio de pagina con fundido corto.
                yield return EndingFlow.Fade(pageGroup, 0f, 0.3f);
                node = next;
                ShowSpeaker(node);
                if (bodyLabel != null) { bodyLabel.text = node.text ?? string.Empty; bodyLabel.maxVisibleCharacters = 0; }
                if (pageGroup != null) pageGroup.alpha = 1f;
            }

            yield return new WaitForSecondsRealtime(finalDelaySeconds);
            yield return ShowFinal();
        }

        void ShowSpeaker(DialogueNode node)
        {
            if (speakerLabel == null) return;
            speakerLabel.text = node.speaker ?? string.Empty;
            speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(node.speaker));
        }

        IEnumerator ShowFinal()
        {
            if (finalGroup != null)
            {
                yield return EndingFlow.Fade(finalGroup, 1f, finalFadeSeconds);
                EndingFlow.SetInteractable(finalGroup, true);
            }
            if (menuButton != null) EndingFlow.Select(menuButton.gameObject);
        }

        public void GoToMenu()
        {
            if (leaving) return;
            leaving = true;
            StartCoroutine(LeaveRoutine());
        }

        IEnumerator LeaveRoutine()
        {
            EndingFlow.Click();
            EndingFlow.FadeOutMusic(1f);
            EndingFlow.SetInteractable(finalGroup, false);
            yield return EndingFlow.Fade(fade, 1f, 1f);
            EndingFlow.LoadScene(menuSceneName);
        }
    }
}

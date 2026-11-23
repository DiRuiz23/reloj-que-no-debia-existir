using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Reloj.Core;
using Reloj.Sound;

namespace Reloj.Endings
{
    /// <summary>
    /// Utilidades compartidas por la escena de decision y los finales: entrada (raton, teclado,
    /// gamepad, tactil), texto progresivo, fundidos y carga de escenas.
    /// </summary>
    public static class EndingFlow
    {
        public const string MenuScene = "MainMenu";

        /// <summary>Clic, toque, Enter / Espacio / E o boton Sur / Oeste del gamepad en este frame.</summary>
        public static bool AnyPress()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                               kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;

            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)) return true;

            return false;
        }

        /// <summary>Texto progresivo (tiempo real). Cualquier pulsacion lo completa de inmediato.</summary>
        public static IEnumerator TypeText(TMP_Text label, string text, float charsPerSecond)
        {
            if (label == null) yield break;
            label.text = text ?? string.Empty;
            label.maxVisibleCharacters = 0;
            label.ForceMeshUpdate();
            int total = label.textInfo.characterCount;
            float shown = 0f;

            yield return null;   // la pulsacion que abrio esta pagina no cuenta
            while (shown < total)
            {
                shown += Mathf.Max(1f, charsPerSecond) * Time.unscaledDeltaTime;
                label.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                if (AnyPress()) break;
                yield return null;
            }
            label.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>Espera minSeconds (tiempo real) y despues una pulsacion.</summary>
        public static IEnumerator WaitPress(float minSeconds)
        {
            float end = Time.unscaledTime + Mathf.Max(0f, minSeconds);
            while (Time.unscaledTime < end) yield return null;
            yield return null;
            while (!AnyPress()) yield return null;
        }

        public static IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            if (group == null) yield break;
            float from = group.alpha;
            if (seconds <= 0f) { group.alpha = target; yield break; }
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            group.alpha = target;
        }

        public static void SetInteractable(CanvasGroup group, bool value)
        {
            if (group == null) return;
            group.interactable = value;
            group.blocksRaycasts = value;
        }

        public static void Select(GameObject go)
        {
            if (go == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(go);
        }

        public static void Click()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.UiClick);
        }

        public static void FadeOutMusic(float seconds)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.FadeOutMusic(seconds);
        }

        /// <summary>Carga una escena con el SceneLoader de GameServices; si no hay (escena abierta directo), con SceneManager.</summary>
        public static void LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Endings] La escena '{sceneName}' no esta en el Build Settings.");
                return;
            }
            Time.timeScale = 1f;
            var services = GameServices.Instance;
            if (services != null && services.Loader != null) services.Loader.LoadSingle(sceneName);
            else SceneManager.LoadScene(sceneName);
        }
    }
}

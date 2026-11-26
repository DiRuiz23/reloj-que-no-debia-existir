using System;
using System.Collections.Generic;
using System.IO;
using Reloj.Clues;
using Reloj.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Reloj.Core
{
    public enum GameState { Jugando, Pausado, FinDelNivel, GameOver }

    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private InputActionReference pauseAction;
        [Tooltip("Nombre de la escena del menú principal (debe estar en el build).")]
        [SerializeField] private string menuSceneName = "MainMenu";

        public GameState CurrentState { get; private set; } = GameState.Jugando;
        public GameState PreviousState { get; private set; } = GameState.Jugando;

        /// <summary>(estado anterior, estado nuevo)</summary>
        public event Action<GameState, GameState> OnStateChanged;

        static bool SceneInBuild(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                if (Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == sceneName) return true;
            return false;
        }

        /// <summary>true si la escena siguiente del build existe y no es el menú.</summary>
        public bool HasNextLevel
        {
            get
            {
                string active = SceneManager.GetActiveScene().name;
                if (LevelSequence.Contains(active))
                {
                    string seq = LevelSequence.NextAfter(active);
                    return seq != null && SceneInBuild(seq);
                }
                int next = SceneManager.GetActiveScene().buildIndex + 1;
                if (next >= SceneManager.sceneCountInBuildSettings) return false;
                string nextName = Path.GetFileNameWithoutExtension(
                    SceneUtility.GetScenePathByBuildIndex(next));
                return nextName != menuSceneName;
            }
        }

        private static readonly HashSet<string> NonPausableScenes = new HashSet<string>
        {
            "Boot", "Splash", "MainMenu", "Decision_Final", "Ending_A", "Ending_B"
        };

        private bool _inputActive;

        private void Start()
        {
            var services = GameServices.Instance;
            if (services != null && services.State != this) return;

            if (pauseAction == null)
            {
                Debug.LogWarning("[GameStateManager] Falta asignar Pause Action.", this);
                return;
            }

            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
            _inputActive = true;
        }

        private void OnDestroy()
        {
            if (!_inputActive) return;
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            if (NonPausableScenes.Contains(SceneManager.GetActiveScene().name)) return;
            if (SettingsService.Instance != null && SettingsService.Instance.IsRebinding) return;

            if (CurrentState == GameState.Jugando)
            {
                if (ClueMenuUI.Instance != null && ClueMenuUI.Instance.IsOpen)
                {
                    ClueMenuUI.Instance.Close();
                    return;
                }
                Pause();
            }
            else if (CurrentState == GameState.Pausado)
            {
                if (ClueMenuUI.Instance != null && ClueMenuUI.Instance.IsOpen)
                {
                    ClueMenuUI.Instance.Close();
                    return;
                }
                if (SkillMenuPanel.TryCloseActive()) return;
                if (OptionsPanelUI.TryCloseActive()) return;
                Resume();
            }
        }

        public void ResetToPlaying()
        {
            SetState(GameState.Jugando);
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        public void Pause()
        {
            if (CurrentState != GameState.Jugando) return;
            SetState(GameState.Pausado);
        }

        public void Resume()
        {
            if (CurrentState != GameState.Pausado) return;
            SetState(GameState.Jugando);
        }

        public void CompleteLevel(int score, float time)
        {
            if (CurrentState != GameState.Jugando) return;
            var services = GameServices.Instance;
            if (services != null && services.Session != null)
            {
                services.Session.SetLevelResult(score, time);
                services.Session.RecordLevelScore(SceneManager.GetActiveScene().name, score);
            }

            // I-38: exactamente 1 punto de habilidad por nivel completado (una sola vez por nivel).
            // Va ANTES de guardar para que el punto quede en el guardado de este mismo nivel.
            if (services != null && services.SkillPoints != null)
                services.SkillPoints.AwardLevelCompleted(SceneManager.GetActiveScene().name);

            // I-34: al terminar el nivel se desbloquea el siguiente y se guarda el progreso.
            if (services != null && services.Save != null)
                services.Save.RegisterLevelComplete(SceneManager.GetActiveScene().name, score);

            SetState(GameState.FinDelNivel);
        }

        public void Fail()
        {
            if (CurrentState != GameState.Jugando) return;
            SetState(GameState.GameOver);
        }

        public void Restart()
        {
            SetState(GameState.Jugando);
            GameServices.Instance.Loader.ReloadCurrent();
        }

        public void LoadNext()
        {
            if (!HasNextLevel)
            {
                Debug.LogWarning("[GameStateManager] No hay un siguiente nivel.");
                return;
            }
            string seqNext = LevelSequence.NextAfter(SceneManager.GetActiveScene().name);
            SetState(GameState.Jugando);
            if (seqNext != null) { GameServices.Instance.Loader.LoadSingle(seqNext); return; }
            GameServices.Instance.Loader.LoadSingle(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void QuitToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                Debug.LogWarning($"[GameStateManager] La escena '{menuSceneName}' no está en el build.");
                return;
            }

            // I-34: al salir al menú se guarda (conserva el último progreso y las opciones).
            var services = GameServices.Instance;
            if (services != null && services.Save != null) services.Save.Save();

            SetState(GameState.Jugando);   // devuelve timeScale a 1 antes de salir
            GameServices.Instance.Loader.LoadSingle(menuSceneName);
        }

        private void SetState(GameState next)
        {
            if (next == CurrentState) return;

            PreviousState = CurrentState;
            CurrentState = next;

            Time.timeScale = next == GameState.Jugando ? 1f : 0f;
            AudioListener.pause = next == GameState.Pausado;

            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }
    }
}

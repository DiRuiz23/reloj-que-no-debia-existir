using System;
using System.IO;
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

        /// <summary>true si la escena siguiente del build existe y no es el menú.</summary>
        public bool HasNextLevel
        {
            get
            {
                int next = SceneManager.GetActiveScene().buildIndex + 1;
                if (next >= SceneManager.sceneCountInBuildSettings) return false;
                string nextName = Path.GetFileNameWithoutExtension(
                    SceneUtility.GetScenePathByBuildIndex(next));
                return nextName != menuSceneName;
            }
        }

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
            if (CurrentState == GameState.Jugando) Pause();
            else if (CurrentState == GameState.Pausado) Resume();
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
                services.Session.SetLevelResult(score, time);
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
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            SetState(GameState.Jugando);
            GameServices.Instance.Loader.LoadSingle(next);
        }

        public void QuitToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
            {
                Debug.LogWarning($"[GameStateManager] La escena '{menuSceneName}' no está en el build.");
                return;
            }
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
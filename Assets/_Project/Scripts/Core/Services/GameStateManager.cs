using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Reloj.Core
{
    public enum GameState { Jugando, Pausado, FinDelNivel, GameOver }

    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private InputActionReference pauseAction;

        public GameState CurrentState { get; private set; } = GameState.Jugando;
        public GameState PreviousState { get; private set; } = GameState.Jugando;

        /// <summary>(estado anterior, estado nuevo)</summary>
        public event Action<GameState, GameState> OnStateChanged;

        private bool _inputActive;

        private void Start()
        {
            // Si este es un GameServices duplicado (al volver a Boot), no tocar el input
            // compartido: se destruye en este frame y desactivaría la acción del original.
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
            // En FinDelNivel y GameOver no se hace nada.
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
            // No se toca GameSession ni DialogueFlagManager: flags y puntos se conservan.
            SetState(GameState.GameOver);
        }

        public void Restart()
        {
            SetState(GameState.Jugando);
            GameServices.Instance.Loader.ReloadCurrent();
        }

        public void LoadNext()
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("[GameStateManager] No hay una escena siguiente en el build.");
                return;
            }
            SetState(GameState.Jugando);
            GameServices.Instance.Loader.LoadSingle(next);
        }

        private void SetState(GameState next)
        {
            if (next == CurrentState) return;

            PreviousState = CurrentState;
            CurrentState = next;

            // Jugando = tiempo normal. Cualquier otro estado congela el gameplay.
            Time.timeScale = next == GameState.Jugando ? 1f : 0f;
            AudioListener.pause = next == GameState.Pausado;

            OnStateChanged?.Invoke(PreviousState, CurrentState);
        }
    }
}
using Reloj.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Reloj.UI
{
    public class GameStateUI : MonoBehaviour
    {
        [SerializeField] private StatePanel[] panels;
        [Tooltip("Opcional. Panel de Opciones (I-35). Si está vacío solo escribe un log.")]
        [SerializeField] private GameObject optionsPanel;

        private GameStateManager _state;
        private StatePanel _active;

        private static GameStateManager State =>
            GameServices.Instance != null ? GameServices.Instance.State : null;

        private void Start()
        {
            _state = State;
            if (_state == null)
            {
                Debug.LogWarning("[GameStateUI] No hay GameServices. Inicia desde Boot o agrega el prefab.", this);
                return;
            }
            _state.OnStateChanged += HandleStateChanged;
            Apply(_state.CurrentState);
        }

        private void OnDestroy()
        {
            if (_state != null) _state.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState previous, GameState next) => Apply(next);

        private void Apply(GameState state)
        {
            _active = null;
            foreach (var p in panels)
            {
                if (p.VisibleIn == state) _active = p;
                else p.Hide();
            }
            if (_active != null) _active.Show();
            if (optionsPanel != null && state != GameState.Pausado) optionsPanel.SetActive(false);
        }

        // Si un clic en el vacío deja sin selección, se recupera para poder seguir con teclado o gamepad.
        private void Update()
        {
            var es = EventSystem.current;
            if (_active != null && es != null && es.currentSelectedGameObject == null)
                _active.Select();
        }

        // ---- Acciones para el On Click de los botones ----
        public void Resume() { if (State != null) State.Resume(); }
        public void Restart() { if (State != null) State.Restart(); }
        public void LoadNext() { if (State != null) State.LoadNext(); }
        public void QuitToMenu() { if (State != null) State.QuitToMenu(); }

        public void OpenOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(true);
            else Debug.Log("[GameStateUI] Opciones: pendiente (I-35).");
        }
    }
}
using TMPro;
using UnityEngine;

namespace Reloj.Core
{
    // Herramienta de prueba (I-11): dispara los estados con botones y muestra el resultado.
    public class GameStateTestPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private int testScore = 1500;
        [SerializeField] private float testTime = 83.5f;

        private static GameStateManager State =>
            GameServices.Instance != null ? GameServices.Instance.State : null;

        public void Pause() { if (State != null) State.Pause(); }
        public void Resume() { if (State != null) State.Resume(); }
        public void CompleteLevel() { if (State != null) State.CompleteLevel(testScore, testTime); }
        public void Fail() { if (State != null) State.Fail(); }
        public void Restart() { if (State != null) State.Restart(); }
        public void LoadNext() { if (State != null) State.LoadNext(); }

        public void AddPointsAndFlag()
        {
            var gs = GameServices.Instance;
            if (gs == null) return;
            gs.Session.AddScore(100);
            DialogueFlagManager.SetFlag("prueba");
        }

        private void Update()
        {
            if (statusLabel == null) return;
            var gs = GameServices.Instance;
            if (gs == null || gs.State == null)
            {
                statusLabel.text = "Sin GameServices";
                return;
            }
            statusLabel.text =
                $"Estado: {gs.State.CurrentState}\n" +
                $"TimeScale: {Time.timeScale}\n" +
                $"Puntos: {gs.Session.Score}\n" +
                $"Flag 'prueba': {DialogueFlagManager.GetFlag("prueba")}\n" +
                $"Resultado nivel: {gs.Session.LastLevelScore} pts / {gs.Session.LastLevelTime:F1} s";
        }
    }
}

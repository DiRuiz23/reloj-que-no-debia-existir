using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    [DefaultExecutionOrder(-100)]
    public class Level01Controller : MonoBehaviour
    {
        [SerializeField] string artSceneName = "Level01_Art";
        [SerializeField] PlayerRespawn player;
        [SerializeField] int maxScore = 1000;
        [SerializeField] int penaltyPerDeath = 100;

        float elapsed;
        bool completed;

        static GameStateManager State =>
            GameServices.Instance != null ? GameServices.Instance.State : null;

        void Awake()
        {
            // Cada partida del nivel empieza sin flags (el telegrama de la maleta hay que volver a tomarlo).
            DialogueFlagManager.Clear();
        }

        IEnumerator Start()
        {
            var services = GameServices.Instance;
            if (services != null && services.Session != null) services.Session.ClearCheckpoint();

            // Espera a que termine cualquier carga en curso (Reiniciar ya recarga el Art solo).
            yield return null;
            while (services != null && services.Loader != null && services.Loader.IsLoading)
                yield return null;

            if (SceneManager.GetSceneByName(artSceneName).isLoaded) yield break;

            if (services != null && services.Loader != null) services.Loader.LoadAdditive(artSceneName);
            else SceneManager.LoadSceneAsync(artSceneName, LoadSceneMode.Additive);
        }

        void Update()
        {
            var s = State;
            if (!completed && (s == null || s.CurrentState == GameState.Jugando))
                elapsed += Time.deltaTime;
        }

        public void Complete()
        {
            if (completed) return;
            var s = State;
            if (s == null) { Debug.LogError("[Level01] No hay GameServices."); return; }
            if (s.CurrentState != GameState.Jugando) return;

            completed = true;
            int deaths = player != null ? player.Deaths : 0;
            int score = Mathf.Max(0, maxScore - deaths * penaltyPerDeath);
            s.CompleteLevel(score, elapsed);
        }
    }
}
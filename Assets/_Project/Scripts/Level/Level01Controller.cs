using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reloj.Core;
using Reloj.Player;
using Reloj.Tutorial;

namespace Reloj.Level
{
    [DefaultExecutionOrder(-100)]
    public class Level01Controller : MonoBehaviour
    {
        [SerializeField] string artSceneName = "Level01_Art";
        [SerializeField] PlayerRespawn player;
        [SerializeField] int maxScore = 1000;
        [SerializeField] int penaltyPerDeath = 100;

        [Header("Flags / Reputacion (Nivel 3+)")]
        [Tooltip("true = el nivel empieza sin flags (Nivel 1). false = conserva las decisiones de niveles previos.")]
        [SerializeField] bool clearFlagsOnAwake = true;
        [Tooltip("Si no esta vacio, al iniciar se borran SOLO los flags con este prefijo (flags locales del nivel, ej. 'n3_'), para que Reiniciar sea limpio sin perder la reputacion.")]
        [SerializeField] string levelFlagPrefix = "";
        [Tooltip("Recalcula la reputacion (ReputationService) al iniciar el nivel.")]
        [SerializeField] bool recalculateReputation;
        [Tooltip("SOLO PRUEBAS (Editor/Development build): flags que se activan si abres la escena directo, sin pasar por el Nivel 1.")]
        [SerializeField] string[] editorTestFlags;

        bool completed;

        static GameStateManager State =>
            GameServices.Instance != null ? GameServices.Instance.State : null;

        void Awake()
        {
            // Cada partida del nivel empieza sin flags (el telegrama de la maleta hay que volver a tomarlo).
            if (clearFlagsOnAwake) DialogueFlagManager.Clear();
            else if (!string.IsNullOrEmpty(levelFlagPrefix))
            {
                var keep = new System.Collections.Generic.Dictionary<string, bool>();
                foreach (var kv in DialogueFlagManager.Snapshot())
                    if (!kv.Key.StartsWith(levelFlagPrefix)) keep[kv.Key] = kv.Value;
                DialogueFlagManager.Restore(keep);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (editorTestFlags != null)
                foreach (var f in editorTestFlags) if (!string.IsNullOrEmpty(f)) DialogueFlagManager.SetFlag(f, true);
#endif
            // ...salvo tutorial_completado, que es progreso de la persona y no del nivel.
            TutorialProgress.SyncFlag();
            if (recalculateReputation)
            {
                int score = ReputationService.Recalculate();
                Debug.Log($"[Level] Reputacion = {score} (alta confianza: {ReputationService.IsHighTrust})");
            }
            ClueTracker.Rebuild();
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

        public void Complete()
        {
            if (completed) return;
            var s = State;
            if (s == null) { Debug.LogError("[Level01] No hay GameServices."); return; }
            if (s.CurrentState != GameState.Jugando) return;

            completed = true;
            // Terminar el nivel cuenta como tutorial superado: al repetirlo no vuelve a explicarse.
            TutorialProgress.MarkCompleted();

            // El tiempo lo lleva LevelTimer: se congela y se toma el valor exacto.
            var timer = LevelTimer.Instance;
            if (timer != null) timer.Stop();
            float time = timer != null ? timer.Elapsed : 0f;

            int deaths = player != null ? player.Deaths : 0;
            int collected = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            int score = Mathf.Max(0, maxScore - deaths * penaltyPerDeath) + collected;

            // GameSession.LastLevelScore/Time -> pantalla de Fin del Nivel.
            s.CompleteLevel(score, time);
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Reloj.Clues;
using Reloj.Core;
using Reloj.Player;
using Reloj.Sound;
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

        [Header("Dificultad (I-30)")]
        [Tooltip("Asset LevelDifficultyConfig de este nivel. Los enemigos y la Parada Temporal leen sus valores de aqui. Vacio = cada uno usa sus valores propios.")]
        [SerializeField] LevelDifficultyConfig difficulty;

        [Header("Flags / Reputacion (Nivel 3+)")]
        [Tooltip("true = el nivel empieza sin flags (Nivel 1). false = conserva las decisiones de niveles previos.")]
        [SerializeField] bool clearFlagsOnAwake = true;
        [Tooltip("Si no esta vacio, al iniciar se borran SOLO los flags con este prefijo (flags locales del nivel, ej. 'n3_'), para que Reiniciar sea limpio sin perder la reputacion.")]
        [SerializeField] string levelFlagPrefix = "";
        [Tooltip("Recalcula la reputacion (ReputationService) al iniciar el nivel.")]
        [SerializeField] bool recalculateReputation;
        [Tooltip("SOLO PRUEBAS (Editor/Development build): flags que se activan si abres la escena directo, sin pasar por el Nivel 1.")]
        [SerializeField] string[] editorTestFlags;

        [Header("Final del juego")]
        [Tooltip("Si no esta vacio, al completar el nivel se carga esta escena (ej. Decision_Final) en lugar de la pantalla de Fin del Nivel.")]
        [SerializeField] string sceneAfterComplete = "";
        [SerializeField, Min(0f)] float sceneAfterCompleteDelay = 1.5f;

        bool completed;

        static GameStateManager State =>
            GameServices.Instance != null ? GameServices.Instance.State : null;

        void Awake()
        {
            // I-30: se registra antes que EnemyAI / TimeStopController (este script corre en orden -100).
            LevelDifficultyConfig.SetCurrent(difficulty, this);

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
                ReputationService.Recalculate();
            }
            // I-27: el inventario de pistas se alinea con los flags ya limpiados. Las pistas de niveles
            // previos se conservan (su flag sigue activo); las de este nivel se pierden al reiniciarlo.
            var clues = ClueInventory.Current;
            if (clues != null) clues.RebuildFromFlags();
        }

        void OnDestroy() { LevelDifficultyConfig.ReleaseCurrent(this); }

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
            // El puntaje del nivel es exactamente lo recolectado (el mismo valor que muestra el HUD).
            int score = collected;

            // Nivel 4: en vez del panel de resultados se pasa a la escena de decision final.
            if (!string.IsNullOrEmpty(sceneAfterComplete) && Application.CanStreamedLevelBeLoaded(sceneAfterComplete))
            {
                var gs = GameServices.Instance;
                if (gs != null && gs.Session != null) gs.Session.SetLevelResult(score, time);
                StartCoroutine(LoadSceneAfterComplete());
                return;
            }

            // GameSession.LastLevelScore/Time -> pantalla de Fin del Nivel.
            s.CompleteLevel(score, time);
        }

        IEnumerator LoadSceneAfterComplete()
        {
            var motor = FindFirstObjectByType<PlayerMotor>();
            if (motor != null) motor.InputLocked = true;
            var audio = AudioManager.Instance;
            if (audio != null) { audio.Play(Sfx.Goal); audio.FadeOutMusic(Mathf.Max(0.5f, sceneAfterCompleteDelay)); }
            yield return new WaitForSecondsRealtime(sceneAfterCompleteDelay);
            var services = GameServices.Instance;
            if (services != null && services.Loader != null) services.Loader.LoadSingle(sceneAfterComplete);
            else SceneManager.LoadScene(sceneAfterComplete);
        }
    }
}

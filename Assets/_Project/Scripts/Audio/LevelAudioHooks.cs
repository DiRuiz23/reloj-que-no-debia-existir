using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Reloj.Core;
using Reloj.Dialogue;
using Reloj.Player;
using Reloj.Stealth;
using Reloj.TimeStop;

namespace Reloj.Sound
{
    /// <summary>
    /// Conecta los eventos del nivel (parada de tiempo, muerte, checkpoint, dialogos, pausa, meta, UI,
    /// salto, objetos, dano y enemigos sospechosos) con el AudioManager.
    /// No modifica ningun otro script del juego.
    /// </summary>
    [RequireComponent(typeof(AudioManager))]
    public class LevelAudioHooks : MonoBehaviour
    {
        [Header("Pasos")]
        [Tooltip("Velocidad horizontal minima (m/s) para que suenen pasos.")]
        [SerializeField] float minStepSpeed = 0.6f;
        [Tooltip("Segundos entre pasos a la velocidad normal del jugador (5 m/s).")]
        [SerializeField] float stepInterval = 0.36f;

        AudioManager audioMgr;
        TimeStopController timeStop;
        PlayerRespawn respawn;
        PlayerMotor motor;
        PlayerHealth health;
        Rigidbody playerBody;
        GameStateManager state;
        DialogueRunner dialogue;
        string lastCheckpointId = "";
        float nextStepTime;
        int lastHealth = -1;

        // Enemigos que estan ahora mismo en estado Sospechoso (el tic-tac suena mientras haya alguno).
        readonly HashSet<EnemyAI> suspiciousEnemies = new HashSet<EnemyAI>();

        void Awake()
        {
            audioMgr = GetComponent<AudioManager>();
        }

        void Start()
        {
            timeStop = TimeStopController.Instance;
            if (timeStop != null)
            {
                timeStop.OnTimeStopStart += HandleTimeStopStart;
                timeStop.OnTimeStopEnd += HandleTimeStopEnd;
                timeStop.OnTimeStopReady += HandleTimeStopReady;
            }

            respawn = FindFirstObjectByType<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.OnDied += HandleDied;
                respawn.OnRespawned += HandleRespawned;
                playerBody = respawn.GetComponent<Rigidbody>();
            }

            motor = FindFirstObjectByType<PlayerMotor>();
            if (motor != null) motor.Jumped += HandleJumped;

            health = PlayerHealth.Instance;
            if (health != null) health.OnHealthChanged += HandleHealthChanged;

            Collectible.OnCollected += HandleCollected;
            EnemyAI.OnEnemyStateChanged += HandleEnemyStateChanged;

            var services = GameServices.Instance;
            if (services != null)
            {
                state = services.State;
                if (state != null) state.OnStateChanged += HandleStateChanged;
                if (services.Session != null) lastCheckpointId = services.Session.CheckpointId ?? "";
            }

            dialogue = DialogueRunner.Instance;
            if (dialogue != null)
            {
                dialogue.OnDialogueStarted += HandleDialogueStarted;
                dialogue.OnDialogueEnded += HandleDialogueEnded;
            }

            // Click en todos los botones de la escena (tambien los de paneles desactivados).
            foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                b.onClick.AddListener(HandleButtonClick);
        }

        void OnDestroy()
        {
            if (timeStop != null)
            {
                timeStop.OnTimeStopStart -= HandleTimeStopStart;
                timeStop.OnTimeStopEnd -= HandleTimeStopEnd;
                timeStop.OnTimeStopReady -= HandleTimeStopReady;
            }
            if (respawn != null)
            {
                respawn.OnDied -= HandleDied;
                respawn.OnRespawned -= HandleRespawned;
            }
            if (motor != null) motor.Jumped -= HandleJumped;
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
            Collectible.OnCollected -= HandleCollected;
            EnemyAI.OnEnemyStateChanged -= HandleEnemyStateChanged;
            if (state != null) state.OnStateChanged -= HandleStateChanged;
            if (dialogue != null)
            {
                dialogue.OnDialogueStarted -= HandleDialogueStarted;
                dialogue.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        void Update()
        {
            PollCheckpoint();
            UpdateFootsteps();
            UpdateTickTock();
        }

        // ------------------------------------------------------------------ pasos / checkpoint

        void UpdateFootsteps()
        {
            if (playerBody == null || Time.timeScale <= 0f) return;
            if (DialogueRunner.IsRunning) return;

            Vector3 v = playerBody.linearVelocity;
            float horizontal = new Vector2(v.x, v.z).magnitude;
            bool grounded = Mathf.Abs(v.y) < 1f;
            if (horizontal < minStepSpeed || !grounded) { nextStepTime = 0f; return; }

            if (Time.time >= nextStepTime)
            {
                audioMgr.Play(Sfx.Footstep);
                nextStepTime = Time.time + stepInterval * (5f / Mathf.Max(horizontal, 1f));
            }
        }

        void PollCheckpoint()
        {
            var services = GameServices.Instance;
            if (services == null || services.Session == null) return;
            string id = services.Session.CheckpointId ?? "";
            if (id == lastCheckpointId) return;
            lastCheckpointId = id;
            if (!string.IsNullOrEmpty(id)) audioMgr.Play(Sfx.Checkpoint);
        }

        // ------------------------------------------------------------------ tic-tac (Sospechoso)

        void HandleEnemyStateChanged(EnemyAI enemy, EnemyState from, EnemyState to)
        {
            if (enemy == null) return;
            if (to == EnemyState.Sospechoso) suspiciousEnemies.Add(enemy);
            else suspiciousEnemies.Remove(enemy);
            UpdateTickTock();
        }

        void UpdateTickTock()
        {
            // Un enemigo destruido o desactivado no deja el tic-tac encendido.
            suspiciousEnemies.RemoveWhere(e => e == null || !e.isActiveAndEnabled);
            audioMgr.SetSuspicious(suspiciousEnemies.Count > 0);
        }

        // ------------------------------------------------------------------ eventos

        void HandleTimeStopStart() { audioMgr.SetTimeStopped(true); audioMgr.Play(Sfx.TimeStopStart); }
        void HandleTimeStopEnd() { audioMgr.SetTimeStopped(false); audioMgr.Play(Sfx.TimeStopEnd); }
        void HandleTimeStopReady() { audioMgr.Play(Sfx.TimeStopReady); }

        void HandleDied() { audioMgr.Play(Sfx.Death); }
        void HandleRespawned() { audioMgr.Play(Sfx.Respawn); }

        void HandleJumped() { audioMgr.Play(Sfx.Jump); }
        void HandleCollected(Collectible c) { audioMgr.Play(Sfx.Pickup); }

        void HandleHealthChanged(int current, int max)
        {
            // Solo suena al perder vida (no al curar ni en la publicacion inicial).
            if (lastHealth >= 0 && current < lastHealth) audioMgr.Play(Sfx.Damage);
            lastHealth = current;
        }

        void HandleDialogueStarted() { audioMgr.Play(Sfx.DialogueOpen); }
        void HandleDialogueEnded() { audioMgr.Play(Sfx.DialogueClose); }

        void HandleButtonClick() { audioMgr.Play(Sfx.UiClick); }

        void HandleStateChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Pausado: audioMgr.Play(Sfx.Pause); break;
                case GameState.Jugando:
                    if (from == GameState.Pausado) audioMgr.Play(Sfx.Resume);
                    break;
                case GameState.FinDelNivel:
                    audioMgr.Play(Sfx.Goal);
                    audioMgr.FadeOutMusic();
                    suspiciousEnemies.Clear();
                    audioMgr.SetSuspicious(false);
                    break;
                case GameState.GameOver:
                    audioMgr.FadeOutMusic();
                    suspiciousEnemies.Clear();
                    audioMgr.SetSuspicious(false);
                    break;
            }
        }
    }
}

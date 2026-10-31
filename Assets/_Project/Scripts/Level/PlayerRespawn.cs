using System;
using UnityEngine;
using Reloj.Core;
using Reloj.Dialogue;

namespace Reloj.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerMotor))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] Transform initialSpawn;
        [SerializeField] float fallLimitY = -6f;
        [SerializeField, Min(0f)] float invulnerableSeconds = 1f;
        [Tooltip("0 = nunca GameOver. >0 = al llegar a ese número de muertes se llama Fail().")]
        [SerializeField, Min(0)] int maxDeathsBeforeGameOver = 0;

        Rigidbody rb;
        PlayerMotor motor;
        float invulnerableUntil;

        public int Deaths { get; private set; }
        public event Action OnDied;
        public event Action OnRespawned;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            motor = GetComponent<PlayerMotor>();
        }

        void Start()
        {
            if (initialSpawn != null) Teleport(initialSpawn.position);
        }

        void FixedUpdate()
        {
            if (transform.position.y < fallLimitY) Kill(true);
        }

        public void Kill(bool ignoreInvulnerability = false)
        {
            var state = GameServices.Instance != null ? GameServices.Instance.State : null;
            if (state != null && state.CurrentState != GameState.Jugando) return;
            if (!ignoreInvulnerability && Time.time < invulnerableUntil) return;

            Deaths++;
            OnDied?.Invoke();

            if (maxDeathsBeforeGameOver > 0 && Deaths >= maxDeathsBeforeGameOver && state != null)
            {
                state.Fail();
                return;
            }
            Respawn();
        }

        public void Respawn()
        {
            Vector3 pos = initialSpawn != null ? initialSpawn.position : transform.position;
            var session = GameServices.Instance != null ? GameServices.Instance.Session : null;
            if (session != null && session.HasCheckpoint) pos = session.CheckpointPosition;

            if (DialogueRunner.IsRunning) DialogueRunner.Instance.EndDialogue();

            Teleport(pos);
            motor.InputLocked = false;
            invulnerableUntil = Time.time + invulnerableSeconds;
            OnRespawned?.Invoke();
        }

        void Teleport(Vector3 pos)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            transform.position = pos;
        }
    }
}
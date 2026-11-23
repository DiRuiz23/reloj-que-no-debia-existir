using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>
    /// Encuentro final del Nivel 4 (arena). Es el "esqueleto" del encuentro: detecta la entrada del jugador,
    /// lleva el estado (iniciado / superado) en flags y expone eventos para que el Guardian se enganche
    /// cuando este terminado. NO contiene al Guardian.
    ///
    /// Contrato para el Guardian:
    ///  - Suscribirse a <see cref="Started"/> (o al UnityEvent onStarted) para aparecer en <see cref="GuardianSpawn"/>.
    ///  - Suscribirse a <see cref="Reset"/> para volver a su estado inicial cuando el jugador reaparece.
    ///  - Llamar a <see cref="Complete"/> al superar el encuentro: activa 'n4_encuentro_superado', que abre la
    ///    meta (LevelGoal) hacia la secuencia final (I-33) y reabre la reja de la arena.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArenaEncounter : MonoBehaviour
    {
        public static ArenaEncounter Current { get; private set; }
        public static event Action Started;
        public static event Action Completed;
        public static event Action Reset;

        [Header("Flags")]
        [SerializeField] string startedFlag = "n4_encuentro_iniciado";
        [SerializeField] string completedFlag = "n4_encuentro_superado";

        [Header("Reja de la arena (vive en Level04_Art)")]
        [SerializeField] string artSceneName = "Level04_Art";
        [SerializeField] string closedGateName = "Reja_Arena_Cerrada";
        [SerializeField] string openGateName = "Reja_Arena_Abierta";
        [Tooltip("Cierra la reja al empezar el encuentro. Mantener en false hasta que el Guardian exista: " +
                 "sin el, cerrar la reja dejaria al jugador encerrado.")]
        [SerializeField] bool closeGateOnStart = false;

        [Header("Guardian (pendiente)")]
        [SerializeField] Transform guardianSpawn;

        [Header("Mensajes")]
        [SerializeField] string startMessage = "";
        [SerializeField] string completeMessage = "La arena queda en silencio. El reloj de la salida vuelve a latir.";

        [Header("Eventos (para enganchar al Guardian sin codigo)")]
        [SerializeField] UnityEvent onStarted;
        [SerializeField] UnityEvent onCompleted;
        [SerializeField] UnityEvent onReset;

        PlayerRespawn respawn;

        public bool IsActive { get; private set; }
        public bool IsCompleted => DialogueFlagManager.GetFlag(completedFlag);
        public Transform GuardianSpawn => guardianSpawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; Started = null; Completed = null; Reset = null; }

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }

        void OnEnable()
        {
            respawn = FindFirstObjectByType<PlayerRespawn>();
            if (respawn != null) respawn.OnRespawned += HandleRespawned;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.OnRespawned -= HandleRespawned;
        }

        void OnValidate() { var c = GetComponent<Collider>(); if (c != null) c.isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (IsActive || IsCompleted) return;
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            StartEncounter();
        }

        public void StartEncounter()
        {
            if (IsActive || IsCompleted) return;
            IsActive = true;
            DialogueFlagManager.SetFlag(startedFlag, true);
            if (closeGateOnStart) SetGate(true);
            if (!string.IsNullOrEmpty(startMessage) && HintBanner.Instance != null) HintBanner.Instance.Flash(startMessage, 3.5f);
            Started?.Invoke();
            onStarted?.Invoke();
        }

        /// <summary>Lo llama el Guardian (o un script de prueba) al superar el encuentro.</summary>
        public void Complete()
        {
            if (IsCompleted) return;
            IsActive = false;
            DialogueFlagManager.SetFlag(completedFlag, true);
            SetGate(false);
            if (!string.IsNullOrEmpty(completeMessage) && HintBanner.Instance != null) HintBanner.Instance.Flash(completeMessage, 3.5f);
            Completed?.Invoke();
            onCompleted?.Invoke();
        }

        /// <summary>Vuelve el encuentro a cero (el jugador fue atrapado y reaparece en el checkpoint).</summary>
        public void ResetEncounter()
        {
            if (!IsActive) return;
            IsActive = false;
            DialogueFlagManager.SetFlag(startedFlag, false);
            SetGate(false);
            Reset?.Invoke();
            onReset?.Invoke();
        }

        void HandleRespawned()
        {
            if (IsActive && !IsCompleted) ResetEncounter();
        }

        // ---- reja (objetos de la escena Art, se buscan por nombre; pueden estar inactivos) ----
        void SetGate(bool closed)
        {
            var closedGo = FindInArt(closedGateName);
            var openGo = FindInArt(openGateName);
            if (closedGo != null) closedGo.SetActive(closed);
            if (openGo != null) openGo.SetActive(!closed);
        }

        GameObject FindInArt(string objectName)
        {
            var s = SceneManager.GetSceneByName(artSceneName);
            if (!s.isLoaded) return null;
            foreach (var root in s.GetRootGameObjects())
            {
                var t = FindDeep(root.transform, objectName);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        static Transform FindDeep(Transform t, string n)
        {
            if (t.name == n) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var r = FindDeep(t.GetChild(i), n);
                if (r != null) return r;
            }
            return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("DEV: iniciar encuentro")] void DevStart() => StartEncounter();
        [ContextMenu("DEV: simular encuentro superado")] void DevComplete() { if (!IsActive) StartEncounter(); Complete(); }
#endif

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            if (guardianSpawn != null) Gizmos.DrawWireSphere(guardianSpawn.position + Vector3.up * 1f, 0.6f);
            var c = GetComponent<BoxCollider>();
            if (c != null) { Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f); Gizmos.matrix = transform.localToWorldMatrix; Gizmos.DrawWireCube(c.center, c.size); }
        }
    }
}

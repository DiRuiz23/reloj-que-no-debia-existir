using System;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Player
{
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        /// <summary>Se dispara al recoger un objeto (sonido, efectos, estadisticas).</summary>
        public static event Action<Collectible> OnCollected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { OnCollected = null; }

        [SerializeField, Min(1)] int points = 100;
        [SerializeField] ParticleSystem pickupParticlesPrefab;

        bool collected;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnEnable() => collected = false;

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (!other.TryGetComponent<PlayerMotor>(out _) && other.GetComponentInParent<PlayerMotor>() == null) return;

            collected = true;   // evita doble suma si dos colliders del jugador entran el mismo frame
            if (ScoreManager.Instance != null) ScoreManager.Instance.Add(points);
            if (pickupParticlesPrefab != null)
                GameObjectPool.SpawnParticle(pickupParticlesPrefab, transform.position, Quaternion.identity);
            OnCollected?.Invoke(this);
            GameObjectPool.Release(gameObject);
        }
    }
}

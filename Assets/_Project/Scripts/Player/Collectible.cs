using System;
using UnityEngine;

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

        bool collected;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.GetComponentInParent<PlayerMotor>() == null) return;

            collected = true;   // evita doble suma si dos colliders del jugador entran el mismo frame
            if (ScoreManager.Instance != null) ScoreManager.Instance.Add(points);
            OnCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}

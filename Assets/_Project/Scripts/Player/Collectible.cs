using UnityEngine;

namespace Reloj.Player
{
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField, Min(1)] int points = 100;

        bool collected;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.GetComponentInParent<PlayerMotor>() == null) return;

            collected = true;   // evita doble suma si dos colliders del jugador entran el mismo frame
            if (ScoreManager.Instance != null) ScoreManager.Instance.Add(points);
            Destroy(gameObject);
        }
    }
}

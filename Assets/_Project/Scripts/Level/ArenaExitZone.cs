using UnityEngine;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>
    /// Zona de la arena del Nivel 4 que da por SUPERADO el encuentro con el Guardian: se coloca en el extremo
    /// opuesto a la entrada, antes del arco de salida. El Guardian (GuardianAI) no tiene vida, asi que el encuentro
    /// se supera sobreviviendo a su patrulla y a su onda de choque (con la Parada Temporal) y cruzando la arena.
    /// Al entrar el jugador llama a <see cref="ArenaEncounter.Complete"/>: activa 'n4_encuentro_superado'
    /// (abre la meta hacia la secuencia final), reabre la reja y retira al Guardian (evento onCompleted).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArenaExitZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            var enc = ArenaEncounter.Current;
            if (enc == null || enc.IsCompleted) return;
            if (!enc.IsActive) enc.StartEncounter();   // por si el jugador llego sin pasar por el trigger de entrada
            enc.Complete();
        }
    }
}

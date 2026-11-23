using UnityEngine;

namespace Reloj.Core
{
    // STUB (I-34): la persistencia real se implementa después.
    // Lo unico real por ahora es la marca de "partida completada" que escriben los finales:
    // se guarda en PlayerPrefs (funciona igual en Editor, WebGL y Android). Cuando I-34 cree el
    // archivo de guardado, debe leer GameCompleted / CompletedEndingId para incluirlos en el.
    public class SaveService : MonoBehaviour
    {
        const string CompletedKey = "save.game_completed";
        const string EndingKey = "save.ending_id";

        public void Save() => Debug.Log("[SaveService] Save() - stub");
        public bool Load() { Debug.Log("[SaveService] Load() - stub"); return false; }

        // STUB (I-34): devolverá true cuando exista un archivo de guardado.
        public bool HasSaveGame => false;

        /// <summary>true si el jugador ya llego a alguno de los finales.</summary>
        public static bool GameCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

        /// <summary>Id del ultimo final alcanzado (Ending_A / Ending_B), o vacio.</summary>
        public static string CompletedEndingId => PlayerPrefs.GetString(EndingKey, "");

        /// <summary>Marca la partida como completada con el final alcanzado y la guarda.</summary>
        public void MarkGameCompleted(string endingId)
        {
            WriteCompletion(endingId);
            Save();
        }

        /// <summary>Misma marca, sin necesitar la instancia (escenas abiertas sin pasar por Boot).</summary>
        public static void WriteCompletion(string endingId)
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.SetString(EndingKey, endingId ?? "");
            PlayerPrefs.Save();
        }
    }
}

using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Orden de los niveles JUGABLES (escenas Gameplay). El boton 'Siguiente nivel' sigue este orden,
    /// no el orden del Build Settings. Cada Gameplay carga su escena Art por si mismo (Level01Controller).
    /// </summary>
    public static class LevelSequence
    {
        public static readonly string[] Order =
        {
            "Level01_Gameplay",
            "Level02_Gameplay",
            "Level03_Gameplay",
        };

        public static bool Contains(string sceneName) => System.Array.IndexOf(Order, sceneName) >= 0;

        /// <summary>Siguiente escena de la secuencia, o null si es el ultimo nivel / no pertenece a la secuencia.</summary>
        public static string NextAfter(string sceneName)
        {
            int i = System.Array.IndexOf(Order, sceneName);
            if (i < 0 || i + 1 >= Order.Length) return null;
            return Order[i + 1];
        }
    }
}

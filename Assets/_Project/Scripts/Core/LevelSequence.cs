using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Orden de los niveles JUGABLES (escenas Gameplay). El boton 'Siguiente nivel' sigue este orden,
    /// no el orden del Build Settings. Cada Gameplay carga su escena Art por si mismo (Level01Controller).
    /// PARA AGREGAR EL NIVEL 2: insertar "Level02_Gameplay" entre Level01 y Level03 (ver Order).
    /// </summary>
    public static class LevelSequence
    {
        public static readonly string[] Order =
        {
            "Level01_Gameplay",
            // "Level02_Gameplay",   // <- descomentar cuando exista el Nivel 2 (y agregarlo al Build Settings)
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

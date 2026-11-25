using UnityEngine;

namespace Reloj.Progression
{
    /// <summary>
    /// Punto unico de lectura de los modificadores de habilidad (I-38). Los sistemas del juego preguntan aqui
    /// y combinan el valor con su base (LevelDifficultyConfig, I-30). Sin SkillPointManager (escena de prueba
    /// abierta sin Boot) devuelve valores neutros: el juego se comporta como antes.
    /// Se leen EN VIVO (no se copian), asi que gastar un punto en la pausa se nota al instante y nunca se acumulan.
    /// </summary>
    public static class SkillModifiers
    {
        static SkillPointManager M => SkillPointManager.Instance;

        /// <summary>Segundos que se SUMAN a la duracion de la Parada Temporal (0 sin mejora).</summary>
        public static float TimeStopBonusSeconds => M != null ? M.TimeStopBonusSeconds : 0f;

        /// <summary>Factor que MULTIPLICA el radio de deteccion de los enemigos (1 sin mejora, 0.7 al maximo).</summary>
        public static float DetectionRadiusMultiplier => M != null ? M.DetectionRadiusMultiplier : 1f;

        /// <summary>Nivel de persuasion (0 a 3).</summary>
        public static int PersuasionLevel => M != null ? M.PersuasionLevel : 0;

        /// <summary>true si una opcion de dialogo que exige 'required' (0 = sin requisito) esta desbloqueada.</summary>
        public static bool IsPersuasionUnlocked(int required) => required <= 0 || PersuasionLevel >= required;
    }
}

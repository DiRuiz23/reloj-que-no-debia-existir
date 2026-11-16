using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Dificultad de un nivel (I-30). Un asset por nivel en Assets/_Project/Data/Difficulty.
    /// El Level01Controller de cada escena lo registra como <see cref="Current"/> al cargar; los enemigos
    /// (EnemyAI) y el TimeStopController leen de aqui, asi el equipo ajusta la dificultad sin tocar codigo.
    /// Estos son los valores que se afinan despues de las pruebas de juego.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDifficulty_Nivel", menuName = "Reloj/Difficulty/Level Difficulty Config")]
    public class LevelDifficultyConfig : ScriptableObject
    {
        // ---------- Nivel cargado ----------
        /// <summary>Config del nivel cargado, o null (escenas de prueba / sin config: cada componente usa sus valores propios).</summary>
        public static LevelDifficultyConfig Current { get; private set; }
        static Object currentOwner;

        // Evita estado viejo si el Editor tiene desactivado "Reload Domain".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; currentOwner = null; }

        /// <summary>Lo llama el controller del nivel en Awake (null = sin config; limpia la del nivel anterior).</summary>
        public static void SetCurrent(LevelDifficultyConfig config, Object owner)
        {
            Current = config;
            currentOwner = owner;
        }

        /// <summary>Solo libera si <paramref name="owner"/> sigue siendo quien la registro (un Reiniciar no borra la del nivel nuevo).</summary>
        public static void ReleaseCurrent(Object owner)
        {
            if (currentOwner != owner) return;
            Current = null;
            currentOwner = null;
        }

        // ---------- Identificacion ----------
        [Tooltip("Numero de nivel (1 a 4). Solo informativo: sirve para ordenar y para reconocer el asset.")]
        [SerializeField, Range(1, 4)] int level = 1;

        // ---------- Enemigos ----------
        [Header("Enemigos")]
        [Tooltip("Velocidad de patrulla de los enemigos (unidades por segundo). Mas alto = recorren la ruta mas rapido y es mas dificil esquivarlos.")]
        [SerializeField, Min(0.1f)] float enemySpeed = 2f;

        [Tooltip("Apertura del cono de vision, en grados. Mas alto = el enemigo ve un abanico mas ancho y hay menos puntos ciegos.")]
        [SerializeField, Range(10f, 180f)] float viewAngle = 70f;

        [Tooltip("Alcance del cono de vision, en unidades. Mas alto = te detecta desde mas lejos.")]
        [SerializeField, Min(0.5f)] float viewDistance = 6f;

        [Tooltip("Segundos de visibilidad continua antes de pasar de Sospechoso a Alerta (te atrapa). Mas bajo = menos margen para esconderte.")]
        [SerializeField, Min(0.1f)] float timeToAlert = 2f;

        [Tooltip("Cantidad de enemigos recomendada para este nivel. Es una guia para quien edita la escena: NO instancia ni borra enemigos.")]
        [SerializeField, Min(0)] int suggestedEnemyCount = 5;

        // ---------- Parada Temporal ----------
        [Header("Parada Temporal (multiplican los valores base del TimeStopController)")]
        [Tooltip("Multiplicador de la duracion de la parada. 1 = valor base; 0.8 = dura un 20% menos. Mas bajo = menos tiempo para actuar.")]
        [SerializeField, Range(0.25f, 2f)] float timeStopDurationMultiplier = 1f;

        [Tooltip("Multiplicador del cooldown de la parada. 1 = valor base; 1.25 = tarda un 25% mas en recargar. Mas alto = puedes usarla menos veces.")]
        [SerializeField, Range(0.25f, 3f)] float timeStopCooldownMultiplier = 1f;

        public int Level => level;
        public float EnemySpeed => enemySpeed;
        public float ViewAngle => viewAngle;
        public float ViewDistance => viewDistance;
        public float TimeToAlert => timeToAlert;
        public int SuggestedEnemyCount => suggestedEnemyCount;
        public float TimeStopDurationMultiplier => timeStopDurationMultiplier;
        public float TimeStopCooldownMultiplier => timeStopCooldownMultiplier;
    }
}

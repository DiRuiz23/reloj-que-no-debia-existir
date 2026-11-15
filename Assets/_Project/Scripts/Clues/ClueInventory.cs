using System;
using System.Collections.Generic;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Clues
{
    /// <summary>
    /// Inventario de pistas (I-27). Vive como componente en GameServices (DontDestroyOnLoad), por eso
    /// conserva las pistas al cambiar de nivel. Una pista entra cuando se activa su flag de narrativa
    /// (<see cref="ClueData.Flag"/>) o con <see cref="Add"/>; nunca se agrega dos veces.
    /// Para el guardado (I-34): <see cref="GetIds"/> / <see cref="RestoreIds"/>.
    /// </summary>
    public class ClueInventory : MonoBehaviour
    {
        [Tooltip("Todas las pistas del juego. Se usa para resolver ids (guardado) y flags.")]
        [SerializeField] ClueData[] catalog = new ClueData[0];

        readonly List<ClueData> obtained = new List<ClueData>();

        /// <summary>Se dispara solo cuando una pista NUEVA entra durante el juego (para el aviso en pantalla).</summary>
        public event Action<ClueData> OnClueAdded;
        /// <summary>Se dispara ante cualquier cambio de la lista (agregar, reiniciar, reconstruir, cargar).</summary>
        public event Action OnChanged;

        public IReadOnlyList<ClueData> Clues => obtained;
        public IReadOnlyList<ClueData> Catalog => catalog;
        public int Count => obtained.Count;

        /// <summary>Atajo: el inventario de GameServices (null si no hay GameServices, p. ej. escena de prueba directa).</summary>
        public static ClueInventory Current =>
            GameServices.Instance != null ? GameServices.Instance.Clues : null;

        void OnEnable() { DialogueFlagManager.OnFlagChanged += HandleFlag; }
        void OnDisable() { DialogueFlagManager.OnFlagChanged -= HandleFlag; }

        public bool Has(string clueId) => obtained.Exists(c => c != null && c.Id == clueId);
        public bool Has(ClueData clue) => clue != null && Has(clue.Id);

        /// <summary>Agrega la pista y avisa. Devuelve false si es nula o ya estaba (no se duplica).</summary>
        public bool Add(ClueData clue)
        {
            if (clue == null || Has(clue)) return false;
            obtained.Add(clue);
            OnClueAdded?.Invoke(clue);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Partida nueva: vacia el inventario.</summary>
        public void Clear()
        {
            if (obtained.Count == 0) return;
            obtained.Clear();
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Reconstruye la lista desde los flags actuales (sin avisos). Se llama al iniciar/reiniciar un nivel,
        /// despues de que el nivel limpie sus flags locales: las pistas de niveles previos se conservan
        /// (sus flags siguen activos) y las del nivel reiniciado se pierden junto con su flag.
        /// </summary>
        public void RebuildFromFlags()
        {
            obtained.Clear();
            foreach (var c in catalog)
                if (c != null && DialogueFlagManager.GetFlag(c.Flag) && !Has(c)) obtained.Add(c);
            OnChanged?.Invoke();
        }

        // ---- Guardado (I-34) ----
        public List<string> GetIds() => obtained.ConvertAll(c => c.Id);

        /// <summary>Reemplaza el inventario por los ids guardados (ignora ids desconocidos).</summary>
        public void RestoreIds(IEnumerable<string> ids)
        {
            obtained.Clear();
            if (ids != null)
                foreach (var id in ids)
                {
                    var c = Find(id);
                    if (c != null && !Has(c)) obtained.Add(c);
                    else if (c == null) Debug.LogWarning($"[Clues] Id de pista desconocido al cargar: '{id}'.");
                }
            OnChanged?.Invoke();
        }

        public ClueData Find(string clueId)
        {
            foreach (var c in catalog) if (c != null && c.Id == clueId) return c;
            return null;
        }

        void HandleFlag(string flag, bool value)
        {
            if (!value) return;
            foreach (var c in catalog)
                if (c != null && c.Flag == flag) Add(c);
        }
    }
}

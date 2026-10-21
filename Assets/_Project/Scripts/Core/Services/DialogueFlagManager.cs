using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Flags de narrativa (decisiones del jugador). Vive como componente en GameServices (I-10),
    /// pero el estado y la API son estaticos para poder consultarlos desde cualquier script:
    /// <c>DialogueFlagManager.GetFlag("pista_dia20_obtenida")</c>.
    /// Tambien funciona en escenas de prueba que no pasan por Boot.
    /// </summary>
    public class DialogueFlagManager : MonoBehaviour
    {
        static readonly Dictionary<string, bool> flags = new Dictionary<string, bool>();

        /// <summary>Se dispara cuando un flag cambia de valor (nombre, nuevo valor).</summary>
        public static event Action<string, bool> OnFlagChanged;

        public static void SetFlag(string key, bool value = true)
        {
            if (string.IsNullOrEmpty(key)) return;
            bool changed = !flags.TryGetValue(key, out var old) || old != value;
            flags[key] = value;
            if (changed) OnFlagChanged?.Invoke(key, value);
        }

        public static bool GetFlag(string key) =>
            !string.IsNullOrEmpty(key) && flags.TryGetValue(key, out var v) && v;

        public static bool HasFlag(string key) =>
            !string.IsNullOrEmpty(key) && flags.ContainsKey(key);

        public static void Clear() => flags.Clear();

        /// <summary>Copia de todos los flags (para el guardado, I-34).</summary>
        public static Dictionary<string, bool> Snapshot() => new Dictionary<string, bool>(flags);

        /// <summary>Reemplaza los flags por los cargados de una partida (I-34).</summary>
        public static void Restore(IDictionary<string, bool> data)
        {
            flags.Clear();
            if (data == null) return;
            foreach (var kv in data) flags[kv.Key] = kv.Value;
        }

        // Evita estado viejo si el Editor tiene desactivado "Reload Domain".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            flags.Clear();
            OnFlagChanged = null;
        }
    }
}

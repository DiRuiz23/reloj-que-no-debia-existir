using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    [Serializable] public class ClueEntry { public string id, flag, title, summary; public int level; }
    [Serializable] class ClueCatalogFile { public List<ClueEntry> clues = new List<ClueEntry>(); }

    /// <summary>
    /// PREPARACION PARA EL INVENTARIO (I-27). Una pista = un flag de narrativa listado en
    /// Resources/Clues/clues_catalog.json. Este tracker registra las pistas obtenidas y avisa con
    /// <see cref="OnClueObtained"/>. I-27 solo debe: suscribirse al evento, leer <see cref="Obtained"/>
    /// al abrir el inventario y (para guardar) persistir los ids con <see cref="ObtainedIds"/>.
    /// Para agregar una pista nueva: anadir el flag a _flags.json y una entrada al catalogo.
    /// </summary>
    public static class ClueTracker
    {
        public const string ResourcesPath = "Clues/clues_catalog";
        public static event Action<ClueEntry> OnClueObtained;

        static ClueCatalogFile catalog;
        static readonly List<ClueEntry> obtained = new List<ClueEntry>();
        static bool subscribed;

        public static IReadOnlyList<ClueEntry> Obtained => obtained;
        public static IReadOnlyList<ClueEntry> Catalog { get { Load(); return catalog.clues; } }
        public static bool Has(string clueId) => obtained.Exists(c => c.id == clueId);
        public static List<string> ObtainedIds() => obtained.ConvertAll(c => c.id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { catalog = null; obtained.Clear(); subscribed = false; OnClueObtained = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (subscribed) return;
            subscribed = true;
            DialogueFlagManager.OnFlagChanged += OnFlag;
        }

        static void Load()
        {
            if (catalog != null) return;
            var ta = Resources.Load<TextAsset>(ResourcesPath);
            catalog = ta != null ? JsonUtility.FromJson<ClueCatalogFile>(ta.text) : null;
            if (catalog == null) { catalog = new ClueCatalogFile(); Debug.LogWarning("[Clues] Falta Resources/" + ResourcesPath + ".json"); }
        }

        static void OnFlag(string flag, bool value)
        {
            if (!value) return;
            Load();
            foreach (var c in catalog.clues)
                if (c.flag == flag && !Has(c.id)) { obtained.Add(c); OnClueObtained?.Invoke(c); }
        }

        /// <summary>Reconstruye la lista desde los flags actuales (tras cargar partida o reiniciar nivel).</summary>
        public static void Rebuild()
        {
            Load();
            obtained.Clear();
            foreach (var c in catalog.clues)
                if (DialogueFlagManager.GetFlag(c.flag)) obtained.Add(c);
        }
    }
}

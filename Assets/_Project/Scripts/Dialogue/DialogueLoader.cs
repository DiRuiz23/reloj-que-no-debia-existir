using UnityEngine;

namespace Reloj.Dialogue
{
    /// <summary>
    /// Lectura de dialogos desde TextAsset. Usa JsonUtility y Resources: funciona igual en
    /// Editor, WebGL y Android (sin File.ReadAllText ni StreamingAssets).
    /// </summary>
    public static class DialogueLoader
    {
        public const string ResourcesFolder = "Dialogues";
        public const string FlagCatalogName = "_flags";

        public static DialogueFile Parse(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[Dialogue] TextAsset nulo.");
                return null;
            }
            return ParseJson(asset.text, asset.name);
        }

        public static DialogueFile ParseJson(string json, string sourceName = "json")
        {
            try
            {
                var file = JsonUtility.FromJson<DialogueFile>(json);
                if (file == null || file.nodes == null || file.nodes.Count == 0)
                {
                    Debug.LogError($"[Dialogue] '{sourceName}' no tiene nodos o el JSON es invalido.");
                    return null;
                }
                return file;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Dialogue] JSON invalido en '{sourceName}': {e.Message}");
                return null;
            }
        }

        /// <summary>Carga Resources/Dialogues/{id}.json</summary>
        public static DialogueFile LoadFromResources(string id)
        {
            var asset = Resources.Load<TextAsset>($"{ResourcesFolder}/{id}");
            if (asset == null)
            {
                Debug.LogError($"[Dialogue] No existe Resources/{ResourcesFolder}/{id}.json");
                return null;
            }
            return Parse(asset);
        }

        public static DialogueFlagCatalog LoadFlagCatalog()
        {
            var asset = Resources.Load<TextAsset>($"{ResourcesFolder}/{FlagCatalogName}");
            if (asset == null) return new DialogueFlagCatalog();
            try { return JsonUtility.FromJson<DialogueFlagCatalog>(asset.text) ?? new DialogueFlagCatalog(); }
            catch { return new DialogueFlagCatalog(); }
        }
    }
}

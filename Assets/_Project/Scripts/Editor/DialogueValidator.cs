using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Reloj.Dialogue.EditorTools
{
    /// <summary>
    /// Tools > Validar diálogos. Revisa todos los .json de Resources/Dialogues:
    /// nodos sin continuación, destinos inexistentes, flags no definidos, ids repetidos, etc.
    /// </summary>
    public static class DialogueValidator
    {
        const string Folder = "Assets/Resources/Dialogues";
        const string CatalogPath = Folder + "/_flags.json";

        static int errors;
        static int warnings;

        [MenuItem("Tools/Validar diálogos")]
        public static void ValidateAll() => Run(true);

        /// <summary>Valida todo. showDialog=false para correrlo desde scripts/CI. Devuelve el número de errores.</summary>
        public static int Run(bool showDialog)
        {
            errors = 0;
            warnings = 0;
            int files = 0;

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                if (showDialog) EditorUtility.DisplayDialog("Validar diálogos", $"No existe la carpeta {Folder}", "OK");
                return 1;
            }

            var flags = LoadFlags();

            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json") || path == CatalogPath) continue;

                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null) continue;
                files++;
                ValidateFile(asset, flags);
            }

            string summary = $"{files} archivo(s) revisado(s): {errors} error(es), {warnings} advertencia(s).";
            if (errors > 0) Debug.LogError($"[Validar diálogos] {summary}");
            else Debug.Log($"[Validar diálogos] {summary}");
            if (showDialog) EditorUtility.DisplayDialog("Validar diálogos", summary + "\nRevisa la consola para ver el detalle.", "OK");
            return errors;
        }

        static HashSet<string> LoadFlags()
        {
            var catalogAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            if (catalogAsset == null)
            {
                Warn(null, $"No existe {CatalogPath}: no se pueden validar los flags.");
                return null;
            }
            try
            {
                var catalog = JsonUtility.FromJson<DialogueFlagCatalog>(catalogAsset.text);
                return new HashSet<string>(catalog?.flags ?? new List<string>());
            }
            catch (System.Exception e)
            {
                Error(catalogAsset, $"_flags.json inválido: {e.Message}");
                return null;
            }
        }

        static void ValidateFile(TextAsset asset, HashSet<string> flags)
        {
            DialogueFile file;
            try { file = JsonUtility.FromJson<DialogueFile>(asset.text); }
            catch (System.Exception e)
            {
                Error(asset, $"{asset.name}: JSON inválido ({e.Message})");
                return;
            }

            if (file == null || file.nodes == null || file.nodes.Count == 0)
            {
                Error(asset, $"{asset.name}: no tiene nodos o el JSON es inválido.");
                return;
            }

            // ids únicos
            var ids = new HashSet<string>();
            foreach (var n in file.nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.id))
                {
                    Error(asset, $"{asset.name}: hay un nodo sin 'id'.");
                    continue;
                }
                if (!ids.Add(n.id))
                    Error(asset, $"{asset.name}: id de nodo repetido '{n.id}'.");
            }

            if (!string.IsNullOrEmpty(file.start) && !ids.Contains(file.start))
                Error(asset, $"{asset.name}: 'start' apunta a '{file.start}', que no existe.");

            foreach (var n in file.nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.id)) continue;
                string where = $"{asset.name} > nodo '{n.id}'";

                if (string.IsNullOrWhiteSpace(n.text))
                    Warn(asset, $"{where}: sin texto.");

                bool hasChoices = n.choices != null && n.choices.Count > 0;

                if (!n.end && !hasChoices)
                    Error(asset, $"{where}: sin continuación (no tiene opciones ni \"end\": true).");
                if (n.end && hasChoices)
                    Warn(asset, $"{where}: tiene \"end\": true y opciones; las opciones se ignoran.");
                if (!n.end && hasChoices && n.choices.Count < 2)
                    Warn(asset, $"{where}: solo tiene 1 opción (se piden 2 o más).");

                if (hasChoices)
                {
                    var targets = new HashSet<string>();
                    for (int i = 0; i < n.choices.Count; i++)
                    {
                        var c = n.choices[i];
                        string cw = $"{where}, opción {i + 1}";
                        if (c == null) { Error(asset, $"{cw}: vacía."); continue; }

                        if (string.IsNullOrWhiteSpace(c.text))
                            Warn(asset, $"{cw}: sin texto.");

                        if (string.IsNullOrEmpty(c.next))
                            Error(asset, $"{cw}: sin 'next' (no lleva a ningún nodo).");
                        else if (!ids.Contains(c.next))
                            Error(asset, $"{cw}: apunta a '{c.next}', que no existe.");
                        else if (!targets.Add(c.next))
                            Warn(asset, $"{cw}: lleva al mismo nodo ('{c.next}') que otra opción.");

                        CheckFlag(asset, cw, c.setFlag, flags);
                    }
                }

                if (n.altIf != null && n.altIf.IsSet)
                {
                    string aw = $"{where} (altIf)";
                    CheckFlag(asset, aw, n.altIf.flag, flags);
                    if (string.IsNullOrEmpty(n.altIf.next) || !ids.Contains(n.altIf.next))
                        Error(asset, $"{aw}: 'next' apunta a '{n.altIf.next}', que no existe.");
                }
            }

            CheckReachability(asset, file, ids);
        }

        static void CheckReachability(TextAsset asset, DialogueFile file, HashSet<string> ids)
        {
            string start = !string.IsNullOrEmpty(file.start) ? file.start : file.nodes[0]?.id;
            if (string.IsNullOrEmpty(start) || !ids.Contains(start)) return;

            var reached = new HashSet<string> { start };
            var stack = new Stack<string>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var node = file.GetNode(stack.Pop());
                if (node == null) continue;

                if (node.choices != null)
                    foreach (var c in node.choices)
                        if (c != null) Visit(c.next, ids, reached, stack);

                if (node.altIf != null && node.altIf.IsSet)
                    Visit(node.altIf.next, ids, reached, stack);
            }

            foreach (var id in ids)
                if (!reached.Contains(id))
                    Warn(asset, $"{asset.name} > nodo '{id}': inalcanzable desde el inicio.");
        }

        static void Visit(string id, HashSet<string> ids, HashSet<string> reached, Stack<string> stack)
        {
            if (!string.IsNullOrEmpty(id) && ids.Contains(id) && reached.Add(id))
                stack.Push(id);
        }

        static void CheckFlag(Object ctx, string where, string flag, HashSet<string> catalog)
        {
            if (string.IsNullOrEmpty(flag) || catalog == null) return;
            if (!catalog.Contains(flag))
                Error(ctx, $"{where}: flag no definido '{flag}' (agrégalo a _flags.json).");
        }

        static void Error(Object ctx, string msg) { errors++; Debug.LogError($"[Validar diálogos] {msg}", ctx); }
        static void Warn(Object ctx, string msg) { warnings++; Debug.LogWarning($"[Validar diálogos] {msg}", ctx); }
    }
}

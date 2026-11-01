using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Reloj.UI;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Tools > Verificar licencias
    /// 1) Falla si una carpeta de ThirdParty o cualquier recurso externo (imagen, modelo, audio, fuente, video)
    ///    no está cubierto por un SOURCE.txt, o si un SOURCE.txt no tiene Autor, URL y Licencia.
    /// 2) Si todo está bien, regenera la lista de recursos de CreditsData a partir de los SOURCE.txt (100 %).
    /// Para CI:  -executeMethod Reloj.EditorTools.LicenseVerifier.RunBatch
    /// </summary>
    public static class LicenseVerifier
    {
        const string CreditsFolder = "Assets/_Project/Data/Credits";
        const string CreditsPath = CreditsFolder + "/CreditsData.asset";
        const string ThirdPartyPath = "Assets/ThirdParty";
        const string SourceFileName = "SOURCE.txt";

        // Carpetas que no son recursos del juego (plantilla de Unity, capturas, recuperación de escenas).
        static readonly string[] ExcludedRoots =
        {
            "Assets/TutorialInfo", "Assets/Screenshots", "Assets/_Recovery"
        };

        // Extensiones que se consideran "recurso externo" y por lo tanto exigen cita y licencia.
        static readonly HashSet<string> ExternalExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".bmp", ".gif", ".exr", ".hdr", ".svg",
            ".fbx", ".obj", ".blend", ".dae", ".gltf", ".glb",
            ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".flac",
            ".ttf", ".otf",
            ".mp4", ".mov", ".webm"
        };

        class SourceEntry
        {
            public string folder;   // carpeta del SOURCE.txt (relativa al proyecto, con "/")
            public string name, author, url, license, tool, usage;
        }

        // ------------------------------------------------------------------ Entradas
        [MenuItem("Tools/Verificar licencias")]
        public static void RunFromMenu()
        {
            var errors = Run(out string summary);
            if (errors.Count > 0)
            {
                Debug.LogError("[Licencias] FALLÓ la verificación (" + errors.Count + " problemas):\n- " + string.Join("\n- ", errors));
                EditorUtility.DisplayDialog("Verificar licencias",
                    "FALLÓ: " + errors.Count + " problema(s).\n\n" + string.Join("\n", errors.Take(8)) +
                    (errors.Count > 8 ? "\n… (ver la consola)" : ""), "Entendido");
            }
            else
            {
                Debug.Log("[Licencias] OK. " + summary);
                EditorUtility.DisplayDialog("Verificar licencias", "Todo en orden.\n\n" + summary, "Listo");
            }
        }

        /// <summary>Para CI / línea de comandos: sale con código 1 si hay errores.</summary>
        public static void RunBatch()
        {
            var errors = Run(out string summary);
            if (errors.Count > 0)
            {
                Debug.LogError("[Licencias] FALLÓ:\n- " + string.Join("\n- ", errors));
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[Licencias] OK. " + summary);
                EditorApplication.Exit(0);
            }
        }

        // ------------------------------------------------------------------ Lógica
        /// <summary>Devuelve la lista de errores (vacía = correcto). Si no hay errores, sincroniza CreditsData.</summary>
        public static List<string> Run(out string summary)
        {
            var errors = new List<string>();
            string root = Directory.GetParent(Application.dataPath).FullName;

            // 1) Leer todos los SOURCE.txt
            var entries = new List<SourceEntry>();
            foreach (var file in Directory.GetFiles(Application.dataPath, SourceFileName, SearchOption.AllDirectories))
            {
                string folder = ToProjectPath(root, Path.GetDirectoryName(file));
                var e = Parse(File.ReadAllText(file));
                e.folder = folder;
                if (string.IsNullOrEmpty(e.name)) e.name = Path.GetFileName(folder);

                var missing = new List<string>();
                if (IsBlank(e.author)) missing.Add("Autor");
                if (IsBlank(e.url)) missing.Add("URL");
                if (IsBlank(e.license)) missing.Add("Licencia");
                if (missing.Count > 0)
                    errors.Add(folder + "/" + SourceFileName + ": falta " + string.Join(", ", missing));
                entries.Add(e);
            }
            var sourceFolders = new HashSet<string>(entries.Select(e => e.folder));

            // 2) Cada subcarpeta de ThirdParty debe tener su propio SOURCE.txt
            string tpFull = Path.Combine(root, ThirdPartyPath);
            if (Directory.Exists(tpFull))
                foreach (var dir in Directory.GetDirectories(tpFull))
                {
                    string d = ToProjectPath(root, dir);
                    if (!sourceFolders.Contains(d))
                        errors.Add(d + ": carpeta de ThirdParty sin " + SourceFileName);
                }

            // 3) Todo recurso externo debe estar cubierto por un SOURCE.txt en su carpeta o en una carpeta superior
            var uncovered = new Dictionary<string, int>();
            int checkedFiles = 0;
            foreach (var file in Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (!ExternalExtensions.Contains(Path.GetExtension(file))) continue;

                string dir = ToProjectPath(root, Path.GetDirectoryName(file));
                if (ExcludedRoots.Any(x => dir == x || dir.StartsWith(x + "/"))) continue;
                checkedFiles++;

                if (!FindCoveringFolder(dir, sourceFolders, out _))
                {
                    uncovered.TryGetValue(dir, out int n);
                    uncovered[dir] = n + 1;
                }
            }
            foreach (var kv in uncovered.OrderBy(k => k.Key))
                errors.Add(kv.Key + ": " + kv.Value + " recurso(s) sin " + SourceFileName);

            // 4) CreditsData
            var data = AssetDatabase.LoadAssetAtPath<CreditsData>(CreditsPath);
            if (data == null) data = CreateCreditsAsset();
            if (data.authors == null || data.authors.Count == 0) errors.Add("CreditsData: no tiene autores.");
            if (data.tools == null || data.tools.Count == 0) errors.Add("CreditsData: no tiene herramientas.");

            if (errors.Count > 0)
            {
                summary = "";
                return errors;
            }

            // 5) Sincronizar: CreditsData.assets = 100 % de los SOURCE.txt
            data.assets = entries
                .OrderBy(e => e.folder, StringComparer.OrdinalIgnoreCase)
                .Select(e => new CreditAsset
                {
                    name = e.name, author = e.author, source = e.url, license = e.license,
                    tool = e.tool, usage = e.usage, sourcePath = e.folder
                }).ToList();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            summary = entries.Count + " SOURCE.txt, " + checkedFiles + " archivos de recurso cubiertos. " +
                      "CreditsData actualizado con " + data.assets.Count + " recursos externos.";
            return errors;
        }

        // ------------------------------------------------------------------ Utilidades
        static bool FindCoveringFolder(string dir, HashSet<string> sourceFolders, out string found)
        {
            // Sube desde la carpeta del archivo hasta justo debajo de "Assets" (un SOURCE.txt en Assets/ no cuenta).
            while (!string.IsNullOrEmpty(dir) && dir != "Assets")
            {
                if (sourceFolders.Contains(dir)) { found = dir; return true; }
                int i = dir.LastIndexOf('/');
                dir = i < 0 ? null : dir.Substring(0, i);
            }
            found = null;
            return false;
        }

        static string ToProjectPath(string root, string fullPath)
        {
            string rel = fullPath.Substring(root.Length).TrimStart('\\', '/');
            return rel.Replace('\\', '/');
        }

        static bool IsBlank(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return true;
            string u = v.Trim().ToUpperInvariant();
            return u.StartsWith("TODO") || u.StartsWith("PENDIENTE") || u == "?" || u == "-";
        }

        static string Normalize(string key)
        {
            var d = key.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString();
        }

        static SourceEntry Parse(string text)
        {
            var e = new SourceEntry();
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || char.IsWhiteSpace(line[0])) continue;   // solo campos en la columna 0
                int idx = line.IndexOf(':');
                if (idx <= 0) continue;
                string key = Normalize(line.Substring(0, idx));
                string val = line.Substring(idx + 1).Trim();
                switch (key)
                {
                    case "nombre": case "fuente": e.name = val; break;
                    case "autor": case "autores": e.author = val; break;
                    case "url": case "enlace": case "origen": e.url = val; break;
                    case "licencia": e.license = val; break;
                    case "herramienta": case "herramientas": e.tool = val; break;
                    case "uso": case "uso en el proyecto": e.usage = val; break;
                }
            }
            return e;
        }

        static CreditsData CreateCreditsAsset()
        {
            if (!AssetDatabase.IsValidFolder(CreditsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Data")) AssetDatabase.CreateFolder("Assets/_Project", "Data");
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Credits");
            }
            var data = ScriptableObject.CreateInstance<CreditsData>();
            AssetDatabase.CreateAsset(data, CreditsPath);
            return data;
        }
    }
}

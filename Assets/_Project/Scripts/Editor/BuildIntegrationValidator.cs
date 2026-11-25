using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Reloj.Clues;
using Reloj.Dialogue.EditorTools;
using Reloj.UI;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Validador de integracion (I-41): comprueba que EditorBuildSettings, los 3 Build Profiles
    /// (Windows, Web, Android), GameService.prefab (catalogo de pistas) y los dialogos esten
    /// alineados y sin errores antes de generar builds o ejecutar la corrida completa.
    /// </summary>
    public static class BuildIntegrationValidator
    {
        public static readonly string[] CanonicalScenes =
        {
            "Assets/_Project/Scenes/Boot.unity",
            "Assets/_Project/Scenes/Splash.unity",
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Level01_Art.unity",
            "Assets/_Project/Scenes/Level01_Gameplay.unity",
            "Assets/_Project/Scenes/Level02_Art.unity",
            "Assets/_Project/Scenes/Level02_Gameplay.unity",
            "Assets/_Project/Scenes/Level03_Art.unity",
            "Assets/_Project/Scenes/Level03_Gameplay.unity",
            "Assets/_Project/Scenes/Level04_Art.unity",
            "Assets/_Project/Scenes/Level04_Gameplay.unity",
            "Assets/_Project/Scenes/Decision_Final.unity",
            "Assets/_Project/Scenes/Ending_A.unity",
            "Assets/_Project/Scenes/Ending_B.unity",
        };

        private static readonly string[] BuildProfilePaths =
        {
            "Assets/Settings/Build Profiles/Windows.asset",
            "Assets/Settings/Build Profiles/Web.asset",
            "Assets/Settings/Build Profiles/Android™.asset",
        };

        private const string GameServicePrefabPath = "Assets/_Project/Prefabs/Core/GameService.prefab";
        private const string CluesFolder = "Assets/_Project/Data/Clues";

        private static readonly string[] UiPrefabPaths =
        {
            "Assets/_Project/Prefabs/UI/GameStateUI.prefab",
            "Assets/_Project/Prefabs/UI/DialogueUI.prefab",
            "Assets/_Project/Prefabs/UI/TouchControls.prefab",
            "Assets/_Project/Prefabs/UI/HUD_Game.prefab",
        };

        [MenuItem("Tools/Integración (I-41)/Validar build y escenas")]
        public static void ValidateMenu() => Validate(showDialog: true);

        /// <summary>Entrada para CI / batchmode (-executeMethod).</summary>
        public static void ValidateForCI()
        {
            int errors = Validate(showDialog: false);
            if (Application.isBatchMode)
                EditorApplication.Exit(errors == 0 ? 0 : 1);
            else if (errors > 0)
                throw new InvalidOperationException($"[I-41] Validacion fallida con {errors} error(es).");
        }

        public static int Validate(bool showDialog)
        {
            int errors = 0;

            errors += ValidateEditorBuildSettings();
            foreach (var profilePath in BuildProfilePaths)
                errors += ValidateBuildProfile(profilePath);
            errors += ValidateGameServiceClues();
            errors += ValidateUiPrefabs();
            errors += DialogueValidator.Run(false);

            string summary = errors == 0
                ? $"Validación I-41 exitosa: {CanonicalScenes.Length} escenas en orden en EditorBuildSettings y 3 Build Profiles, pistas y diálogos OK."
                : $"Validación I-41 fallida con {errors} error(es). Revisa la consola.";

            if (errors > 0) Debug.LogError($"[I-41] {summary}");
            else Debug.Log($"[I-41] {summary}");

            if (showDialog)
                EditorUtility.DisplayDialog("Validar build y escenas (I-41)", summary, "OK");

            return errors;
        }

        private static int ValidateEditorBuildSettings()
        {
            int errors = 0;
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length != CanonicalScenes.Length)
            {
                Debug.LogError($"[I-41] EditorBuildSettings tiene {scenes.Length} escenas; se esperaban {CanonicalScenes.Length}.");
                errors++;
            }

            int count = Mathf.Min(scenes.Length, CanonicalScenes.Length);
            for (int i = 0; i < count; i++)
            {
                if (!scenes[i].enabled)
                {
                    Debug.LogError($"[I-41] EditorBuildSettings escena #{i} ({scenes[i].path}) está deshabilitada.");
                    errors++;
                }
                if (!string.Equals(scenes[i].path, CanonicalScenes[i], StringComparison.Ordinal))
                {
                    Debug.LogError($"[I-41] EditorBuildSettings escena #{i}: esperada '{CanonicalScenes[i]}', encontrada '{scenes[i].path}'.");
                    errors++;
                }
                if (!File.Exists(CanonicalScenes[i]))
                {
                    Debug.LogError($"[I-41] No existe el archivo de escena '{CanonicalScenes[i]}'.");
                    errors++;
                }
            }
            return errors;
        }

        private static int ValidateBuildProfile(string profilePath)
        {
            if (!File.Exists(profilePath))
            {
                Debug.LogError($"[I-41] No existe el Build Profile '{profilePath}'.");
                return 1;
            }

            var entries = ParseProfileScenes(profilePath);
            int errors = 0;
            if (entries.Count != CanonicalScenes.Length)
            {
                Debug.LogError($"[I-41] Build Profile '{profilePath}' tiene {entries.Count} escenas; se esperaban {CanonicalScenes.Length}.");
                errors++;
            }

            int count = Mathf.Min(entries.Count, CanonicalScenes.Length);
            for (int i = 0; i < count; i++)
            {
                if (!entries[i].enabled)
                {
                    Debug.LogError($"[I-41] '{profilePath}' escena #{i} ({entries[i].path}) está deshabilitada.");
                    errors++;
                }
                if (!string.Equals(entries[i].path, CanonicalScenes[i], StringComparison.Ordinal))
                {
                    Debug.LogError($"[I-41] '{profilePath}' escena #{i}: esperada '{CanonicalScenes[i]}', encontrada '{entries[i].path}'.");
                    errors++;
                }
            }
            return errors;
        }

        private static List<(bool enabled, string path)> ParseProfileScenes(string profilePath)
        {
            var result = new List<(bool enabled, string path)>();
            var lines = File.ReadAllLines(profilePath);
            bool inScenes = false;
            bool currentEnabled = false;
            bool hasCurrent = false;

            foreach (var raw in lines)
            {
                if (!inScenes)
                {
                    if (raw.Trim() == "m_Scenes:") inScenes = true;
                    continue;
                }

                if (raw.StartsWith("  m_") && !raw.StartsWith("  m_Scenes"))
                    break;

                string trimmed = raw.Trim();
                if (trimmed.StartsWith("- m_enabled:"))
                {
                    currentEnabled = trimmed.EndsWith("1");
                    hasCurrent = true;
                }
                else if (hasCurrent && trimmed.StartsWith("m_path:"))
                {
                    string path = trimmed.Substring("m_path:".Length).Trim();
                    result.Add((currentEnabled, path));
                    hasCurrent = false;
                }
            }
            return result;
        }

        private static int ValidateGameServiceClues()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameServicePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[I-41] No se pudo cargar '{GameServicePrefabPath}'.");
                return 1;
            }

            var inv = prefab.GetComponent<ClueInventory>();
            if (inv == null)
            {
                Debug.LogError($"[I-41] '{GameServicePrefabPath}' no tiene componente ClueInventory.");
                return 1;
            }

            int errors = 0;
            var catalogSet = new HashSet<ClueData>();
            foreach (var c in inv.Catalog)
            {
                if (c == null)
                {
                    Debug.LogError($"[I-41] '{GameServicePrefabPath}' tiene una entrada nula en ClueInventory.catalog.");
                    errors++;
                }
                else
                {
                    catalogSet.Add(c);
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:ClueData", new[] { CluesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clue = AssetDatabase.LoadAssetAtPath<ClueData>(path);
                if (clue != null && !catalogSet.Contains(clue))
                {
                    Debug.LogError($"[I-41] Falta registrar '{clue.name}' ({path}) en GameService.prefab > ClueInventory.catalog.");
                    errors++;
                }
            }

            return errors;
        }

        private static int ValidateUiPrefabs()
        {
            int errors = 0;
            var idPattern = new Regex(@"(?:&|fileID:\s*)(\d{18,})");

            foreach (var path in UiPrefabPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"[I-41] No existe el prefab UI '{path}'.");
                    errors++;
                    continue;
                }

                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match m in idPattern.Matches(lines[i]))
                    {
                        string num = m.Groups[1].Value;
                        if (!long.TryParse(num, out _))
                        {
                            Debug.LogError($"[I-41] '{path}:{i + 1}' tiene un fileID que desborda Int64 ({num}).");
                            errors++;
                        }
                    }
                }
            }

            var stateUiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPaths[0]);
            if (stateUiPrefab != null)
            {
                var stateUi = stateUiPrefab.GetComponent<GameStateUI>();
                var skillPanel = stateUiPrefab.GetComponentInChildren<SkillMenuPanel>(true);
                if (stateUi == null || skillPanel == null)
                {
                    Debug.LogError($"[I-41] '{UiPrefabPaths[0]}' no contiene GameStateUI o SkillMenuPanel.");
                    errors++;
                }
                else
                {
                    var so = new SerializedObject(skillPanel);
                    var backProp = so.FindProperty("backButton");
                    var backBtn = backProp != null ? backProp.objectReferenceValue as Component : null;
                    if (backBtn == null || !backBtn.transform.IsChildOf(skillPanel.transform))
                    {
                        Debug.LogError($"[I-41] SkillMenuPanel en '{UiPrefabPaths[0]}' no tiene backButton valido dentro de su jerarquia.");
                        errors++;
                    }
                }
            }

            return errors;
        }
    }
}

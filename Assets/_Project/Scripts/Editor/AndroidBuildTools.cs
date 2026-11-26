using System;
using System.IO;
using System.Text;
using Reloj.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Herramientas de configuracion, validacion y generacion de APK para Android (I-43).
    /// No depende de ensamblados de extension Android para compilar incluso antes de instalar el modulo en Unity Hub.
    /// </summary>
    public static class AndroidBuildTools
    {
        public const string ExpectedPackageName = "com.pixelnova.game1910";
        public const string DefaultBundleVersion = "1.0.0";
        private const string AndroidProfilePath = "Assets/Settings/Build Profiles/Android™.asset";
        private const string MobileRpAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
        private const string HudPrefabPath = "Assets/_Project/Prefabs/UI/HUD_Game.prefab";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string ReleaseApkPath = "Builds/Android/1910.apk";
        private const string DevApkPath = "Builds/Android/1910_Dev.apk";

        private static readonly string[] GameplayScenes =
        {
            "Assets/_Project/Scenes/Level01_Gameplay.unity",
            "Assets/_Project/Scenes/Level02_Gameplay.unity",
            "Assets/_Project/Scenes/Level03_Gameplay.unity",
            "Assets/_Project/Scenes/Level04_Gameplay.unity",
        };

        [MenuItem("Tools/Android (I-43)/Aplicar configuración Android recomendada", priority = 1)]
        public static void ApplyRecommendedSettings()
        {
            PlayerSettings.companyName = "PIXELNOVA GAME STUDIO";
            PlayerSettings.productName = "1910";
            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1.0")
                PlayerSettings.bundleVersion = DefaultBundleVersion;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ExpectedPackageName);
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);

            // Orientacion horizontal fija (permite Landscape Left y Landscape Right, bloquea Portrait).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.resizeableActivity = false;

            // Garantiza que el boton/gesto Atras llegue como Escape al Input System en Android 13+.
            PlayerSettings.Android.predictiveBackSupport = false;

            // Scripting y SDK recomendados para telefonos reales.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            if (PlayerSettings.Android.targetArchitectures == AndroidArchitecture.None)
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < (int)AndroidSdkVersions.AndroidApiLevel25)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.buildAppBundle = false;

            AssetDatabase.SaveAssets();
            Debug.Log("[I-43] Configuración recomendada de Android aplicada correctamente.");
        }

        [MenuItem("Tools/Android (I-43)/Arquitectura/Solo ARM64 (teléfonos modernos - recomendado)", priority = 20)]
        public static void SetArchArm64Only() => SetArchitecture(AndroidArchitecture.ARM64, "ARM64");

        [MenuItem("Tools/Android (I-43)/Arquitectura/ARM64 + ARMv7 (máxima compatibilidad real)", priority = 21)]
        public static void SetArchArm64AndArmv7() =>
            SetArchitecture(AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7, "ARM64 + ARMv7");

        [MenuItem("Tools/Android (I-43)/Arquitectura/ARM64 + x86_64 (teléfonos + emulador PC)", priority = 22)]
        public static void SetArchArm64AndX86_64() =>
            SetArchitecture(AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64, "ARM64 + x86_64");

        private static void SetArchitecture(AndroidArchitecture arch, string label)
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = arch;
            AssetDatabase.SaveAssets();
            Debug.Log($"[I-43] Arquitectura Android actualizada a: {label} (IL2CPP).");
        }

        [MenuItem("Tools/Android (I-43)/Validar configuración Android", priority = 40)]
        public static void ValidateMenu() => Validate(showDialog: true);

        /// <summary>Entrada para CI / batchmode (-executeMethod Reloj.EditorTools.AndroidBuildTools.ValidateForCI).</summary>
        public static void ValidateForCI()
        {
            int errors = Validate(showDialog: false);
            if (Application.isBatchMode)
                EditorApplication.Exit(errors == 0 ? 0 : 1);
            else if (errors > 0)
                throw new InvalidOperationException($"[I-43] Validacion Android fallida con {errors} error(es).");
        }

        public static int Validate(bool showDialog)
        {
            int errors = 0;
            int warnings = 0;
            var notes = new StringBuilder();

            errors += BuildIntegrationValidator.Validate(showDialog: false);

            // 1. Identidad y versionado
            string pkg = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (pkg != ExpectedPackageName)
            {
                Debug.LogError($"[I-43] Application Identifier Android esperado '{ExpectedPackageName}', actual '{pkg}'.");
                errors++;
            }
            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
            {
                Debug.LogError("[I-43] PlayerSettings.bundleVersion esta vacio.");
                errors++;
            }
            if (PlayerSettings.Android.bundleVersionCode < 1)
            {
                Debug.LogError($"[I-43] AndroidBundleVersionCode debe ser >= 1 (actual: {PlayerSettings.Android.bundleVersionCode}).");
                errors++;
            }

            // 2. Orientacion horizontal fija
            bool landscapeAllowed = PlayerSettings.allowedAutorotateToLandscapeLeft || PlayerSettings.allowedAutorotateToLandscapeRight;
            bool portraitAllowed = PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool validOrientation = PlayerSettings.defaultInterfaceOrientation == UIOrientation.LandscapeLeft
                || PlayerSettings.defaultInterfaceOrientation == UIOrientation.LandscapeRight
                || (PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation && landscapeAllowed && !portraitAllowed);

            if (!validOrientation)
            {
                Debug.LogError($"[I-43] La orientacion Android no esta bloqueada en horizontal (default={PlayerSettings.defaultInterfaceOrientation}, portrait={portraitAllowed}, landscape={landscapeAllowed}).");
                errors++;
            }
            if (PlayerSettings.Android.resizeableActivity)
            {
                Debug.LogError("[I-43] Android.resizeableActivity debe estar en false para mantener pantalla completa horizontal.");
                errors++;
            }

            // 3. Boton Atras
            if (PlayerSettings.Android.predictiveBackSupport)
            {
                Debug.LogError("[I-43] Android.predictiveBackSupport debe estar en false para interceptar Atrás como pausa en Android 13+.");
                errors++;
            }

            // 4. Scripting, arquitectura, ASTC y Build Profile
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            if (backend != ScriptingImplementation.IL2CPP)
            {
                Debug.LogWarning($"[I-43] Scripting Backend Android es {backend}; se recomienda IL2CPP con ARM64.");
                warnings++;
            }
            if (PlayerSettings.Android.targetArchitectures == AndroidArchitecture.None)
            {
                Debug.LogError("[I-43] No hay ninguna arquitectura objetivo seleccionada para Android.");
                errors++;
            }

            if (File.Exists(ProjectSettingsPath))
            {
                string psText = File.ReadAllText(ProjectSettingsPath);
                if (!psText.Contains("m_BuildTarget: Android") || !psText.Contains("m_Formats: 03000000"))
                {
                    Debug.LogError("[I-43] Default Texture Compression Format para Android no esta configurado en ASTC.");
                    errors++;
                }
            }

            if (File.Exists(AndroidProfilePath))
            {
                string profileText = File.ReadAllText(AndroidProfilePath);
                if (!profileText.Contains("m_BuildSubtarget: 6"))
                {
                    Debug.LogError("[I-43] El Build Profile Android™.asset no tiene Texture Compression = ASTC (m_BuildSubtarget: 6).");
                    errors++;
                }
                if (!profileText.Contains("m_BuildAppBundle: 0"))
                {
                    Debug.LogError("[I-43] El Build Profile Android™.asset tiene Build App Bundle activado; debe ser 0 para generar APK.");
                    errors++;
                }
            }
            else
            {
                Debug.LogError($"[I-43] No se encontro '{AndroidProfilePath}'.");
                errors++;
            }

            // 5. Calidad movil y Mobile_RPAsset (renderScale <= 0.85)
            string[] qualityNames = QualitySettings.names;
            if (qualityNames.Length == 0 || !string.Equals(qualityNames[0], "Baja", StringComparison.Ordinal))
            {
                Debug.LogWarning("[I-43] Se esperaba el nivel de calidad 'Baja' en el indice 0 para Android.");
                warnings++;
            }

            var mobileRp = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MobileRpAssetPath);
            if (mobileRp == null)
            {
                Debug.LogError($"[I-43] Falta '{MobileRpAssetPath}'.");
                errors++;
            }
            else
            {
                using var so = new SerializedObject(mobileRp);
                var renderScaleProp = so.FindProperty("m_RenderScale");
                if (renderScaleProp != null && renderScaleProp.floatValue > 0.85f)
                {
                    Debug.LogWarning($"[I-43] Mobile_RPAsset tiene RenderScale = {renderScaleProp.floatValue:F2}; se recomienda <= 0.85 para Android.");
                    warnings++;
                }
            }

            // 6. Controles tactiles en HUD_Game y en los 4 niveles
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (hudPrefab == null || hudPrefab.GetComponentInChildren<TouchControlsUI>(true) == null)
            {
                Debug.LogError($"[I-43] '{HudPrefabPath}' no contiene TouchControlsUI.");
                errors++;
            }
            else
            {
                string hudGuid = AssetDatabase.AssetPathToGUID(HudPrefabPath);
                foreach (string scenePath in GameplayScenes)
                {
                    if (!File.Exists(scenePath) || !File.ReadAllText(scenePath).Contains(hudGuid))
                    {
                        Debug.LogError($"[I-43] La escena '{scenePath}' no referencia HUD_Game.prefab (con TouchControls).");
                        errors++;
                    }
                }
            }

            // 7. Overrides de texturas en Assets/_Project/Art
            int nonAstcOverrides = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti)
                {
                    var androidSettings = ti.GetPlatformTextureSettings("Android");
                    if (androidSettings.overridden && !androidSettings.format.ToString().StartsWith("ASTC", StringComparison.Ordinal))
                    {
                        nonAstcOverrides++;
                        Debug.LogWarning($"[I-43] Textura '{path}' tiene override Android '{androidSettings.format}' distinto de ASTC.");
                    }
                }
            }
            warnings += nonAstcOverrides;

            // 8. Estado del modulo Android Build Support en este Unity Editor
            bool androidSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            if (!androidSupported)
            {
                notes.AppendLine("AVISO: El modulo 'Android Build Support' no esta instalado en Unity 6000.3.9f1 (instalalo desde Unity Hub para compilar el APK).");
                Debug.LogWarning("[I-43] El modulo 'Android Build Support' no esta instalado en este Unity Editor. Configuracion del proyecto validada, pero no se podra compilar el APK hasta instalarlo en Unity Hub.");
            }

            string summary = errors == 0
                ? $"Validacion Android (I-43) OK ({warnings} aviso(s)). Paquete: {pkg} v{PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode}), Backend: {backend}, Arch: {PlayerSettings.Android.targetArchitectures}, ASTC activo."
                : $"Validacion Android (I-43) fallo con {errors} error(es) y {warnings} aviso(s). Revisa la consola.";

            if (notes.Length > 0) summary += "\n\n" + notes.ToString().TrimEnd();

            if (errors > 0) Debug.LogError($"[I-43] {summary}");
            else Debug.Log($"[I-43] {summary}");

            if (showDialog)
                EditorUtility.DisplayDialog("Validar Android (I-43)", summary, "OK");

            return errors;
        }

        [MenuItem("Tools/Android (I-43)/Generar APK Release (Builds/Android/1910.apk)", priority = 60)]
        public static void BuildReleaseApk() => BuildApk(development: false, outputPath: ReleaseApkPath, interactive: true);

        [MenuItem("Tools/Android (I-43)/Generar APK Development con FPS (Builds/Android/1910_Dev.apk)", priority = 61)]
        public static void BuildDevelopmentApk() => BuildApk(development: true, outputPath: DevApkPath, interactive: true);

        /// <summary>Entrada CLI para compilar APK Release (-executeMethod Reloj.EditorTools.AndroidBuildTools.BuildApkFromCLI).</summary>
        public static void BuildApkFromCLI()
        {
            bool dev = Array.Exists(Environment.GetCommandLineArgs(), a => string.Equals(a, "-devApk", StringComparison.OrdinalIgnoreCase));
            bool ok = BuildApk(development: dev, outputPath: dev ? DevApkPath : ReleaseApkPath, interactive: false);
            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
            else if (!ok)
                throw new InvalidOperationException("[I-43] Fallo la generacion del APK.");
        }

        private static bool BuildApk(bool development, string outputPath, bool interactive)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                const string msg = "No esta instalado 'Android Build Support' (Android SDK & NDK Tools + OpenJDK) para Unity 6000.3.9f1 en Unity Hub.";
                Debug.LogError($"[I-43] {msg}");
                if (interactive) EditorUtility.DisplayDialog("Falta Android Build Support", msg, "OK");
                return false;
            }

            if (Validate(showDialog: false) > 0)
            {
                const string msg = "La validacion de Android reporto errores. Corrigelos antes de generar el APK.";
                Debug.LogError($"[I-43] {msg}");
                if (interactive) EditorUtility.DisplayDialog("Validacion fallida", msg, "OK");
                return false;
            }

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            var options = new BuildPlayerOptions
            {
                scenes = BuildIntegrationValidator.CanonicalScenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                string msg = $"APK generado correctamente en '{outputPath}' ({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F1} s).";
                Debug.Log($"[I-43] {msg}");
                if (interactive) EditorUtility.DisplayDialog("APK generado (I-43)", msg, "OK");
                return true;
            }

            string failMsg = $"Error generando APK ({summary.result}): {summary.totalErrors} error(es). Revisa la consola.";
            Debug.LogError($"[I-43] {failMsg}");
            if (interactive) EditorUtility.DisplayDialog("Error de build Android", failMsg, "OK");
            return false;
        }
    }
}

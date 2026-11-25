#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Reloj.UI;

namespace Reloj.EditorTools
{
    /// <summary>
    /// UI responsiva: CanvasScaler en Scale With Screen Size 1920x1080 y un panel "SafeArea" (SafeAreaFitter)
    /// bajo cada Canvas raiz. Los fondos y fundidos a pantalla completa quedan fuera del area segura para
    /// que cubran toda la pantalla; el resto del contenido cuelga de "SafeArea".
    /// Es idempotente: se puede ejecutar varias veces.
    /// </summary>
    public static class UIResponsiveSetup
    {
        const float RefW = 1920f, RefH = 1080f;
        const string SafeName = "SafeArea";
        const string ContentName = "Content";

        // Elementos que son puro fondo/fundido: se dejan a pantalla completa, fuera del area segura.
        static readonly Regex PureBackground = new Regex("fondo|fundido|fade|backdrop|vineta|overlay|dim", RegexOptions.IgnoreCase);

        static readonly string[] CanvasPrefabs =
        {
            "Assets/_Project/Prefabs/UI/GameStateUI.prefab",
            "Assets/_Project/Prefabs/UI/DialogueUI.prefab",
            "Assets/_Project/Prefabs/UI/TouchControls.prefab",
        };

        // Paneles (raiz con Image + hijos) que se usan dentro de otros Canvas.
        static readonly string[] PanelPrefabs =
        {
            "Assets/_Project/Art/UI/Panel_Opciones.prefab",
            "Assets/_Project/Art/UI/Panel_Creditos.prefab",
        };

        static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Splash.unity",
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Decision_Final.unity",
            "Assets/_Project/Scenes/Ending_A.unity",
            "Assets/_Project/Scenes/Ending_B.unity",
            "Assets/_Project/Scenes/Level01_Gameplay.unity",
            "Assets/_Project/Scenes/Level02_Gameplay.unity",
            "Assets/_Project/Scenes/Level03_Gameplay.unity",
            "Assets/_Project/Scenes/Level04_Gameplay.unity",
            "Assets/_Project/Scenes/Test_GameState.unity",
            "Assets/_Project/Scenes/Test_Stats.unity",
            "Assets/_Project/Scenes/Test_Visual.unity",
            "Assets/_Project/Scenes/Test_Input.unity",
        };

        [MenuItem("Reloj/UI/Aplicar responsive (escalado + area segura)")]
        public static void Run()
        {
            var log = new List<string>();

            foreach (var path in PanelPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { log.Add("FALTA " + path); continue; }
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (WrapPanel((RectTransform)root.transform, 1f)) { PrefabUtility.SaveAsPrefabAsset(root, path); log.Add("panel: " + path); }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            foreach (var path in CanvasPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { log.Add("FALTA " + path); continue; }
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var canvas = root.GetComponent<Canvas>();
                    if (canvas != null) { ProcessCanvas(canvas, log); PrefabUtility.SaveAsPrefabAsset(root, path); log.Add("prefab: " + path); }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            foreach (var path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { log.Add("FALTA " + path); continue; }
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas) continue;
                    if (PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject)) continue;   // se arregla en el prefab
                    changed |= ProcessCanvas(canvas, log);
                }
                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); log.Add("escena: " + path); }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[UIResponsiveSetup]\n" + string.Join("\n", log));
        }

        /// <summary>Devuelve true si cambio algo.</summary>
        static bool ProcessCanvas(Canvas canvas, List<string> log)
        {
            bool changed = false;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) { scaler = canvas.gameObject.AddComponent<CanvasScaler>(); changed = true; }

            // Factor para mantener el tamano visual de lo maquetado en otra referencia (1280x720 -> 1.5).
            float factor = 1f;
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution.y > 1f)
                factor = RefH / scaler.referenceResolution.y;
            if (Mathf.Abs(factor - 1f) < 0.01f) factor = 1f;

            bool scalerOk = scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                            && scaler.referenceResolution == new Vector2(RefW, RefH)
                            && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                            && Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f);
            if (!scalerOk)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(RefW, RefH);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
                changed = true;
                log.Add($"scaler {canvas.name}: referencia -> {RefW}x{RefH} (factor {factor:0.##})");
            }

            var rt = (RectTransform)canvas.transform;
            if (rt.Find(SafeName) != null) return changed;   // ya tiene SafeArea

            var toMove = new List<Transform>();
            foreach (Transform child in rt)
            {
                if (IsPureBackground(child)) continue;
                toMove.Add(child);
            }

            // Paneles a pantalla completa con su propio fondo (pausa, fin de nivel...): el fondo cubre todo
            // y el SafeArea va DENTRO del panel. Los demas hijos van al SafeArea del Canvas.
            var direct = new List<Transform>();
            foreach (var child in toMove)
            {
                if (IsFullStretch(child) && child.GetComponent<Graphic>() != null && child.childCount > 0)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                    {
                        log.Add($"{canvas.name}/{child.name}: es una instancia de prefab; su SafeArea se arma en el prefab.");
                        continue;
                    }
                    WrapPanel((RectTransform)child, factor);
                    changed = true;
                }
                else direct.Add(child);
            }

            if (direct.Count > 0)
            {
                int index = direct[0].GetSiblingIndex();
                var holder = CreateSafeArea(rt, index, factor);
                foreach (var child in direct) child.SetParent(holder, false);
                changed = true;
                log.Add($"{canvas.name}: {direct.Count} elemento(s) -> {SafeName}");
            }
            return changed;
        }

        /// <summary>Mete los hijos del panel en un SafeArea propio (el fondo del panel sigue cubriendo toda la pantalla).</summary>
        static bool WrapPanel(RectTransform panel, float factor)
        {
            if (panel.Find(SafeName) != null || panel.childCount == 0) return false;
            var kids = new List<Transform>();
            foreach (Transform c in panel) kids.Add(c);
            var holder = CreateSafeArea(panel, 0, factor);
            foreach (var k in kids) k.SetParent(holder, false);
            return true;
        }

        static RectTransform CreateSafeArea(RectTransform parent, int siblingIndex, float factor)
        {
            var go = new GameObject(SafeName, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            rt.SetSiblingIndex(siblingIndex);
            var fitter = go.AddComponent<SafeAreaFitter>();

            if (factor <= 1.001f) return rt;

            var cgo = new GameObject(ContentName, typeof(RectTransform));
            var crt = (RectTransform)cgo.transform;
            crt.SetParent(rt, false);
            Stretch(crt);
            fitter.Configure(crt, factor);
            return crt;
        }

        static bool IsPureBackground(Transform t)
        {
            return PureBackground.IsMatch(t.name) && t.childCount == 0 && IsFullStretch(t);
        }

        static bool IsFullStretch(Transform t)
        {
            var r = t as RectTransform;
            if (r == null) return false;
            return r.anchorMin == Vector2.zero && r.anchorMax == Vector2.one
                   && r.offsetMin.sqrMagnitude < 0.01f && r.offsetMax.sqrMagnitude < 0.01f;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            r.localScale = Vector3.one;
        }
    }
}
#endif

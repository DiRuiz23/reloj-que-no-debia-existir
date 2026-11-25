#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Reloj.UI;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Herramientas de verificacion de UI responsiva (historia "UI se ve bien en cualquier pantalla").
    /// SetGameViewSize cambia la resolucion del Game view; Audit (en Play Mode) revisa textos cortados,
    /// elementos fuera de pantalla / del area segura y elementos encimados.
    /// </summary>
    public static class UIVerifyTools
    {
        public static void SetGameViewSize(int width, int height)
        {
            var asm = typeof(Editor).Assembly;
            var gvT = asm.GetType("UnityEditor.GameView");
            var sizesT = asm.GetType("UnityEditor.GameViewSizes");
            var sizeT = asm.GetType("UnityEditor.GameViewSize");
            var typeT = asm.GetType("UnityEditor.GameViewSizeType");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesT);
            var inst = single.GetProperty("instance").GetValue(null);
            var group = sizesT.GetProperty("currentGroup").GetValue(inst);
            var gT = group.GetType();
            int total = (int)gT.GetMethod("GetTotalCount").Invoke(group, null);
            int found = -1;
            for (int i = 0; i < total; i++)
            {
                var s = gT.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)sizeT.GetProperty("width").GetValue(s) == width && (int)sizeT.GetProperty("height").GetValue(s) == height
                    && (int)sizeT.GetProperty("sizeType").GetValue(s) == 1) { found = i; break; }
            }
            if (found < 0)
            {
                var fixedType = System.Enum.ToObject(typeT, 1);   // FixedResolution
                var ns = System.Activator.CreateInstance(sizeT, new object[] { fixedType, width, height, $"{width}x{height}" });
                gT.GetMethod("AddCustomSize").Invoke(group, new[] { ns });
                found = (int)gT.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }
            var gv = EditorWindow.GetWindow(gvT);
            var cb = gvT.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            cb.Invoke(gv, new object[] { found, null });
            gv.Repaint();
        }

        static Rect ScreenRect(RectTransform rt, Canvas canvas)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, c[i]);
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return sb.ToString();
        }

        class Item { public RectTransform rt; public Rect r; public bool isText; public bool isButton; public string path; public Canvas canvas; }

        /// <summary>Revisa los Canvas activos. Hay que llamarlo en Play Mode y con la UI a revisar activada.</summary>
        public static string Audit(bool checkSafeArea)
        {
            Canvas.ForceUpdateCanvases();
            var sb = new StringBuilder();
            int W = Screen.width, H = Screen.height;
            Rect safe = Screen.safeArea;
            if (SafeAreaFitter.DebugNormalizedSafeArea.HasValue)
            {
                var d = SafeAreaFitter.DebugNormalizedSafeArea.Value;
                safe = new Rect(d.x * W, d.y * H, d.width * W, d.height * H);
            }
            sb.AppendLine($"Pantalla {W}x{H}  area segura {safe.xMin:0},{safe.yMin:0}-{safe.xMax:0},{safe.yMax:0}");

            var items = new List<Item>();
            int issues = 0;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || !canvas.isActiveAndEnabled) continue;
                foreach (var tmp in canvas.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (!tmp.isActiveAndEnabled || string.IsNullOrWhiteSpace(tmp.text)) continue;
                    if (tmp.canvasRenderer.GetAlpha() < 0.05f || tmp.color.a < 0.05f) continue;
                    var cg = tmp.GetComponentInParent<CanvasGroup>();
                    if (cg != null && cg.alpha < 0.05f) continue;
                    tmp.ForceMeshUpdate();
                    var rt = tmp.rectTransform;
                    var r = ScreenRect(rt, canvas);
                    var it = new Item { rt = rt, r = r, isText = true, path = Path(rt), canvas = canvas };
                    items.Add(it);

                    // Texto cortado: lo que necesita vs. lo que tiene (en unidades del propio rect).
                    float needW = tmp.preferredWidth, needH = tmp.preferredHeight;
                    bool wraps = tmp.textWrappingMode != TextWrappingModes.NoWrap;
                    bool autosize = tmp.enableAutoSizing;
                    if (!autosize)
                    {
                        if (wraps && needH > rt.rect.height + 2f && tmp.overflowMode != TextOverflowModes.ScrollRect)
                        { sb.AppendLine($"  [CORTADO alto] {it.path}  necesita {needH:0} de {rt.rect.height:0}  \"{Trim(tmp.text)}\""); issues++; }
                        else if (!wraps && needW > rt.rect.width + 2f && tmp.overflowMode == TextOverflowModes.Truncate)
                        { sb.AppendLine($"  [CORTADO ancho] {it.path}  necesita {needW:0} de {rt.rect.width:0}  \"{Trim(tmp.text)}\""); issues++; }
                    }
                    if (tmp.isTextTruncated) { sb.AppendLine($"  [TRUNCADO] {it.path}  \"{Trim(tmp.text)}\""); issues++; }

                    if (OutsideScreen(r, W, H)) { sb.AppendLine($"  [FUERA DE PANTALLA] {it.path}  rect {r.xMin:0},{r.yMin:0}-{r.xMax:0},{r.yMax:0}"); issues++; }
                    else if (checkSafeArea && OutsideRect(r, safe)) { sb.AppendLine($"  [FUERA DEL AREA SEGURA] {it.path}  rect {r.xMin:0},{r.yMin:0}-{r.xMax:0},{r.yMax:0}"); issues++; }
                }
                foreach (var sel in canvas.GetComponentsInChildren<Selectable>(false))
                {
                    if (!sel.isActiveAndEnabled || !sel.interactable) continue;
                    var rt = (RectTransform)sel.transform;
                    var r = ScreenRect(rt, canvas);
                    if (r.width < 2f || r.height < 2f) continue;
                    var it = new Item { rt = rt, r = r, isButton = true, path = Path(rt), canvas = canvas };
                    items.Add(it);
                    if (OutsideScreen(r, W, H)) { sb.AppendLine($"  [FUERA DE PANTALLA] {it.path}  rect {r.xMin:0},{r.yMin:0}-{r.xMax:0},{r.yMax:0}"); issues++; }
                    else if (checkSafeArea && OutsideRect(r, safe)) { sb.AppendLine($"  [FUERA DEL AREA SEGURA] {it.path}  rect {r.xMin:0},{r.yMin:0}-{r.xMax:0},{r.yMax:0}"); issues++; }
                }
            }

            // Encimados: textos con textos y botones con botones (sin parentesco), mas texto con boton ajeno.
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i]; var b = items[j];
                    if (a.canvas != b.canvas) continue;
                    if (a.rt.IsChildOf(b.rt) || b.rt.IsChildOf(a.rt)) continue;
                    if (a.isText && b.isButton && (a.rt.GetComponentInParent<Selectable>() == b.rt.GetComponent<Selectable>() )) continue;
                    if (a.isButton && b.isText && (b.rt.GetComponentInParent<Selectable>() == a.rt.GetComponent<Selectable>())) continue;
                    var ix = Rect.MinMaxRect(Mathf.Max(a.r.xMin, b.r.xMin), Mathf.Max(a.r.yMin, b.r.yMin), Mathf.Min(a.r.xMax, b.r.xMax), Mathf.Min(a.r.yMax, b.r.yMax));
                    if (ix.width > 4f && ix.height > 4f)
                    { sb.AppendLine($"  [ENCIMADOS] {a.path}  <->  {b.path}  ({ix.width:0}x{ix.height:0}px)"); issues++; }
                }

            sb.AppendLine(issues == 0 ? "  OK: sin problemas." : $"  Problemas: {issues}");
            return sb.ToString();
        }

        static string Trim(string s) { s = s.Replace("\n", " "); return s.Length > 40 ? s.Substring(0, 40) + "..." : s; }
        static bool OutsideScreen(Rect r, int W, int H) => r.xMin < -1f || r.yMin < -1f || r.xMax > W + 1f || r.yMax > H + 1f;
        static bool OutsideRect(Rect r, Rect s) => r.xMin < s.xMin - 1f || r.yMin < s.yMin - 1f || r.xMax > s.xMax + 1f || r.yMax > s.yMax + 1f;
    }
}
#endif

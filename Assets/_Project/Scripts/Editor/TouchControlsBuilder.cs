#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Reloj.Player;
using Reloj.Tutorial;
using Reloj.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Genera los sprites, el TMP_SpriteAsset, el prefab TouchControls, lo anida en HUD_Game
    /// y actualiza las referencias de Move en Player y Level01_Gameplay (I-37).
    /// </summary>
    public static class TouchControlsBuilder
    {
        const string ArtFolder = "Assets/_Project/Art/UI/Touch";
        const string SpriteAssetFolder = "Assets/Resources/Sprite Assets";
        const string SpriteAssetPath = SpriteAssetFolder + "/TouchIcons_TMP.asset";
        const string PrefabPath = "Assets/_Project/Prefabs/UI/TouchControls.prefab";
        const string HudPrefabPath = "Assets/_Project/Prefabs/UI/HUD_Game.prefab";
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        const string Level01ScenePath = "Assets/_Project/Scenes/Level01_Gameplay.unity";
        const string TestInputScenePath = "Assets/_Project/Scenes/Test_Input.unity";
        const string InputActionsPath = "Assets/_Project/Data/Input/PlayerControls.inputactions";
        const string PalettePath = "Assets/_Project/Data/UI/UIPalette.asset";
        const string FontPath = "Assets/_Project/Art/UI/Fonts/Alegreya SDF.asset";

        [MenuItem("Reloj/Controles táctiles/Construir controles táctiles e íconos (I-37)")]
        public static void BuildAll()
        {
            EnsureDirectories();

            UIPalette palette = AssetDatabase.LoadAssetAtPath<UIPalette>(PalettePath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            GenerateAllSprites(palette);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureAllSpriteImporters();

            BuildTmpSpriteAsset();
            BuildTouchControlsPrefab(palette, font);
            NestInHudGamePrefab();
            FixBrokenMoveReferences();
            WireTestInputScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TouchControlsBuilder] Controles tactiles, iconos TMP y prefabs construidos correctamente (I-37).");
        }

        [MenuItem("Reloj/Controles táctiles/Simular esquema Táctil (Play)")]
        static void SimulateTouch() => InputDeviceTracker.Set(InputScheme.Touch);

        [MenuItem("Reloj/Controles táctiles/Simular esquema Teclado (Play)")]
        static void SimulateKeyboard() => InputDeviceTracker.Set(InputScheme.Keyboard);

        [MenuItem("Reloj/Controles táctiles/Simular esquema Gamepad (Play)")]
        static void SimulateGamepad() => InputDeviceTracker.Set(InputScheme.Gamepad);

        // ------------------------------------------------------------------ directorios

        static void EnsureDirectories()
        {
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory(SpriteAssetFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/_Project/Prefabs/UI");
        }

        // ------------------------------------------------------------------ generacion de PNGs

        static void GenerateAllSprites(UIPalette palette)
        {
            Color tinta = palette != null ? palette.Tinta : new Color(0.17f, 0.11f, 0.08f, 1f);
            Color arena = palette != null ? palette.Arena : new Color(0.79f, 0.66f, 0.47f, 1f);
            Color perga = palette != null ? palette.Pergamino : new Color(0.92f, 0.85f, 0.71f, 1f);
            Color acento = palette != null ? palette.Acento : new Color(1f, 0.65f, 0.17f, 1f);

            SaveAndDestroyPng(ArtFolder + "/touch_stick_bg.png", DrawStickBg(256));
            SaveAndDestroyPng(ArtFolder + "/touch_stick_knob.png", DrawStickKnob(128));
            SaveAndDestroyPng(ArtFolder + "/touch_btn_bg.png", DrawButtonBg(256));
            SaveAndDestroyPng(ArtFolder + "/touch_btn_ring.png", DrawButtonRing(256));

            Texture2D moveIcon = DrawMoveIcon(128, Color.white);
            Texture2D jumpIcon = DrawJumpIcon(128, Color.white);
            Texture2D interactIcon = DrawInteractIcon(128, Color.white);
            Texture2D timestopIcon = DrawTimeStopIcon(128, Color.white);
            Texture2D pauseIcon = DrawPauseIcon(128, Color.white);

            SaveAndDestroyPng(ArtFolder + "/icon_touch_move.png", moveIcon);
            SaveAndDestroyPng(ArtFolder + "/icon_touch_jump.png", jumpIcon);
            SaveAndDestroyPng(ArtFolder + "/icon_touch_interact.png", interactIcon);
            SaveAndDestroyPng(ArtFolder + "/icon_touch_timestop.png", timestopIcon);
            SaveAndDestroyPng(ArtFolder + "/icon_touch_pause.png", pauseIcon);

            Texture2D atlas = BuildIconsAtlas(tinta, arena, perga, acento);
            SaveAndDestroyPng(ArtFolder + "/TouchIcons_Atlas.png", atlas);
        }

        static void SaveAndDestroyPng(string path, Texture2D tex)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void ConfigureAllSpriteImporters()
        {
            string[] files = Directory.GetFiles(ArtFolder, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
                if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; dirty = true; }
                if (Mathf.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f) { importer.spritePixelsPerUnit = 100f; dirty = true; }

                if (dirty) importer.SaveAndReimport();
            }
        }

        // ---- Primitivas SDF con antialiasing ----

        static float SmoothAlpha(float sd, float aa = 1.5f) => Mathf.Clamp01(0.5f - sd / aa);

        static float SdCircle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

        static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude;
        }

        static float SdBox(Vector2 p, Vector2 c, Vector2 halfSize, float radius = 0f)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - (halfSize - Vector2.one * radius);
            Vector2 dPos = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f));
            return dPos.magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - radius;
        }

        static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
            Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(
                Vector2.Min(
                    new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                    new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        static Texture2D DrawStickBg(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float rOuter = size * 0.46f;
            float rBorderInner = rOuter - size * 0.025f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dOuter = SdCircle(p, center, rOuter);
                float fillA = SmoothAlpha(dOuter);
                float ringA = SmoothAlpha(Mathf.Abs((p - center).magnitude - (rOuter + rBorderInner) * 0.5f) - (rOuter - rBorderInner) * 0.5f);

                // 4 muescas cardinales suaves
                float notch = Mathf.Min(
                    Mathf.Min(
                        SdSegment(p, center + Vector2.up * (rOuter * 0.72f), center + Vector2.up * (rOuter * 0.86f)) - 3f,
                        SdSegment(p, center + Vector2.down * (rOuter * 0.72f), center + Vector2.down * (rOuter * 0.86f)) - 3f),
                    Mathf.Min(
                        SdSegment(p, center + Vector2.left * (rOuter * 0.72f), center + Vector2.left * (rOuter * 0.86f)) - 3f,
                        SdSegment(p, center + Vector2.right * (rOuter * 0.72f), center + Vector2.right * (rOuter * 0.86f)) - 3f));
                float notchA = SmoothAlpha(notch) * 0.55f;

                // Blanco neutro para que Image.color de la paleta tinte limpio; borde y muescas algo mas brillantes
                float brightness = Mathf.Lerp(0.55f, 1f, Mathf.Max(ringA, notchA));
                float alpha = Mathf.Max(fillA * 0.55f, Mathf.Max(ringA * 0.95f, notchA));
                pixels[y * size + x] = new Color(brightness, brightness, brightness, alpha);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D DrawStickKnob(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float r = size * 0.44f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = (p - center).magnitude;
                float a = SmoothAlpha(dist - r);
                float rim = SmoothAlpha(Mathf.Abs(dist - (r - 4f)) - 3f);
                float grad = Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(1f - dist / r));
                float b = Mathf.Max(grad, rim);
                pixels[y * size + x] = new Color(b, b, b, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D DrawButtonBg(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float rOuter = size * 0.46f;
            float ringThick = size * 0.022f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = (p - center).magnitude;
                float fillA = SmoothAlpha(dist - rOuter);
                float borderA = SmoothAlpha(Mathf.Abs(dist - (rOuter - ringThick)) - ringThick);
                float b = Mathf.Lerp(0.82f, 1f, borderA);
                float a = Mathf.Max(fillA * 0.85f, borderA);
                pixels[y * size + x] = new Color(b, b, b, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D DrawButtonRing(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float rMid = size * 0.455f;
            float halfThick = size * 0.03f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Mathf.Abs((p - center).magnitude - rMid) - halfThick;
                float a = SmoothAlpha(d);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D DrawMoveIcon(int size, Color tint)
        {
            return RenderMask(size, tint, (p, s) =>
            {
                Vector2 c = Vector2.one * (s * 0.5f);
                float d = SdCircle(p, c, s * 0.14f);
                float off = s * 0.25f;
                float tip = s * 0.40f;
                float hw = s * 0.10f;
                d = Mathf.Min(d, SdTriangle(p, c + new Vector2(0f, tip), c + new Vector2(-hw, off), c + new Vector2(hw, off)));
                d = Mathf.Min(d, SdTriangle(p, c + new Vector2(0f, -tip), c + new Vector2(hw, -off), c + new Vector2(-hw, -off)));
                d = Mathf.Min(d, SdTriangle(p, c + new Vector2(tip, 0f), c + new Vector2(off, hw), c + new Vector2(off, -hw)));
                d = Mathf.Min(d, SdTriangle(p, c + new Vector2(-tip, 0f), c + new Vector2(-off, -hw), c + new Vector2(-off, hw)));
                return d;
            });
        }

        static Texture2D DrawJumpIcon(int size, Color tint)
        {
            return RenderMask(size, tint, (p, s) =>
            {
                Vector2 c = Vector2.one * (s * 0.5f);
                float head = SdTriangle(p,
                    c + new Vector2(0f, s * 0.30f),
                    c + new Vector2(-s * 0.23f, s * 0.04f),
                    c + new Vector2(s * 0.23f, s * 0.04f));
                float stem = SdBox(p, c + new Vector2(0f, -s * 0.06f), new Vector2(s * 0.08f, s * 0.15f), s * 0.02f);
                float baseLine = SdBox(p, c + new Vector2(0f, -s * 0.28f), new Vector2(s * 0.24f, s * 0.04f), s * 0.02f);
                return Mathf.Min(head, Mathf.Min(stem, baseLine));
            });
        }

        static Texture2D DrawInteractIcon(int size, Color tint)
        {
            return RenderMask(size, tint, (p, s) =>
            {
                Vector2 c = Vector2.one * (s * 0.5f);
                // Anillo exterior + centro solido + destello superior (simbolo de interaccion / accion)
                float outerRing = Mathf.Abs(SdCircle(p, c, s * 0.30f)) - s * 0.045f;
                float innerDot = SdCircle(p, c, s * 0.13f);
                return Mathf.Min(outerRing, innerDot);
            });
        }

        static Texture2D DrawTimeStopIcon(int size, Color tint)
        {
            return RenderMask(size, tint, (p, s) =>
            {
                Vector2 c = Vector2.one * (s * 0.5f - s * 0.03f);
                float r = s * 0.31f;
                float ring = Mathf.Abs(SdCircle(p, c, r)) - s * 0.045f;
                float crownStem = SdBox(p, c + new Vector2(0f, r + s * 0.06f), new Vector2(s * 0.045f, s * 0.055f), s * 0.015f);
                float crownTop = SdBox(p, c + new Vector2(0f, r + s * 0.12f), new Vector2(s * 0.095f, s * 0.035f), s * 0.02f);
                // Manecillas a las 3:17
                float hHand = SdSegment(p, c, c + new Vector2(r * 0.45f, -r * 0.10f)) - s * 0.032f;
                float mHand = SdSegment(p, c, c + new Vector2(r * 0.72f, -r * 0.20f)) - s * 0.024f;
                float centerDot = SdCircle(p, c, s * 0.048f);
                return Mathf.Min(Mathf.Min(ring, crownStem), Mathf.Min(crownTop, Mathf.Min(Mathf.Min(hHand, mHand), centerDot)));
            });
        }

        static Texture2D DrawPauseIcon(int size, Color tint)
        {
            return RenderMask(size, tint, (p, s) =>
            {
                Vector2 c = Vector2.one * (s * 0.5f);
                float dx = s * 0.13f;
                Vector2 half = new Vector2(s * 0.065f, s * 0.24f);
                float left = SdBox(p, c + new Vector2(-dx, 0f), half, s * 0.03f);
                float right = SdBox(p, c + new Vector2(dx, 0f), half, s * 0.03f);
                return Mathf.Min(left, right);
            });
        }

        static Texture2D RenderMask(int size, Color tint, Func<Vector2, float, float> sdf)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = SmoothAlpha(sdf(new Vector2(x, y), size));
                pixels[y * size + x] = new Color(tint.r, tint.g, tint.b, tint.a * a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D BuildIconsAtlas(Color tinta, Color arena, Color perga, Color acento)
        {
            const int slot = 128;
            const int count = 5;
            var atlas = new Texture2D(slot * count, slot, TextureFormat.RGBA32, false);
            var clear = new Color[slot * count * slot];
            atlas.SetPixels(clear);

            Texture2D[] icons =
            {
                DrawMoveIcon(slot, perga),
                DrawJumpIcon(slot, perga),
                DrawInteractIcon(slot, perga),
                DrawTimeStopIcon(slot, acento),
                DrawPauseIcon(slot, perga),
            };
            Color[] borders = { arena, arena, arena, acento, arena };

            for (int i = 0; i < count; i++)
            {
                Texture2D bg = DrawBadge(slot, tinta, borders[i]);
                Color[] bgPx = bg.GetPixels();

                for (int y = 0; y < slot; y++)
                for (int x = 0; x < slot; x++)
                {
                    int idx = y * slot + x;
                    // Encoger un poco el icono dentro del badge (muestra centrada ~76%)
                    float sx = (x - slot * 0.5f) / 0.72f + slot * 0.5f;
                    float sy = (y - slot * 0.5f) / 0.72f + slot * 0.5f;
                    Color fg = (sx >= 0 && sx < slot - 1 && sy >= 0 && sy < slot - 1)
                        ? icons[i].GetPixelBilinear(sx / slot, sy / slot)
                        : Color.clear;

                    Color dst = bgPx[idx];
                    Color outC = AlphaOver(dst, fg);
                    atlas.SetPixel(i * slot + x, y, outC);
                }

                UnityEngine.Object.DestroyImmediate(bg);
                UnityEngine.Object.DestroyImmediate(icons[i]);
            }

            atlas.Apply();
            return atlas;
        }

        static Texture2D DrawBadge(int size, Color fill, Color border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float rOuter = size * 0.45f;
            float ringThick = size * 0.04f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = (new Vector2(x, y) - center).magnitude;
                float fillA = SmoothAlpha(dist - rOuter);
                float borderA = SmoothAlpha(Mathf.Abs(dist - (rOuter - ringThick * 0.5f)) - ringThick * 0.5f);

                Color cFill = new Color(fill.r, fill.g, fill.b, fillA * 0.92f);
                Color cBorder = new Color(border.r, border.g, border.b, borderA);
                pixels[y * size + x] = AlphaOver(cFill, cBorder);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Color AlphaOver(Color dst, Color src)
        {
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f) return Color.clear;
            float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
            float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
            float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;
            return new Color(r, g, b, outA);
        }

        // ------------------------------------------------------------------ TMP_SpriteAsset

        static void BuildTmpSpriteAsset()
        {
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/TouchIcons_Atlas.png");
            if (atlas == null)
            {
                Debug.LogError("[TouchControlsBuilder] No se encontro TouchIcons_Atlas.png.");
                return;
            }

            TMP_SpriteAsset spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath);
            if (spriteAsset == null)
            {
                spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, SpriteAssetPath);
            }

            spriteAsset.name = "TouchIcons_TMP";
            spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);
            spriteAsset.spriteSheet = atlas;

            using (var so = new SerializedObject(spriteAsset))
            {
                var verProp = so.FindProperty("m_Version");
                if (verProp != null) verProp.stringValue = "1.1.0";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (spriteAsset.material == null)
            {
                Shader shader = Shader.Find("TextMeshPro/Sprite");
                Material mat = new Material(shader) { name = spriteAsset.name + " Material" };
                mat.SetTexture(ShaderUtilities.ID_MainTex, atlas);
                spriteAsset.material = mat;
                AssetDatabase.AddObjectToAsset(mat, spriteAsset);
            }
            else
            {
                spriteAsset.material.SetTexture(ShaderUtilities.ID_MainTex, atlas);
            }

            string[] names = { "touch_stick", "touch_jump", "touch_interact", "touch_timestop", "touch_pause" };
            var glyphTable = spriteAsset.spriteGlyphTable;
            var charTable = spriteAsset.spriteCharacterTable;
            glyphTable.Clear();
            charTable.Clear();

            const float cell = 128f;
            for (int i = 0; i < names.Length; i++)
            {
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(cell, cell, 0f, 106f, cell),
                    glyphRect = new GlyphRect(i * (int)cell, 0, (int)cell, (int)cell),
                    scale = 1.3f,
                    atlasIndex = 0,
                };
                glyphTable.Add(glyph);

                var ch = new TMP_SpriteCharacter(0xFFFE, glyph)
                {
                    name = names[i],
                    scale = 1.0f,
                };
                charTable.Add(ch);
            }

            spriteAsset.UpdateLookupTables();
            EditorUtility.SetDirty(spriteAsset);
        }

        // ------------------------------------------------------------------ Prefab TouchControls

        static void BuildTouchControlsPrefab(UIPalette palette, TMP_FontAsset font)
        {
            Color tinta = palette != null ? palette.Tinta : new Color(0.17f, 0.11f, 0.08f, 1f);
            Color arena = palette != null ? palette.Arena : new Color(0.79f, 0.66f, 0.47f, 1f);
            Color perga = palette != null ? palette.Pergamino : new Color(0.92f, 0.85f, 0.71f, 1f);
            Color acento = palette != null ? palette.Acento : new Color(1f, 0.65f, 0.17f, 1f);

            var root = new GameObject("TouchControls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TouchControlsUI));
            var rootRt = (RectTransform)root.transform;
            Stretch(rootRt);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            canvas.overrideSorting = true;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var safeAreaGo = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            var safeRt = (RectTransform)safeAreaGo.transform;
            safeRt.SetParent(rootRt, false);
            Stretch(safeRt);

            var controlsRoot = new GameObject("ControlsRoot", typeof(RectTransform));
            var controlsRt = (RectTransform)controlsRoot.transform;
            controlsRt.SetParent(safeRt, false);
            Stretch(controlsRt);
            safeAreaGo.GetComponent<SafeAreaFitter>().Configure(controlsRt, 1.5f);

            // Stick izquierdo (Mover)
            Sprite stickBgSprite = LoadSprite("touch_stick_bg.png");
            Sprite stickKnobSprite = LoadSprite("touch_stick_knob.png");
            Sprite btnBgSprite = LoadSprite("touch_btn_bg.png");
            Sprite btnRingSprite = LoadSprite("touch_btn_ring.png");

            var stickGo = new GameObject("Stick_Move", typeof(RectTransform), typeof(Image));
            var stickRt = (RectTransform)stickGo.transform;
            stickRt.SetParent(controlsRt, false);
            stickRt.anchorMin = stickRt.anchorMax = stickRt.pivot = Vector2.zero;
            stickRt.anchoredPosition = new Vector2(42f, 38f);
            stickRt.sizeDelta = new Vector2(160f, 160f);
            var stickImg = stickGo.GetComponent<Image>();
            stickImg.sprite = stickBgSprite;
            stickImg.color = new Color(tinta.r, tinta.g, tinta.b, 0.58f);
            stickImg.raycastTarget = false;

            var knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image), typeof(OnScreenStick));
            var knobRt = (RectTransform)knobGo.transform;
            knobRt.SetParent(stickRt, false);
            knobRt.anchorMin = knobRt.anchorMax = knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.anchoredPosition = Vector2.zero;
            knobRt.sizeDelta = new Vector2(78f, 78f);
            var knobImg = knobGo.GetComponent<Image>();
            knobImg.sprite = stickKnobSprite;
            knobImg.color = new Color(arena.r, arena.g, arena.b, 0.90f);
            knobImg.raycastTarget = true;
            knobImg.raycastPadding = new Vector4(-42f, -42f, -42f, -42f);

            var stickComp = knobGo.GetComponent<OnScreenStick>();
            using (var so = new SerializedObject(stickComp))
            {
                so.FindProperty("m_ControlPath").stringValue = "<TouchControlsDevice>/stick";
                so.FindProperty("m_MovementRange").floatValue = 48f;
                so.FindProperty("m_Behaviour").enumValueIndex = (int)OnScreenStick.Behaviour.RelativePositionWithStaticOrigin;
                so.FindProperty("m_UseIsolatedInputActions").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Boton Saltar
            CreateActionButton(
                controlsRt, "Btn_Jump", btnBgSprite, LoadSprite("icon_touch_jump.png"),
                "<TouchControlsDevice>/jump",
                new Vector2(1f, 0f), new Vector2(-42f, 38f), new Vector2(102f, 102f),
                new Color(tinta.r, tinta.g, tinta.b, 0.72f), perga,
                "Saltar", font, 14f, 54f,
                out _, out _);

            // Boton Interactuar
            CreateActionButton(
                controlsRt, "Btn_Interact", btnBgSprite, LoadSprite("icon_touch_interact.png"),
                "<TouchControlsDevice>/interact",
                new Vector2(1f, 0f), new Vector2(-164f, 48f), new Vector2(90f, 90f),
                new Color(tinta.r, tinta.g, tinta.b, 0.62f), perga,
                "Acción", font, 13f, 46f,
                out Image interactBg, out Image interactIcon);

            // Boton Parada Temporal
            var timestopBtn = CreateActionButton(
                controlsRt, "Btn_Timestop", btnBgSprite, LoadSprite("icon_touch_timestop.png"),
                "<TouchControlsDevice>/timestop",
                new Vector2(1f, 0f), new Vector2(-54f, 158f), new Vector2(96f, 96f),
                new Color(tinta.r, tinta.g, tinta.b, 0.75f), acento,
                "Parada", font, 13f, 50f,
                out _, out Image timestopIcon);

            var ringGo = new GameObject("CooldownRing", typeof(RectTransform), typeof(Image));
            var ringRt = (RectTransform)ringGo.transform;
            ringRt.SetParent(timestopBtn.transform, false);
            ringRt.SetAsFirstSibling();
            Stretch(ringRt);
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = btnRingSprite;
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillOrigin = (int)Image.Origin360.Top;
            ringImg.fillClockwise = true;
            ringImg.fillAmount = 1f;
            ringImg.color = acento;
            ringImg.raycastTarget = false;

            // Boton Pausa (a la izquierda de HUD_TimeStop)
            CreateActionButton(
                controlsRt, "Btn_Pause", btnBgSprite, LoadSprite("icon_touch_pause.png"),
                "<TouchControlsDevice>/pause",
                new Vector2(1f, 1f), new Vector2(-142f, -28f), new Vector2(70f, 70f),
                new Color(tinta.r, tinta.g, tinta.b, 0.72f), perga,
                null, font, 12f, 34f,
                out _, out _);

            var ui = root.GetComponent<TouchControlsUI>();
            using (var so = new SerializedObject(ui))
            {
                so.FindProperty("controlsRoot").objectReferenceValue = controlsRoot;
                so.FindProperty("stickKnob").objectReferenceValue = knobRt;
                so.FindProperty("palette").objectReferenceValue = palette;
                so.FindProperty("timestopFillRing").objectReferenceValue = ringImg;
                so.FindProperty("timestopIcon").objectReferenceValue = timestopIcon;
                so.FindProperty("interactBackground").objectReferenceValue = interactBg;
                so.FindProperty("interactIcon").objectReferenceValue = interactIcon;
                so.FindProperty("forceShowInEditor").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        static GameObject CreateActionButton(
            RectTransform parent, string name, Sprite bgSprite, Sprite iconSprite, string controlPath,
            Vector2 anchorPivot, Vector2 anchoredPos, Vector2 sizeDelta,
            Color bgColor, Color iconColor, string labelText, TMP_FontAsset font, float labelSize, float iconSize,
            out Image bgImg, out Image iconImg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(OnScreenButton), typeof(TouchButtonVisual));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchorPivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            bgImg = go.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.color = bgColor;
            bgImg.raycastTarget = true;

            var btn = go.GetComponent<OnScreenButton>();
            using (var so = new SerializedObject(btn))
            {
                so.FindProperty("m_ControlPath").stringValue = controlPath;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.SetParent(rt, false);
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = string.IsNullOrEmpty(labelText) ? Vector2.zero : new Vector2(0f, 7f);
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);
            iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = iconColor;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            if (!string.IsNullOrEmpty(labelText))
            {
                var lblGo = new GameObject("Label", typeof(RectTransform));
                var lblRt = (RectTransform)lblGo.transform;
                lblRt.SetParent(rt, false);
                lblRt.anchorMin = new Vector2(0f, 0f);
                lblRt.anchorMax = new Vector2(1f, 0f);
                lblRt.pivot = new Vector2(0.5f, 0f);
                lblRt.anchoredPosition = new Vector2(0f, 8f);
                lblRt.sizeDelta = new Vector2(0f, 20f);

                var tmp = lblGo.AddComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = labelText;
                tmp.fontSize = labelSize;
                tmp.color = new Color(0.92f, 0.85f, 0.71f, 0.95f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }

            return go;
        }

        static Sprite LoadSprite(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + fileName);

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // ------------------------------------------------------------------ Integracion en HUD_Game, Player y Escenas

        static void NestInHudGamePrefab()
        {
            GameObject touchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (touchPrefab == null)
            {
                Debug.LogError("[TouchControlsBuilder] No se pudo cargar TouchControls.prefab.");
                return;
            }

            GameObject hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
            try
            {
                for (int i = hudRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = hudRoot.transform.GetChild(i);
                    if (child.name == "TouchControls")
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(touchPrefab, hudRoot.transform);
                instance.name = "TouchControls";
                var rt = instance.transform as RectTransform;
                if (rt != null) Stretch(rt);

                PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
        }

        static InputActionReference FindGameplayMoveReference()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath);
            foreach (var obj in assets)
            {
                if (obj is InputActionReference r && r.action != null
                    && r.action.actionMap != null && r.action.actionMap.name == "Gameplay"
                    && r.action.name == "Move")
                {
                    return r;
                }
            }
            return null;
        }

        static void FixBrokenMoveReferences()
        {
            InputActionReference moveRef = FindGameplayMoveReference();
            if (moveRef == null)
            {
                Debug.LogError("[TouchControlsBuilder] No se encontro InputActionReference Gameplay/Move en PlayerControls.inputactions.");
                return;
            }

            // 1. Player.prefab
            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var motor = playerRoot.GetComponent<PlayerMotor>();
                if (motor != null)
                {
                    using (var so = new SerializedObject(motor))
                    {
                        so.FindProperty("moveAction").objectReferenceValue = moveRef;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }

            // 2. Level01_Gameplay.unity (TutorialHintZone "mover")
            var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            bool modified = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var zone in root.GetComponentsInChildren<TutorialHintZone>(true))
                {
                    using (var so = new SerializedObject(zone))
                    {
                        if (so.FindProperty("stepId").stringValue == "mover")
                        {
                            so.FindProperty("action").objectReferenceValue = moveRef;
                            so.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(zone);
                            modified = true;
                        }
                    }
                }
            }
            if (modified) EditorSceneManager.SaveScene(scene);
        }

        static void WireTestInputScene()
        {
            GameObject touchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (touchPrefab == null) return;

            var scene = EditorSceneManager.OpenScene(TestInputScenePath, OpenSceneMode.Single);

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            if (UnityEngine.Object.FindFirstObjectByType<TouchControlsUI>() == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(touchPrefab);
                instance.name = "TouchControls";
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif

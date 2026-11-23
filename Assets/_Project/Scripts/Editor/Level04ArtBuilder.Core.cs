#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Generador de Level04_Art (solo editor). Cada zona se reconstruye desde cero (idempotente).
    /// Solo reutiliza piezas del kit ElReloj, prefabs PF_* de Level01 y materiales existentes de Art/.
    /// Orientaciones: yaw 0 = frente hacia -Z (camara); muros a lo largo de Z usan yaw 90.
    /// </summary>
    public static partial class Level04ArtBuilder
    {
        const string FbxRoot = "Assets/_Project/Art/Environment/ElReloj/FBX";
        const string PfRoot = "Assets/_Project/Prefabs/Level01";

        static Dictionary<string, GameObject> prefabs;
        static System.Random rng = new System.Random(4040);
        static Scene scene;
        static Transform util;
        static readonly List<Vector2> placed = new List<Vector2>();

        // ---------------------------------------------------------------- infraestructura
        public static void Init(int seed = 4040)
        {
            rng = new System.Random(seed);
            placed.Clear();
            scene = SceneManager.GetSceneByName("Level04_Art");
            if (!scene.IsValid() || !scene.isLoaded) throw new Exception("Level04_Art no esta abierta.");
            SceneManager.SetActiveScene(scene);
            if (prefabs == null || prefabs.Count == 0)
            {
                prefabs = new Dictionary<string, GameObject>();
                foreach (var root in new[] { FbxRoot, PfRoot })
                    foreach (var g in AssetDatabase.FindAssets("t:GameObject", new[] { root }))
                    {
                        var p = AssetDatabase.GUIDToAssetPath(g);
                        var n = Path.GetFileNameWithoutExtension(p);
                        if (!prefabs.ContainsKey(n)) prefabs[n] = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    }
            }
            util = FindOrCreateRoot("Utileria_Jugable");
        }

        static Transform FindOrCreateRoot(string name)
        {
            foreach (var r in scene.GetRootGameObjects()) if (r.name == name) return r.transform;
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.transform;
        }

        static Transform Fresh(string name, Transform parent)
        {
            if (parent == null)
            {
                foreach (var r in scene.GetRootGameObjects()) if (r.name == name) UnityEngine.Object.DestroyImmediate(r);
                var g0 = new GameObject(name);
                SceneManager.MoveGameObjectToScene(g0, scene);
                return g0.transform;
            }
            var ex = parent.Find(name);
            if (ex != null) UnityEngine.Object.DestroyImmediate(ex.gameObject);
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            return g.transform;
        }

        static Transform Group(string name, Transform parent, Vector3 pos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            return g.transform;
        }

        static float Rnd(float a, float b) => (float)(a + rng.NextDouble() * (b - a));
        static Material Mat(string rel) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/" + rel + ".mat");

        // ---------------------------------------------------------------- colocacion
        public static GameObject Put(string name, float x, float z, float yaw = 0f, float scale = 1f, float y = 0f,
            Transform parent = null, float pitch = 0f, float roll = 0f)
            => PutS(name, x, z, yaw, new Vector3(scale, scale, scale), y, parent, pitch, roll);

        public static GameObject PutS(string name, float x, float z, float yaw, Vector3 scale, float y = 0f,
            Transform parent = null, float pitch = 0f, float roll = 0f)
        {
            if (!prefabs.TryGetValue(name, out var pf)) throw new Exception("Prefab/FBX no encontrado: " + name);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
            var t = go.transform;
            if (parent != null) t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z);
            t.localRotation = Quaternion.Euler(pitch, yaw, roll);
            t.localScale = scale;
            return go;
        }

        static void NoCollide(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Undo.DestroyObjectImmediate(c);
        }

        static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material m,
            Transform parent, bool keepCollider = false, bool castShadow = true)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            if (!keepCollider) { var c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c); }
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            if (!castShadow) r.shadowCastingMode = ShadowCastingMode.Off;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = scale;
            return g;
        }

        static Light AddPoint(Transform parent, string name, Vector3 localPos, Color c, float intensity, float range)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            var l = g.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            g.AddComponent<UniversalAdditionalLightData>();
            return l;
        }

        static readonly Color LuzCalida = new Color(1f, 0.69f, 0.4f);
        static readonly Color LuzFuego = new Color(1f, 0.55f, 0.2f);

        // ---------------------------------------------------------------- teselados / lineas
        static void Tiles(string[] names, float x0, float x1, float z0, float z1, Transform parent, float skip = 0f, float y = 0f)
        {
            int cx = Mathf.RoundToInt((x1 - x0) / 2f), cz = Mathf.RoundToInt((z1 - z0) / 2f);
            for (int i = 0; i < cx; i++)
                for (int j = 0; j < cz; j++)
                {
                    if (skip > 0f && rng.NextDouble() < skip) continue;
                    Put(names[rng.Next(names.Length)], x0 + 1f + 2f * i, z0 + 1f + 2f * j, 90f * rng.Next(4), 1f, y, parent);
                }
        }

        static void RailsX(float z, float xa, float xb, Transform parent)
        {
            for (float x = xa; x <= xb + 0.01f; x += 2f) Put("PF_Via2m", x, z, 0f, 1f, 0f, parent);
        }

        static void Telegrafo(float xa, float xb, float z, Transform parent)
        {
            for (float x = xa; x <= xb + 0.01f; x += 6f)
            {
                Put("PROP_Telegraph_Pole", x, z, 0f, 1f, 0f, parent);
                if (x + 6f <= xb + 0.01f) Put("PROP_Telegraph_Wire_Span_6m", x, z, 0f, 1f, 0f, parent);
            }
        }

        // ---------------------------------------------------------------- muros
        static bool InGaps(float a, float b, float[] g)
        {
            if (g == null) return false;
            for (int i = 0; i + 1 < g.Length; i += 2) if (b > g[i] + 0.01f && a < g[i + 1] - 0.01f) return true;
            return false;
        }

        /// <summary>Muro corrido a lo largo de X. gaps = pares [a,b] alineados a la rejilla de 2 m del muro.</summary>
        public static void WallX(float z, float xa, float xb, Transform parent, float[] gaps = null, float h = 1f, int windowEvery = 0)
        {
            int n = Mathf.RoundToInt((xb - xa) / 2f);
            for (int i = 0; i < n; i++)
            {
                float a = xa + 2f * i;
                if (InGaps(a, a + 2f, gaps)) continue;
                bool win = windowEvery > 0 && i % windowEvery == windowEvery - 1;
                PutS(win ? "WALL_Adobe_Window_2x3" : "WALL_Adobe_Straight_2x3", a + 1f, z, 0f, new Vector3(1f, h, 1f), 0f, parent);
                if (win) Put("WINDOW_Shutter_Wood_Pair", a + 1f, z - 0.25f, 0f, 1f, 1f, parent);
            }
            Posts(true, z, xa, xb, gaps, h, parent);
        }

        /// <summary>Muro corrido a lo largo de Z (yaw 90).</summary>
        public static void WallZ(float x, float za, float zb, Transform parent, float[] gaps = null, float h = 1f, int windowEvery = 0)
        {
            int n = Mathf.RoundToInt((zb - za) / 2f);
            for (int i = 0; i < n; i++)
            {
                float a = za + 2f * i;
                if (InGaps(a, a + 2f, gaps)) continue;
                PutS("WALL_Adobe_Straight_2x3", x, a + 1f, 90f, new Vector3(1f, h, 1f), 0f, parent);
            }
            Posts(false, x, za, zb, gaps, h, parent);
        }

        static void Posts(bool alongX, float fixedCoord, float a, float b, float[] gaps, float h, Transform parent)
        {
            var pts = new List<float> { a, b };
            if (gaps != null) for (int i = 0; i < gaps.Length; i++) if (gaps[i] > a - 0.01f && gaps[i] < b + 0.01f) pts.Add(gaps[i]);
            foreach (var p in pts.Distinct())
                PutS("WALL_Corner_Outer_Adobe", alongX ? p : fixedCoord, alongX ? fixedCoord : p, 0f, new Vector3(1f, h, 1f), 0f, parent);
        }

        /// <summary>Puerta corrediza industrial: marco (2 pilares + dintel) y hoja recogida junto al muro.</summary>
        public static Transform PuertaCorrediza(string id, float cx, float cz, bool alongZ, Transform parent, float halfGap = 2f)
        {
            var g = Group(id, parent, new Vector3(cx, 0f, cz));
            float yaw = alongZ ? 90f : 0f;
            float dx = alongZ ? 0f : 1f, dz = alongZ ? 1f : 0f;
            for (int s = -1; s <= 1; s += 2) Put("PILLAR_Wood_Square", dx * s * halfGap, dz * s * halfGap, yaw, 1f, 0f, g);
            for (float o = -halfGap + 1f; o <= halfGap - 1f + 0.01f; o += 2f) Put("BEAM_Wood_2m", dx * o, dz * o, yaw, 1f, 2.75f, g);
            float off = halfGap + 1.3f;
            float px = alongZ ? -0.35f : dx * off, pz = alongZ ? dz * off : -0.35f;
            var hoja = Put("DOOR_Warehouse_Sliding_Pair", px, pz, yaw, 1f, 0f, g);
            hoja.name = "Hoja_Corrediza";
            return g;
        }

        // ---------------------------------------------------------------- edificios
        /// <summary>Casa de adobe de 4 m de ancho como las de Level03 (frente, esquinas, techo a dos aguas).</summary>
        public static void Casa(float cx, float zf, Transform parent, bool doorRight)
        {
            string left = doorRight ? "WALL_Adobe_Window_2x3" : "WALL_Adobe_Door_2x3";
            string right = doorRight ? "WALL_Adobe_Door_2x3" : "WALL_Adobe_Window_2x3";
            Put(left, cx - 1f, zf, 0f, 1f, 0f, parent);
            Put(right, cx + 1f, zf, 0f, 1f, 0f, parent);
            float wx = doorRight ? cx - 1f : cx + 1f, dxx = doorRight ? cx + 1f : cx - 1f;
            Put("WINDOW_Shutter_Wood_Pair", wx, zf - 0.25f, 0f, 1f, 1f, parent);
            Put("FRAME_Door_Wood_1x2", dxx, zf, 0f, 1f, 0f, parent);
            Put("DOOR_Wood_Single", dxx - 0.42f, zf - 0.12f, 0f, 1f, 0f, parent);
            float zb = zf + 3.59f;
            foreach (var xs in new[] { cx - 2.05f, cx + 2.05f })
            {
                Put("WALL_Corner_Outer_Adobe", xs, zf, 0f, 1f, 0f, parent);
                Put("WALL_Corner_Outer_Adobe", xs, zb, 0f, 1f, 0f, parent);
                PutS("WALL_Adobe_Straight_2x3", xs, zf + 1.2f, 90f, new Vector3(0.9f, 1f, 1f), 0f, parent);
                PutS("WALL_Adobe_Straight_2x3", xs, zf + 3.0f, 90f, new Vector3(0.9f, 1f, 1f), 0f, parent);
            }
            Put("WALL_Adobe_Straight_2x3", cx - 1f, zb, 0f, 1f, 0f, parent);
            Put("WALL_Adobe_Straight_2x3", cx + 1f, zb, 0f, 1f, 0f, parent);
            Put("ROOF_GableEnd_Adobe_4m", cx, zf, 0f, 1f, 3f, parent);
            for (int k = 0; k < 2; k++)
            {
                float zr = zf + 1f + 2f * k;
                Put("ROOF_Tile_Slope_2x2", cx - 1f, zr, 90f, 1f, 3f, parent);
                Put("ROOF_Tile_Slope_2x2", cx + 1f, zr, 270f, 1f, 3f, parent);
                Put("ROOF_Ridge_2m", cx, zr, 90f, 1f, 3.94f, parent);
            }
        }

        /// <summary>Fogata/brasero como la de Level03 (anillo de rocas + vigas + luz).</summary>
        public static void Fogata(string id, float cx, float cz, Transform parent, float intensity = 8f, float range = 11f)
        {
            var g = Group(id, parent, new Vector3(cx, 0f, cz));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Put("ROCK_02_Medium", Mathf.Cos(a) * 1.0f, Mathf.Sin(a) * 1.0f, Rnd(0, 360), 1.3f, 0f, g);
            }
            for (int i = 0; i < 3; i++) Put("BEAM_Wood_2m", 0f, 0f, i * 60f, 0.8f, 0.3f, g, 0f, 22f);
            AddPoint(g, "Luz_Fogata", new Vector3(0f, 0.9f, 0f), LuzFuego, intensity, range);
        }

        /// <summary>Pilar monumental para accesos (columna + cornisa).</summary>
        static void Pilar(string id, float x, float z, float h, Transform parent)
        {
            var g = Group(id, parent, new Vector3(x, 0f, z));
            PutS("WALL_Corner_Outer_Adobe", 0f, 0f, 0f, new Vector3(2.4f, h / 3.02f, 2.4f), 0f, g);
            PutS("TRIM_Cornice_2m", 0f, -0.75f, 0f, new Vector3(0.8f, 1.6f, 7f), h - 0.2f, g);
        }

        // ---------------------------------------------------------------- dispersion
        static bool InAny(Rect[] rs, float x, float z)
        {
            if (rs == null) return false;
            foreach (var r in rs) if (r.Contains(new Vector2(x, z))) return true;
            return false;
        }
        static Rect R(float x0, float z0, float x1, float z1) => new Rect(x0, z0, x1 - x0, z1 - z0);

        static int Scatter(string[] names, float x0, float x1, float z0, float z1, int count, Transform parent,
            float smin, float smax, float minDist, Rect[] avoid = null, bool fullRot = true)
        {
            int done = 0, tries = 0;
            while (done < count && tries++ < count * 60)
            {
                float x = Rnd(x0, x1), z = Rnd(z0, z1);
                if (InAny(avoid, x, z)) continue;
                var p = new Vector2(x, z);
                bool close = false;
                foreach (var q in placed) if ((q - p).sqrMagnitude < minDist * minDist) { close = true; break; }
                if (close) continue;
                Put(names[rng.Next(names.Length)], x, z, fullRot ? Rnd(0, 360) : 0f, Rnd(smin, smax), 0f, parent);
                placed.Add(p);
                done++;
            }
            return done;
        }

        static void Cluster(float cx, float cz, string[] names, int n, float radius, Transform parent, float smin = 1f, float smax = 1f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = Rnd(0, Mathf.PI * 2f), r = Rnd(0.3f, radius);
                Put(names[rng.Next(names.Length)], cx + Mathf.Cos(a) * r, cz + Mathf.Sin(a) * r, Rnd(0, 360), Rnd(smin, smax), 0f, parent);
            }
        }

        // ---------------------------------------------------------------- engranes (MAT_Reja_Engrane / Hierro)
        static Transform Gear(string name, Vector3 center, float radius, int teeth, float thickness, Vector3 euler,
            Material body, Material hub, Transform parent)
        {
            var g = Group(name, parent, center);
            g.localEulerAngles = euler;
            Prim(PrimitiveType.Cylinder, "Disco", Vector3.zero, new Vector3(radius * 2f, thickness * 0.5f, radius * 2f), Vector3.zero, body, g, false, false);
            Prim(PrimitiveType.Cylinder, "Buje", new Vector3(0f, thickness * 0.3f, 0f), new Vector3(radius * 0.7f, thickness * 0.5f, radius * 0.7f), Vector3.zero, hub, g, false, false);
            float tw = Mathf.PI * radius / teeth;
            for (int i = 0; i < teeth; i++)
            {
                float a = i * Mathf.PI * 2f / teeth;
                Prim(PrimitiveType.Cube, "Diente", new Vector3(Mathf.Cos(a) * radius * 1.04f, 0f, Mathf.Sin(a) * radius * 1.04f),
                    new Vector3(radius * 0.26f, thickness, tw), new Vector3(0f, -a * Mathf.Rad2Deg, 0f), body, g, false, false);
            }
            return g;
        }
    }
}
#endif

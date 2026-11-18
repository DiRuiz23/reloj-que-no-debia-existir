#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    public static partial class Level04ArtBuilder
    {
        // ============================================================ ZONA 4 - ESTACION SAN ROQUE (x 16..50)  [hub]
        public static void BuildZona4()
        {
            Init(404);
            var z = Fresh("Zona_Estacion", util);
            Tiles(SueloTile, 16f, 50f, -8f, 12f, z, 0f);
            Tiles(SueloTierra, 16f, 50f, -10f, -8f, z, 0.1f);
            Tiles(SueloTile, 16f, 50f, 12f, 14f, z, 0f);

            var ed = Group("Estacion_Edificio", z, Vector3.zero);
            for (float x = 20f; x <= 48f; x += 4f) Put("FACADE_Station_4x3", x, 13.6f, 0f, 1f, 0f, ed);
            Put("PROP_Clock_Wall_Station", 36f, 13.28f, 0f, 3f, 3.5f, ed);
            Put("PROP_Poster_Timetable", 30.8f, 13.2f, 0f, 1f, 1.8f, ed);
            Put("PROP_Poster_Wanted", 41.5f, 13.2f, 0f, 1f, 1.8f, ed);
            Put("PROP_Notice_Board_Cork", 26.6f, 13.2f, 0f, 1f, 1.6f, ed);

            // marquesina: pilares + vigas + tejado (solo en z>=11.3, no tapa al jugador)
            var mq = Group("Marquesina", z, Vector3.zero);
            for (float x = 21f; x <= 45f; x += 6f) Put("PILLAR_Wood_Square", x, 11.5f, 0f, 1f, 0f, mq);
            for (float x = 20f; x <= 48f; x += 2f) Put("ROOF_Tile_Slope_2x2", x, 12.4f, 0f, 1f, 3f, mq);
            for (float x = 20f; x <= 48f; x += 2f) Put("BEAM_Wood_2m", x, 11.5f, 0f, 1f, 3.0f, mq);

            var det = Group("Estacion_Detalles", z, Vector3.zero);
            Put("PROP_TicketBooth_Taquilla_200", 24f, 11.9f, 0f, 1f, 0f, det);
            Put("PROP_Sign_Station_Name", 30f, 11.6f, 0f, 1f, 0f, det);
            foreach (var x in new[] { 19f, 36f, 42f, 48f }) Put("PF_Banca", x, 12.4f, 0f, 1f, 0f, det);
            Put("PROP_Lamp_Hanging_Oil", 33f, 12.0f, 0f, 1f, 3.0f, det);
            AddPoint(det.GetChild(det.childCount - 1), "Luz_Colgante", new Vector3(0f, -0.6f, 0f), LuzCalida, 5f, 8f);
            Put("PROP_Lamp_Hanging_Oil", 45f, 12.0f, 0f, 1f, 3.0f, det);
            AddPoint(det.GetChild(det.childCount - 1), "Luz_Colgante", new Vector3(0f, -0.6f, 0f), LuzCalida, 5f, 8f);
            Put("PF_Maleta", 40.2f, 12.1f, 15f, 1f, 0f, det);
            Put("PROP_Suitcase_Leather", 22.2f, 12.3f, 70f, 1f, 0f, det);
            Put("PROP_Trunk_Luggage", 34.4f, 12.3f, 20f, 1f, 0f, det);
            Put("PROP_Bin_Waste_Metal", 28f, 12.3f, 0f, 1f, 0f, det);
            Put("PROP_Lantern_Rail_Hand", 46.6f, 12.2f, 30f, 1f, 0f, det);
            Put("PROP_Signal_Semaphore", 17.5f, 6.4f, 0f, 1f, 0f, det);

            // coberturas del anden (sigilo / reagrupacion)
            var cob = Group("Cobertura", z, Vector3.zero);
            var pts = new[] { new Vector3(22f, -3f, 0f), new Vector3(28f, 3f, 90f), new Vector3(35f, -5f, 0f), new Vector3(40f, 2f, 0f), new Vector3(45.5f, -3.5f, 90f), new Vector3(47f, 5f, 0f) };
            foreach (var p in pts) { Put("PF_PilaCajas", p.x, p.y, p.z, 1f, 0f, cob); Cluster(p.x, p.y, Cajas, 2, 2.2f, cob); }
            foreach (var p in new[] { new Vector2(25f, -6.5f), new Vector2(38f, 6.5f), new Vector2(43f, -7.5f) }) Put("PF_PilaBarriles", p.x, p.y, 0f, 1f, 0f, cob);
            Put("PROP_Wheelbarrow", 31f, 6.6f, 40f, 1f, 0f, cob);
            Put("PROP_Wheelbarrow", 24.5f, 7f, 200f, 1f, 0f, cob);
            Put("PROP_Cargo_Bale", 33f, -1.5f, 60f, 1f, 0f, cob);
            Put("PROP_Planks_Stack_2m", 20f, -6f, 0f, 1f, 0f, cob);

            foreach (var p in new[] { new Vector2(20f, 6.5f), new Vector2(30f, -6.8f), new Vector2(36f, 6.6f), new Vector2(44f, -6.8f), new Vector2(48.4f, 6.6f), new Vector2(19f, -3f) })
                Put("PF_Farol", p.x, p.y, 0f, 1f, 0f, z);

            var pel = Group("Peligros_Nopales_Visual", z, Vector3.zero);
            foreach (var p in new[] { new Vector2(19f, -9.2f), new Vector2(26.5f, -9.3f), new Vector2(33f, -9.1f), new Vector2(40f, -9.3f), new Vector2(47f, -9.2f) })
                Put("VEG_Cactus_Nopal", p.x, p.y, Rnd(-15, 15), 1f, 0f, pel);
            Scatter(RocasBajas, 16f, 50f, -10.8f, -9.6f, 8, z, 0.8f, 1.4f, 3.5f);
            Scatter(Blandos, 16f, 50f, -10.8f, -8.2f, 14, z, 0.8f, 1.2f, 2f);
        }

        // ============================================================ ZONA 5 - ARENA DEL GUARDIAN (x 50..94)
        public static void BuildZona5()
        {
            Init(505);
            var z = Fresh("Zona_Arena_Guardian", util);
            var hierro = Mat("Materials/Level2/MAT_Reja_Hierro");
            var engrane = Mat("Materials/Level2/MAT_Reja_Engrane");
            var acento = Mat("Textures/Material/Mat_Acento_Reloj");

            // suelo en forma de reloj: anillos concentricos alrededor de (73, 1)
            var fl = Group("Suelo_Reloj", z, Vector3.zero);
            var c = new Vector2(73f, 1f);
            for (float x = 51f; x < 94f; x += 2f)
                for (float zz = -9f; zz < 12f; zz += 2f)
                {
                    float r = Vector2.Distance(new Vector2(x, zz), c);
                    string n = r < 3.2f ? "FLOOR_Tile_Worn_2x2" : r < 8.5f ? "FLOOR_Tile_2x2" : r < 10.8f ? "PAVE_Stone_Straight_2m" : r < 15f ? "FLOOR_Tile_Worn_2x2" : "FLOOR_Dirt_Packed_2x2";
                    Put(n, x, zz, 90f * rng.Next(4), 1f, 0f, fl);
                }
            Tiles(SueloTierra, 50f, 52f, -10f, 12f, fl, 0f);
            Gear("Engranaje_Suelo", new Vector3(73f, 0.1f, 1f), 2.6f, 16, 0.06f, Vector3.zero, engrane, hierro, fl);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                float s = (i % 3 == 0) ? 1.1f : 0.7f;
                Prim(PrimitiveType.Cylinder, "Marca_Hora", new Vector3(73f + Mathf.Cos(a) * 9.65f, 0.1f, 1f + Mathf.Sin(a) * 9.65f), new Vector3(s, 0.02f, s), Vector3.zero, engrane, fl, false, false);
            }

            // perimetro
            var m = Group("Muros", z, Vector3.zero);
            WallZ(50f, -10f, 12f, m, new[] { -4f, 6f });
            WallX(-10.4f, 50f, 94f, m, null, 0.45f);
            WallZ(94f, -10f, 12f, m, new[] { -4f, 0f });
            WallX(11.4f, 90f, 94f, m);
            PutS("WALL_Adobe_Straight_2x3", 68.5f, 11.4f, 0f, new Vector3(0.5f, 1f, 1f), 0f, m);
            PutS("WALL_Adobe_Straight_2x3", 77.5f, 11.4f, 0f, new Vector3(0.5f, 1f, 1f), 0f, m);
            var cs = Group("Casas_Norte", z, Vector3.zero);
            Casa(54f, 11.4f, cs, true); Casa(58f, 11.4f, cs, false); Casa(62f, 11.4f, cs, true); Casa(66f, 11.4f, cs, false);
            Casa(80f, 11.4f, cs, false); Casa(84f, 11.4f, cs, true); Casa(88f, 11.4f, cs, false);

            // acceso monumental con reja (abierta visible; cerrada inactiva para el encuentro)
            var ac = Group("Acceso_Arena", z, Vector3.zero);
            Pilar("Pilar_Acceso_Sur", 50f, -4f, 4.4f, ac);
            Pilar("Pilar_Acceso_Norte", 50f, 6f, 4.4f, ac);
            for (float zz = -3f; zz <= 5f; zz += 2f) Put("BEAM_Wood_2m", 50f, zz, 90f, 1.6f, 3.2f, ac);
            var abierta = Group("Reja_Arena_Abierta", ac, new Vector3(50f, 0f, 1f));
            for (float zz = -4.5f; zz <= 4.5f; zz += 0.5f) Prim(PrimitiveType.Cube, "Barra", new Vector3(0f, 3.95f, zz), new Vector3(0.12f, 1.1f, 0.12f), Vector3.zero, hierro, abierta);
            Prim(PrimitiveType.Cube, "Riel_Inferior", new Vector3(0f, 3.4f, 0f), new Vector3(0.16f, 0.14f, 10f), Vector3.zero, hierro, abierta);
            Prim(PrimitiveType.Cube, "Riel_Superior", new Vector3(0f, 4.45f, 0f), new Vector3(0.16f, 0.14f, 10f), Vector3.zero, hierro, abierta);
            var cerrada = Group("Reja_Arena_Cerrada", ac, new Vector3(50f, 0f, 1f));
            for (float zz = -4.5f; zz <= 4.5f; zz += 0.5f) Prim(PrimitiveType.Cube, "Barra", new Vector3(0f, 1.6f, zz), new Vector3(0.12f, 3.2f, 0.12f), Vector3.zero, hierro, cerrada);
            foreach (var yy in new[] { 0.3f, 1.6f, 3.0f }) Prim(PrimitiveType.Cube, "Riel", new Vector3(0f, yy, 0f), new Vector3(0.16f, 0.14f, 10f), Vector3.zero, hierro, cerrada);
            Gear("Engranaje_Reja", new Vector3(-0.1f, 1.9f, 0f), 0.9f, 12, 0.12f, new Vector3(0f, 0f, 90f), engrane, hierro, cerrada);
            var bc = cerrada.gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.4f, 3.2f, 10f); bc.center = new Vector3(0f, 1.6f, 0f);
            cerrada.gameObject.SetActive(false);

            // torre del reloj (fondo central)
            Torre("Torre_del_Reloj", 73f, 11.4f, z, engrane, hierro, acento);

            // plataforma del Guardian (aun sin villano)
            var dais = Group("Plataforma_Guardian", z, Vector3.zero);
            foreach (var x in new[] { 71f, 73f, 75f }) foreach (var zz in new[] { 5.4f, 7.4f }) Put("PLATFORM_Wood_Raised_2x2", x, zz, 0f, 1f, 0f, dais);
            foreach (var x in new[] { 71f, 73f, 75f }) Put("STEP_Platform_2x1", x, 4.4f, 180f, 1f, 0f, dais);

            // locomotora y espuela
            var tren = Group("Locomotora_Guardian", z, Vector3.zero);
            RailsX(9f, 77f, 93f, tren);
            Put("BUFFER_Stop_Timber", 74.4f, 9f, 90f, 1f, 0f, tren);
            Put("PF_Locomotora", 82f, 9f, 0f, 1f, 0f, tren);
            Put("PF_Tender", 89.7f, 9f, 0f, 1f, 0f, tren);
            Put("PROP_Signal_Semaphore", 76.2f, 6.6f, 0f, 1f, 0f, tren);

            // coberturas
            var cob = Group("Cobertura", z, Vector3.zero);
            foreach (var p in new[] { new Vector3(58f, -6.5f, 0f), new Vector3(60f, 7.5f, 90f), new Vector3(66f, -8.2f, 0f), new Vector3(80f, -8.2f, 90f), new Vector3(88f, -5.5f, 0f), new Vector3(89f, 3.5f, 90f), new Vector3(55.5f, 5f, 0f) })
            { Put("PF_PilaCajas", p.x, p.y, p.z, 1f, 0f, cob); Cluster(p.x, p.y, Cajas, 2, 2.2f, cob); }
            foreach (var p in new[] { new Vector2(62f, -4.2f), new Vector2(84f, -3.4f), new Vector2(66f, 7.6f) }) Put("PF_PilaBarriles", p.x, p.y, 0f, 1f, 0f, cob);

            // fuegos y luces
            var lu = Group("Luces_Arena", z, Vector3.zero);
            Fogata("Brasero_01", 56.5f, -7.8f, lu); Fogata("Brasero_02", 56.5f, 8.2f, lu);
            Fogata("Brasero_03", 90f, -7.8f, lu); Fogata("Brasero_04", 63f, -9.3f, lu);
            foreach (var p in new[] { new Vector2(52.8f, -8.5f), new Vector2(52.8f, 9f), new Vector2(68f, -9.4f), new Vector2(78f, -9.4f), new Vector2(91f, -3f), new Vector2(66f, 9.2f) })
                Put("PF_Farol", p.x, p.y, 0f, 1f, 0f, lu);

            // salida hacia la secuencia final (I-33)
            var sa = Group("Arco_Salida", z, Vector3.zero);
            Pilar("Pilar_Salida_Sur", 94f, -4f, 4f, sa);
            Pilar("Pilar_Salida_Norte", 94f, 0f, 4f, sa);
            for (float zz = -3f; zz <= -1f; zz += 2f) Put("BEAM_Wood_2m", 94f, zz, 90f, 1.6f, 3.2f, sa);
            Put("PROP_Clock_Wall_Station", 93.55f, -2f, 90f, 2.2f, 3.85f, sa);
            Prim(PrimitiveType.Cube, "Portal_Reloj_Salida", new Vector3(94.35f, 1.5f, -2f), new Vector3(0.05f, 3f, 3.6f), Vector3.zero, acento, sa, false, false);
            AddPoint(sa, "Luz_Salida", new Vector3(92.8f, 2.2f, -2f), new Color(1f, 0.85f, 0.55f), 8f, 10f);

            // peligros visibles (nopales)
            var pel = Group("Peligros_Nopales_Visual", z, Vector3.zero);
            foreach (var p in new[] { new Vector2(53.5f, -9.2f), new Vector2(92f, -9.2f), new Vector2(55f, 10.2f), new Vector2(79f, -9.6f), new Vector2(60f, 10.2f), new Vector2(92.5f, 5f) })
                Put("VEG_Cactus_Nopal", p.x, p.y, Rnd(-15, 15), 1f, 0f, pel);
            Scatter(RocasBajas, 50f, 94f, -11f, -10.5f, 8, z, 0.8f, 1.4f, 4f);
        }

        static void Torre(string id, float cx, float zf, Transform parent, Material engrane, Material hierro, Material acento)
        {
            var g = Group(id, parent, Vector3.zero);
            for (int lv = 0; lv < 3; lv++)
            {
                float y = lv * 3f;
                for (int i = 0; i < 4; i++)
                {
                    float x = cx - 3f + 2f * i;
                    string p = lv == 0 ? (i == 1 ? "WALL_Adobe_Door_2x3" : i == 2 ? "WALL_Adobe_Window_2x3" : "WALL_Adobe_Straight_2x3")
                             : lv == 1 ? "WALL_Adobe_Straight_2x3" : "WALL_Adobe_Window_2x3";
                    Put(p, x, zf, 0f, 1f, y, g);
                    if (lv == 2) Put("WINDOW_Shutter_Wood_Pair", x, zf - 0.25f, 0f, 1f, y + 1f, g);
                    if (lv == 0 && i == 2) Put("WINDOW_Shutter_Wood_Pair", x, zf - 0.25f, 0f, 1f, 1f, g);
                    if (lv == 0 && i == 1) { Put("FRAME_Door_Wood_1x2", x, zf, 0f, 1f, 0f, g); Put("DOOR_Wood_Single", x - 0.42f, zf - 0.12f, 0f, 1f, 0f, g); }
                }
                foreach (var sx in new[] { cx - 4f, cx + 4f })
                {
                    Put("WALL_Adobe_Straight_2x3", sx, zf + 1f, 90f, 1f, y, g);
                    Put("WALL_Adobe_Straight_2x3", sx, zf + 3f, 90f, 1f, y, g);
                    Put("WALL_Corner_Outer_Adobe", sx, zf, 0f, 1f, y, g);
                    Put("WALL_Corner_Outer_Adobe", sx, zf + 4f, 0f, 1f, y, g);
                }
            }
            for (int i = 0; i < 4; i++) Put("TRIM_Cornice_2m", cx - 3f + 2f * i, zf - 0.17f, 180f, 1f, 9f, g);
            for (int i = 0; i < 4; i++)
            {
                float x = cx - 3f + 2f * i;
                Put("ROOF_Tile_Slope_2x2", x, zf + 1f, 0f, 1f, 9f, g);
                Put("ROOF_Tile_Slope_2x2", x, zf + 3f, 180f, 1f, 9f, g);
                Put("ROOF_Ridge_2m", x, zf + 2f, 0f, 1f, 9.94f, g);
            }
            Put("ROOF_GableEnd_Adobe_4m", cx - 4f, zf + 2f, 90f, 1f, 9f, g);
            Put("ROOF_GableEnd_Adobe_4m", cx + 4f, zf + 2f, 90f, 1f, 9f, g);

            // esfera del reloj + halo ambar + engranes laterales
            Prim(PrimitiveType.Cylinder, "Halo_Reloj", new Vector3(cx, 4.6f, zf - 0.2f), new Vector3(4.2f, 0.04f, 4.2f), new Vector3(90f, 0f, 0f), acento, g, false, false);
            Put("PROP_Clock_Wall_Station", cx, zf, 0f, 5f, 4.6f, g);
            Gear("Engranaje_Izq", new Vector3(cx - 2.85f, 4.6f, zf - 0.3f), 0.95f, 12, 0.25f, new Vector3(90f, 0f, 0f), engrane, hierro, g);
            Gear("Engranaje_Der", new Vector3(cx + 2.85f, 4.6f, zf - 0.3f), 0.95f, 12, 0.25f, new Vector3(90f, 0f, 0f), engrane, hierro, g);
            AddPoint(g, "Luz_Reloj", new Vector3(cx, 4.6f, zf - 2.8f), new Color(1f, 0.78f, 0.35f), 10f, 12f);
        }

        // ============================================================ FONDOS (parallax)
        static Transform Parallax(string name, float factor)
        {
            var t = Fresh(name, null);
            var pl = t.gameObject.AddComponent<Reloj.Art.ParallaxLayer>();
            var so = new SerializedObject(pl);
            so.FindProperty("factor").floatValue = factor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        static void SinColision(Transform t)
        {
            foreach (var c in t.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        public static void BuildFondos()
        {
            Init(606);

            // lejano (0.85): lomas, rocas y cactus gigantes
            var le = Parallax("Parallax_Lejano", 0.85f);
            for (float x = -125f; x <= 105f; x += Rnd(7f, 10f)) Put("MOUND_Dirt_2x2", x, Rnd(39f, 47f), 270f, Rnd(9f, 13f), -1f, le.transform);
            Scatter(new[] { "ROCK_05_Tall" }, -125f, 105f, 28f, 38f, 16, le, 8f, 14f, 7f);
            Scatter(new[] { "ROCK_03_Large" }, -125f, 105f, 30f, 40f, 14, le, 6f, 10f, 7f);
            Scatter(new[] { "VEG_Cactus_Saguaro_Tall" }, -125f, 105f, 28f, 36f, 12, le, 4f, 6f, 8f);
            Scatter(new[] { "VEG_Tree_Mesquite" }, -125f, 105f, 30f, 38f, 8, le, 3f, 4.5f, 8f);
            SinColision(le);

            // medio (0.6): caserio lejano, siding con vagones y arboles
            var me = Parallax("Parallax_Medio", 0.6f);
            bool door = true;
            for (float x = -125f; x <= 35f; x += Rnd(8f, 11f)) { Casa(x, 23f, me, door); door = !door; }
            RailsX(20f, -122f, -60f, me);
            foreach (var x in new[] { -112f, -103f, -94f, -80f }) Put("PF_Vagon", x, 20f, 0f, 1f, 0f, me);
            Scatter(new[] { "VEG_Tree_Mesquite" }, -125f, 35f, 18.5f, 28f, 14, me, 1.2f, 1.8f, 6f);
            Scatter(new[] { "VEG_Cactus_Saguaro_Tall" }, -125f, 35f, 18.5f, 28f, 10, me, 1.2f, 1.8f, 6f);
            Scatter(Rocas, -125f, 35f, 18.5f, 28f, 12, me, 2f, 3.5f, 5f);
            SinColision(me);

            // cercano (1.0): vegetacion y casas fijas al mundo
            var ce = Parallax("Parallax_Cercano", 1f);
            door = false;
            foreach (var x in new[] { -88f, -52f, -14f, 20f, 58f, 86f }) { Casa(x, 20f, ce, door); door = !door; }
            Scatter(new[] { "VEG_Tree_Mesquite" }, -95f, 96f, 18.5f, 30f, 26, ce, 1.3f, 2f, 5f, new[] { R(-90f, 19f, 96f, 25f) });
            Scatter(new[] { "VEG_Cactus_Saguaro_Tall" }, -95f, 96f, 18.5f, 30f, 14, ce, 1.2f, 1.8f, 6f, new[] { R(-90f, 19f, 96f, 25f) });
            Scatter(Rocas, -95f, 96f, 18.5f, 30f, 16, ce, 2f, 3.5f, 5f, new[] { R(-90f, 19f, 96f, 25f) });
            Scatter(Blandos, -95f, 96f, 18.5f, 30f, 40, ce, 1.2f, 2f, 3f);
            SinColision(ce);
        }

        // ============================================================ LUCES Y POST-PROCESO
        public static void BuildLuces()
        {
            Init();
            var lg = Fresh("_Lighting", null);
            var at = lg.gameObject.AddComponent<Reloj.Art.LevelAtmosphere>();
            var so = new SerializedObject(at);
            so.FindProperty("paleta").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Data/UI/UIPalette.asset");
            so.FindProperty("usarNiebla").boolValue = true;
            so.FindProperty("nieblaInicio").floatValue = 26f;
            so.FindProperty("nieblaFin").floatValue = 105f;
            so.FindProperty("noche").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var sun = new GameObject("Luz_Direccional");
            sun.transform.SetParent(lg, false);
            sun.transform.localEulerAngles = new Vector3(30f, 300f, 0f);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional; l.color = new Color(1f, 0.70f, 0.45f); l.intensity = 1.9f;
            l.shadows = LightShadows.Soft; l.shadowBias = 0.05f; l.shadowNormalBias = 0.4f;
            sun.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();

            var pf = Fresh("_PostFX", null);
            var vg = new GameObject("Volume_HD2D");
            vg.transform.SetParent(pf, false);
            var v = vg.AddComponent<UnityEngine.Rendering.Volume>();
            v.isGlobal = true; v.priority = 10; v.weight = 1f;
            var alto = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/_Project/Data/Volumes/HD2D_Alto.asset");
            var bajo = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/_Project/Data/Volumes/HD2D_Bajo.asset");
            v.sharedProfile = alto;
            var sw = vg.AddComponent<Reloj.UI.VolumeProfileSwitcher>();
            var so2 = new SerializedObject(sw);
            so2.FindProperty("perfilAlto").objectReferenceValue = alto;
            so2.FindProperty("perfilBajo").objectReferenceValue = bajo;
            so2.ApplyModifiedPropertiesWithoutUndo();
        }

        // ============================================================ MARCADORES Y LIMITES
        public static void BuildMarcadoresLimites()
        {
            Init();
            var mk = Fresh("Marcadores", util);
            void M(string n, float x, float y, float zz) { var g = new GameObject(n); g.transform.SetParent(mk, false); g.transform.localPosition = new Vector3(x, y, zz); }
            M("Spawn_Jugador", -85f, 0f, 1.5f);
            M("Pista_Inicio_01", -87f, 0f, 4.9f); M("Pista_Inicio_02", -85f, 0f, 4.9f); M("Pista_Inicio_03", -88.2f, 0f, 3.6f);
            M("CP_01_Llegada", -60f, 0f, 0f); M("CP_02_Patio", -21f, 0f, 0f); M("CP_03_Estacion", 19f, 0f, 0f); M("CP_04_Antesala_Arena", 46f, 0f, 1f);
            M("Zona_Sigilo_1_Centro", -38f, 0f, 0f); M("Zona_Sigilo_2_Centro", 1f, 0f, 1f); M("Zona_Estacion_Centro", 33f, 0f, 2f);
            M("Puerta_Corrediza_01", -18f, 0f, 0f); M("Puerta_Corrediza_02", -4f, 0f, 8f); M("Puerta_Corrediza_03", 6f, 0f, -8f);
            M("Entrada_Arena", 50f, 0f, 1f); M("Trigger_Inicio_Encuentro", 57f, 0f, 1f);
            M("Arena_Centro", 73f, 0f, 1f); M("Guardian_Spawn", 73f, 0.6f, 6.4f);
            M("Meta_Salida_I33", 93.2f, 0f, -2f);
            M("Botiquin_Sugerido_01", -62f, 0f, -3f); M("Botiquin_Sugerido_02", 1f, 0f, -7.5f); M("Botiquin_Sugerido_03", 30f, 0f, -6f); M("Botiquin_Sugerido_04", 54.5f, 0f, -8.6f);
            M("Coleccionable_Sugerido_01", -38.5f, 0.9f, 9.4f); M("Coleccionable_Sugerido_02", 11f, 0.9f, 8.5f); M("Coleccionable_Sugerido_03", 46f, 0.9f, 10.5f); M("Coleccionable_Sugerido_04", 88f, 0.9f, -8.5f);

            var lim = Fresh("Limites_Jugable", util);
            void L(string n, float x, float zz, float sx, float sz) { var g = new GameObject(n); g.transform.SetParent(lim, false); g.transform.localPosition = new Vector3(x, 2f, zz); var b = g.AddComponent<BoxCollider>(); b.size = new Vector3(sx, 6f, sz); }
            L("Limite_Sur", 2f, -11.5f, 190f, 1f);
            L("Limite_Norte", 2f, 15.9f, 190f, 1f);
            L("Limite_Oeste", -91f, 2.2f, 1f, 28f);
            L("Limite_Este", 95f, 2.2f, 1f, 28f);
        }

        [MenuItem("Reloj/Level04 Art/Construir todo")]
        public static void BuildAll()
        {
            BuildGlobal(); BuildZona1(); BuildZona2(); BuildZona3(); BuildZona4(); BuildZona5();
            BuildFondos(); BuildLuces(); BuildMarcadoresLimites();
        }
    }
}
#endif

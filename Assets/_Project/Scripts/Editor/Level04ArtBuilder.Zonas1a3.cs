#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    public static partial class Level04ArtBuilder
    {
        static readonly string[] Rocas = { "ROCK_01_Small", "ROCK_02_Medium", "ROCK_03_Large", "ROCK_05_Tall", "ROCK_04_Flat" };
        static readonly string[] RocasBajas = { "ROCK_01_Small", "ROCK_02_Medium", "ROCK_04_Flat", "ROCK_03_Large" };
        static readonly string[] Blandos = { "VEG_Grass_Tuft_Dry", "VEG_Grass_Tuft_Dry", "VEG_Grass_Tuft_Dry", "VEG_Shrub_Dry", "VEG_Agave_Blue", "VEG_Bush_Desert_Green" };
        static readonly string[] SueloTierra = { "FLOOR_Dirt_Packed_2x2", "FLOOR_Dirt_Packed_2x2", "FLOOR_Dirt_2x2" };
        static readonly string[] SueloTile = { "FLOOR_Tile_2x2", "FLOOR_Tile_2x2", "FLOOR_Tile_Worn_2x2" };
        static readonly string[] Cajas = { "PROP_Crate_Wood_090", "PROP_Crate_Wood_060", "PROP_Crate_Wood_Large_120", "PROP_Barrel_Medium", "PROP_Barrel_Small", "PROP_Sack_Grain", "PROP_Pallet_Wood" };

        // ============================================================ GLOBAL
        public static void BuildGlobal()
        {
            Init();
            var suelo = Fresh("Suelo", util);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Base_Tierra";
            cube.transform.SetParent(suelo, false);
            cube.transform.localPosition = new Vector3(2f, -0.25f, 14f);
            cube.transform.localScale = new Vector3(190f, 0.5f, 80f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = Mat("Environment/Materials/MAT_Suelo_Tierra");
            cube.layer = LayerMask.NameToLayer("Ground");

            var via = Fresh("Via_Principal", util);
            RailsX(9f, -89f, 47f, via);
            Put("BUFFER_Stop_Timber", -91.6f, 9f, 90f, 1f, 0f, via);
            Put("BUFFER_Stop_Timber", 49.6f, 9f, 270f, 1f, 0f, via);

            var tel = Fresh("Telegrafo", util);
            Telegrafo(-90f, 90f, 16.4f, tel);
        }

        // ============================================================ ZONA 1 - LLEGADA (x -90..-58)
        public static void BuildZona1()
        {
            Init(101);
            var z = Fresh("Zona_Llegada", util);
            Tiles(SueloTierra, -90f, -58f, -5f, 7f, z, 0.1f);
            for (int i = 0; i < 9; i++) Put("GRAVEL_Patch_2x2", Rnd(-88f, -60f), Rnd(-4f, 6f), Rnd(0, 360), 1f, 0.02f, z);

            // casas del fondo (como Level03)
            Casa(-82f, 11f, z, true); Casa(-74f, 11f, z, false); Casa(-66f, 11f, z, true);

            // rincon de pistas (solo aqui apareceran las pistas del nivel)
            var rp = Group("Rincon_Pistas", z, Vector3.zero);
            WallX(7.2f, -88f, -84f, rp);
            Put("PROP_Notice_Board_Cork", -87f, 6.98f, 0f, 1f, 1.6f, rp);
            Put("PROP_Poster_Wanted", -85.3f, 6.98f, 0f, 1f, 1.8f, rp);
            Put("PROP_Poster_Timetable", -84.4f, 6.98f, 0f, 1f, 1.5f, rp);
            Put("PF_Banca", -86f, 5.6f, 0f, 1f, 0f, rp);
            Put("PF_Maleta", -88.4f, 4.4f, 20f, 1f, 0f, rp);
            Put("PROP_Trunk_Luggage", -88.6f, 2.9f, 70f, 1f, 0f, rp);
            Put("PROP_Lantern_Rail_Hand", -86.4f, 4.4f, 40f, 1f, 0f, rp);
            Put("PROP_Docs_DeskSet", -83.2f, 6.2f, 0f, 1f, 0f, rp);
            Put("PROP_Sign_Station_Name", -79.5f, 7.0f, 0f, 1f, 0f, z);

            // barrera ferroviaria rota + semaforo
            Put("BARRIER_Gate_Post", -62f, 6.2f, 0f, 1f, 0f, z);
            Put("BARRIER_Gate_Arm", -62f, 6.2f, 270f, 1f, 0.9f, z, 0f, 35f);
            Put("PROP_Signal_Semaphore", -58.6f, 6.9f, 0f, 1f, 0f, z);

            // faroles
            Put("PF_Farol", -79f, 6.4f, 0f, 1f, 0f, z);
            Put("PF_Farol", -64f, -4.6f, 0f, 1f, 0f, z);
            Put("PF_Farol", -88f, -3.4f, 0f, 1f, 0f, z);

            // peligros visibles (nopales)
            var pel = Group("Peligros_Nopales_Visual", z, Vector3.zero);
            foreach (var p in new[] { new Vector2(-76f, -6.2f), new Vector2(-69f, -5.8f), new Vector2(-62.5f, -7.4f), new Vector2(-83f, -7.2f) })
                Put("VEG_Cactus_Nopal", p.x, p.y, Rnd(-15, 15), 1f, 0f, pel);

            // bordes naturales (nada con colision en la ruta z -5..8)
            var ruta = new[] { R(-91f, -5.2f, -57f, 8f) };
            Scatter(Rocas, -90f, -58f, -10.8f, -6f, 20, z, 0.9f, 1.8f, 3.2f);
            Scatter(new[] { "VEG_Cactus_Saguaro_Tall" }, -90f, -58f, -10.8f, -7f, 5, z, 1.0f, 1.4f, 5f);
            Scatter(Blandos, -90f, -58f, -10.8f, 7.2f, 60, z, 0.8f, 1.3f, 1.4f, new[] { R(-89f, 5f, -82f, 7.4f) });
            for (int i = 0; i < 6; i++) Put("ROCK_03_Large", -92.6f, -10f + i * 5f, Rnd(0, 360), 3f, 0f, z);
        }

        // ============================================================ ZONA 2 - PATIO DE CARGA (x -58..-18)  [sigilo 1]
        public static void BuildZona2()
        {
            Init(202);
            var z = Fresh("Zona_Patio_Carga", util);
            Tiles(SueloTierra, -58f, -18f, -9f, 11f, z, 0.08f);
            for (int i = 0; i < 14; i++) Put("GRAVEL_Patch_2x2", Rnd(-56f, -20f), Rnd(-8f, 7f), Rnd(0, 360), 1f, 0.02f, z);

            // fachada de bodegas al fondo
            var fb = Group("Bodegas_Fondo", z, Vector3.zero);
            for (float x = -56f; x <= -20f; x += 4f) Put("FACADE_Warehouse_4x3", x, 12.4f, 0f, 1f, 0f, fb);

            // convoy parado en la via principal (muro norte; hueco = nicho)
            var tren = Group("Tren_Patio", z, Vector3.zero);
            foreach (var x in new[] { -52.5f, -44f, -31.8f, -23.2f }) Put("PF_Vagon", x, 9f, 0f, 1f, 0f, tren);
            Put("PROP_Crate_Wood_Large_120", -37.6f, 8.6f, 15f, 1f, 0f, tren);
            Put("PROP_Barrel_Medium", -38.8f, 10.2f, 0f, 1f, 0f, tren);
            Put("PROP_Lantern_Rail_Hand", -36.6f, 10.0f, 30f, 1f, 0f, tren);

            // desvios con topes e interruptores
            var vias = Group("Desvios", z, Vector3.zero);
            RailsX(3f, -56f, -36f, vias); Put("BUFFER_Stop_Timber", -33.7f, 3f, 270f, 1f, 0f, vias);
            RailsX(-4f, -48f, -24f, vias); Put("BUFFER_Stop_Timber", -21.7f, -4f, 270f, 1f, 0f, vias);
            Put("SWITCH_Stand_Lever", -58.4f, 5f, 0f, 1f, 0f, vias);
            Put("SWITCH_Stand_Lever", -50.4f, -2.3f, 0f, 1f, 0f, vias);

            // islas de cobertura
            var cob = Group("Cobertura", z, Vector3.zero);
            Put("PF_PilaCajas", -51f, -1.2f, 0f, 1f, 0f, cob);
            Put("PF_PilaCajas", -46f, 6f, 90f, 1f, 0f, cob);
            Put("PF_PilaBarriles", -42.5f, -6.2f, 0f, 1f, 0f, cob);
            Put("PF_PilaCajas", -38f, 1f, 0f, 1f, 0f, cob);
            Put("PF_PilaBarriles", -33f, 5.8f, 0f, 1f, 0f, cob);
            Put("PF_PilaCajas", -30f, -5.2f, 90f, 1f, 0f, cob);
            Put("PF_PilaCajas", -26.5f, 0.8f, 0f, 1f, 0f, cob);
            Put("PF_PilaBarriles", -22f, -2.4f, 0f, 1f, 0f, cob);
            foreach (var c in new[] { new Vector2(-51f, -1.2f), new Vector2(-46f, 6f), new Vector2(-42.5f, -6.2f), new Vector2(-38f, 1f), new Vector2(-33f, 5.8f), new Vector2(-30f, -5.2f), new Vector2(-26.5f, 0.8f), new Vector2(-22f, -2.4f) })
                Cluster(c.x + Rnd(-0.5f, 0.5f), c.y + Rnd(-0.3f, 0.3f), Cajas, 3, 2.4f, cob);
            Put("PROP_Wheelbarrow", -55f, 1f, 25f, 1f, 0f, cob);
            Put("PROP_Wheelbarrow", -28f, 6.6f, 160f, 1f, 0f, cob);
            Put("PROP_Cargo_Bale", -48f, -5.5f, 30f, 1f, 0f, cob);
            Put("PROP_Cargo_Bale", -35.5f, -2.3f, 80f, 1f, 0f, cob);
            Put("PROP_Planks_Stack_2m", -41f, 6.6f, 90f, 1f, 0f, cob);
            Put("PROP_Poster_Wanted", -34f, 12.1f, 0f, 1f, 1.5f, cob);

            // iluminacion
            foreach (var p in new[] { new Vector2(-56f, 6.2f), new Vector2(-41f, 6.9f), new Vector2(-27f, 6.9f), new Vector2(-47f, -7.6f), new Vector2(-33f, -7.8f), new Vector2(-20.5f, -7.6f) })
                Put("PF_Farol", p.x, p.y, 0f, 1f, 0f, z);

            // peligros visibles (nopales) y borde sur bajo
            var pel = Group("Peligros_Nopales_Visual", z, Vector3.zero);
            foreach (var p in new[] { new Vector2(-55f, -8.2f), new Vector2(-47f, -8.8f), new Vector2(-39.5f, -7.4f), new Vector2(-34f, -8.6f), new Vector2(-27f, -7.8f), new Vector2(-21f, -8.9f) })
                Put("VEG_Cactus_Nopal", p.x, p.y, Rnd(-15, 15), 1f, 0f, pel);
            Scatter(RocasBajas, -58f, -18f, -10.8f, -9.4f, 14, z, 0.9f, 1.6f, 3f);
            Scatter(Blandos, -58f, -18f, -10.5f, 7f, 40, z, 0.8f, 1.2f, 1.5f);
        }

        // ============================================================ ZONA 3 - TALLERES (x -18..16)  [sigilo 2 / Parada Temporal]
        public static void BuildZona3()
        {
            Init(303);
            var z = Fresh("Zona_Talleres", util);
            Tiles(SueloTile, -18f, 16f, -10f, 12f, z, 0f);

            // fachada norte del cobertizo
            var fb = Group("Fachada_Cobertizo", z, Vector3.zero);
            for (float x = -16f; x <= 16f; x += 4f) Put("FACADE_Warehouse_4x3", x, 12.4f, 0f, 1f, 0f, fb);

            // muros: borde sur bajo + tabiques de zigzag
            var m = Group("Muros", z, Vector3.zero);
            WallX(-10.2f, -18f, 16f, m, null, 0.45f);
            WallZ(-18f, -10f, 12f, m, new[] { -2f, 2f });   // entrada
            WallZ(-4f, -10f, 12f, m, new[] { 6f, 10f });    // tabique 1 (paso norte)
            WallZ(6f, -10f, 12f, m, new[] { -10f, -6f });   // tabique 2 (paso sur)
            WallZ(16f, -10f, 12f, m, new[] { -4f, 4f });    // salida a la estacion

            // puertas corredizas (futuras Parada Temporal)
            var pu = Group("Puertas_Corredizas", z, Vector3.zero);
            PuertaCorrediza("Puerta_Corrediza_01", -18f, 0f, true, pu);
            PuertaCorrediza("Puerta_Corrediza_02", -4f, 8f, true, pu);
            PuertaCorrediza("Puerta_Corrediza_03", 6f, -8f, true, pu);

            // armazon superior con lamparas colgantes
            var ar = Group("Armazon_Techo", z, Vector3.zero);
            foreach (var cx in new[] { -11f, 1f, 11f })
            {
                for (float zz = -9f; zz <= 11f; zz += 2f) Put("BEAM_Wood_2m", cx, zz, 90f, 1f, 3.1f, ar);
                Put("PILLAR_Wood_Square", cx, -9.7f, 90f, 1f, 0f, ar);
                Put("PILLAR_Wood_Square", cx, 11.2f, 90f, 1f, 0f, ar);
                foreach (var lz in new[] { -4f, 4f })
                {
                    var l = Put("PROP_Lamp_Hanging_Oil", cx, lz, 0f, 1f, 3.1f, ar);
                    AddPoint(l.transform, "Luz_Colgante", new Vector3(0f, -0.6f, 0f), LuzCalida, 5f, 8f);
                }
            }

            // camara A (x -18..-4)
            var a = Group("Camara_A_Taller", z, Vector3.zero);
            Put("PF_PilaCajas", -12.5f, -5.5f, 0f, 1f, 0f, a);
            Put("PF_PilaBarriles", -8f, -7.5f, 0f, 1f, 0f, a);
            Put("PF_PilaCajas", -14.5f, 6.4f, 90f, 1f, 0f, a);
            Put("PROP_Counter_Straight_200", -8.5f, 0.5f, 0f, 1f, 0f, a);
            Put("PROP_Chair_Wood", -8.8f, 2.0f, 200f, 1f, 0f, a);
            Put("PROP_Desk_Station_140", -6.6f, -3.8f, 90f, 1f, 0f, a);
            Put("PROP_Stove_Iron_Potbelly", -16.6f, -8.4f, 0f, 1f, 0f, a);
            Put("PROP_Docs_DeskSet", -8.5f, 0.5f, 0f, 1f, 0.78f, a);
            Put("PROP_Poster_Railway_Map", -10f, 12.1f, 0f, 1f, 1.8f, a);

            // camara B (x -4..6): mesa larga y estantes
            var b = Group("Camara_B_Mesas", z, Vector3.zero);
            Put("PROP_Counter_Straight_200", 1.8f, 5.5f, 90f, 1f, 0f, b);
            Put("PROP_Counter_Straight_200", 1.8f, -4.8f, 90f, 1f, 0f, b);
            Put("PF_PilaCajas", -1.2f, -1.5f, 0f, 1f, 0f, b);
            Put("PROP_Bookcase_Tall_100", -3.4f, -8.8f, 0f, 1f, 0f, b);
            Put("PROP_Books_Row_Set", 1.8f, 5.5f, 90f, 1f, 0.78f, b);
            Put("PROP_Bin_Waste_Metal", 4.8f, 3.0f, 0f, 1f, 0f, b);
            Put("PROP_Chair_Wood", 3.1f, 5.5f, 90f, 1f, 0f, b);
            Put("PROP_Frame_Painting_Desert", 1f, 12.1f, 0f, 1f, 1.8f, b);

            // camara C (x 6..16): almacen
            var c = Group("Camara_C_Almacen", z, Vector3.zero);
            Put("PF_PilaCajas", 12f, -3.8f, 0f, 1f, 0f, c);
            Put("PF_PilaBarriles", 13f, 6.8f, 0f, 1f, 0f, c);
            Put("PF_PilaCajas", 10.5f, 3.6f, 90f, 1f, 0f, c);
            Put("PROP_Trunk_Luggage", 14.8f, -8.6f, 30f, 1f, 0f, c);
            Put("PROP_Suitcase_Leather", 8.6f, 7f, 80f, 1f, 0f, c);
            Cluster(10.5f, 3.6f, Cajas, 3, 2.0f, c);
            Cluster(12f, -3.8f, Cajas, 3, 2.0f, c);
            Cluster(-12.5f, -5.5f, Cajas, 3, 2.2f, a);
            Cluster(1.5f, -1.5f, Cajas, 2, 2f, b);

            // luz de la puerta de entrada y entorno
            Put("PF_Farol", -20.3f, 5.5f, 0f, 1f, 0f, z);
            Put("PF_Farol", 14.8f, -8.6f, 0f, 1f, 0f, z);
        }
    }
}
#endif

// El Reloj que No Debia Existir - Importador de entorno 3D (Blender -> Unity)
// Ruta del kit en este proyecto: Assets/_Project/Art/Environment/ElReloj (ver Root). Requiere Unity 2022.3 LTS o superior.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ElRelojPostprocessor : AssetPostprocessor
{
    const string Root = "Assets/_Project/Art/Environment/ElReloj";
    const string MatDir = Root + "/Materials";
    static readonly Dictionary<GameObject, string> pending = new Dictionary<GameObject, string>();

    // ---------- Menu ----------
    [MenuItem("ElReloj/1 - Crear materiales")]
    static void CreateMaterials()
    {
        Directory.CreateDirectory(MatDir);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/TEX_Palette_16x16.png");
        if (tex == null) { Debug.LogError("Falta " + Root + "/Textures/TEX_Palette_16x16.png"); return; }
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        MakeMaterial("MAT_Palette", sh, tex, false);
        MakeMaterial("MAT_Glass_Clear", sh, tex, true);
        AssetDatabase.SaveAssets();
        Debug.Log("ElReloj: materiales listos en " + MatDir + ". Ahora reimporta la carpeta " + Root + "/FBX.");
    }

    [MenuItem("ElReloj/2 - Alternar tejado de la demo")]
    static void ToggleRoof()
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!t.gameObject.scene.IsValid() || !t.name.StartsWith("D_ROOF_")) continue;
            t.gameObject.SetActive(!t.gameObject.activeSelf);
        }
    }

    static void MakeMaterial(string name, Shader sh, Texture2D tex, bool glass)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.mainTexture = tex;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (glass)
        {
            var c = Color.white; c.a = 0.18f; m.color = c;
            if (m.HasProperty("_Surface"))            // URP
            {
                m.SetFloat("_Surface", 1f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else if (m.HasProperty("_Mode"))          // Standard
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
            }
        }
        EditorUtility.SetDirty(m);
    }

    // ---------- Texturas: paleta con filtrado Point ----------
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("TEX_Palette")) return;
        var ti = (TextureImporter)assetImporter;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.sRGBTexture = true;
        ti.maxTextureSize = 64;
    }

    // ---------- Modelos ----------
    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Root)) return;
        var mi = (ModelImporter)assetImporter;
        mi.globalScale = 1f;
        mi.useFileScale = true;
        mi.bakeAxisConversion = true;
        mi.importBlendShapes = false;
        mi.importCameras = false;
        mi.importLights = false;
        mi.importVisibility = false;
        mi.isReadable = false;
        mi.meshCompression = ModelImporterMeshCompression.Off;
        mi.animationType = ModelImporterAnimationType.None;
        mi.importNormals = ModelImporterNormals.Import;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        mi.materialLocation = ModelImporterMaterialLocation.External;
        mi.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        mi.materialSearch = ModelImporterMaterialSearch.Everywhere;
    }

    // Propiedades personalizadas de Blender: unity_collider, unity_interactable
    void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] names, object[] values)
    {
        if (!assetPath.StartsWith(Root)) return;
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == "unity_collider")
            {
                string kind = values[i] as string;
                if (!string.IsNullOrEmpty(kind) && kind != "none") pending[go] = kind;
            }
            else if (names[i] == "unity_interactable")
            {
                int layer = LayerMask.NameToLayer("Interactable");   // opcional: crea la capa "Interactable"
                if (layer >= 0) go.layer = layer;
            }
        }
    }

    void OnPostprocessModel(GameObject root)
    {
        foreach (var kv in pending)
        {
            var go = kv.Key;
            if (go == null) continue;
            var mf = go.GetComponent<MeshFilter>();
            Bounds b = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            switch (kv.Value)
            {
                case "box":
                {
                    var c = go.AddComponent<BoxCollider>(); c.center = b.center; c.size = b.size; break;
                }
                case "capsule":
                {
                    var c = go.AddComponent<CapsuleCollider>(); c.center = b.center;
                    c.radius = Mathf.Max(b.extents.x, b.extents.z); c.height = b.size.y; c.direction = 1; break;
                }
                case "sphere":
                {
                    var c = go.AddComponent<SphereCollider>(); c.center = b.center;
                    c.radius = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z)); break;
                }
                case "mesh_convex":
                {
                    var c = go.AddComponent<MeshCollider>(); c.convex = true; break;
                }
            }
        }
        pending.Clear();
    }
}

using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Herramientas de verificacion y empaquetado de rendimiento (Reloj > Rendimiento).
    /// </summary>
    public static class PerformanceOptimizer
    {
        [MenuItem("Reloj/Rendimiento/Empaquetar Sprite Atlas y Auditar Rendimiento", priority = 10)]
        public static void PackAndAudit()
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Art" });
            int compressedTex = 0;
            int noMipTex = 0;
            foreach (string g in texGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (AssetImporter.GetAtPath(path) is TextureImporter ti)
                {
                    if (!ti.mipmapEnabled) noMipTex++;
                    if (ti.textureCompression != TextureImporterCompression.Uncompressed) compressedTex++;
                }
            }

            string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" });
            int streamingAudio = 0;
            int adpcmOrVorbis = 0;
            foreach (string g in audioGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (AssetImporter.GetAtPath(path) is AudioImporter ai)
                {
                    var s = ai.defaultSampleSettings;
                    if (s.loadType == AudioClipLoadType.Streaming || s.loadType == AudioClipLoadType.CompressedInMemory)
                        streamingAudio++;
                    if (s.compressionFormat == AudioCompressionFormat.ADPCM || s.compressionFormat == AudioCompressionFormat.Vorbis)
                        adpcmOrVorbis++;
                }
            }

            string[] atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets/_Project/Art" });
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int shadowLights = 0;
            foreach (var l in lights)
                if (l != null && l.shadows != LightShadows.None) shadowLights++;

            Debug.Log(
                $"[Rendimiento] Auditoria completada:\n" +
                $"  - Sprite Atlas activos: {atlasGuids.Length} (SpritePackerMode = {EditorSettings.spritePackerMode})\n" +
                $"  - Texturas en Assets/_Project/Art: {texGuids.Length} ({noMipTex} sin MipMaps, {compressedTex} comprimidas)\n" +
                $"  - Audios en Assets/_Project/Audio: {audioGuids.Length} ({adpcmOrVorbis} comprimidos ADPCM/Vorbis, {streamingAudio} Streaming/CompressedInMemory)\n" +
                $"  - Luces en escena activa: {lights.Length} ({shadowLights} con sombras en tiempo real)");
        }
    }
}

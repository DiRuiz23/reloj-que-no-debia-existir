using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Reloj.Player;

namespace Reloj.TimeStop
{
    // Feedback de la Parada Temporal: animacion de lanzamiento del protagonista, onda expansiva,
    // vineta dorada en pantalla, tinte de los objetos congelados y, si la camara tiene
    // postprocesado, desaturacion del mundo. Todo se crea en codigo (sin assets extra).
    public class TimeStopFeedback : MonoBehaviour
    {
        [Header("Jugador")]
        [SerializeField] PlayerMotor player;
        [SerializeField] Animator playerAnimator;
        [Tooltip("Segundos que el jugador queda quieto lanzando la Parada (igual a la animacion).")]
        [SerializeField, Min(0f)] float castLockSeconds = 0.42f;

        [Header("Color (acento reservado a lo fuera de tiempo)")]
        [SerializeField] Color accent = new Color(1f, 0.651f, 0.169f, 1f);
        [SerializeField, Range(0f, 1f)] float frozenTint = 0.45f;

        [Header("Onda expansiva")]
        [SerializeField, Min(1f)] float waveRadius = 16f;
        [SerializeField, Min(0.1f)] float waveSeconds = 0.7f;

        [Header("Pantalla")]
        [SerializeField, Range(0f, 1f)] float overlayStrength = 0.5f;
        [SerializeField, Min(0.01f)] float fadeInSeconds = 0.2f;
        [SerializeField, Min(0.01f)] float fadeOutSeconds = 0.5f;
        [SerializeField, Range(-100f, 0f)] float worldSaturation = -60f;

        static readonly int TimeStopHash = Animator.StringToHash("TimeStop");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        struct TintTarget
        {
            public Renderer renderer;
            public int index;
            public int prop;
            public Color original;
        }

        readonly List<TintTarget> targets = new List<TintTarget>();
        TimeStopController controller;
        MaterialPropertyBlock block;

        float weight;
        float weightTarget;
        float waveTime = -1f;

        Transform waveTransform;
        SpriteRenderer waveRenderer;
        RawImage overlay;
        Volume volume;
        VolumeProfile profile;
        Texture2D ringTex;
        Texture2D vignetteTex;
        Sprite ringSprite;

        void Start()
        {
            block = new MaterialPropertyBlock();
            if (player == null) player = FindFirstObjectByType<PlayerMotor>();
            if (playerAnimator == null && player != null) playerAnimator = player.GetComponentInChildren<Animator>();

            controller = TimeStopController.Instance;
            if (controller != null)
            {
                controller.OnTimeStopStart += HandleStart;
                controller.OnTimeStopEnd += HandleEnd;
            }
            else Debug.LogWarning("[TimeStopFeedback] No hay TimeStopController en la escena.", this);

            BuildOverlay();
            BuildWave();
            BuildVolume();
        }

        void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnTimeStopStart -= HandleStart;
                controller.OnTimeStopEnd -= HandleEnd;
            }
            ClearTint();
            if (profile != null) Destroy(profile);
            if (ringTex != null) Destroy(ringTex);
            if (vignetteTex != null) Destroy(vignetteTex);
            if (ringSprite != null) Destroy(ringSprite);
        }

        void HandleStart()
        {
            weightTarget = 1f;
            waveTime = 0f;
            CollectTargets();

            if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
                playerAnimator.SetTrigger(TimeStopHash);
            if (player != null) player.LockFor(castLockSeconds);

            if (waveTransform != null && player != null)
            {
                waveTransform.position = player.transform.position + Vector3.up * 0.08f;
                waveRenderer.enabled = true;
            }
        }

        void HandleEnd()
        {
            weightTarget = 0f;
        }

        void Update()
        {
            float rate = weightTarget > weight ? 1f / fadeInSeconds : 1f / fadeOutSeconds;
            float before = weight;
            weight = Mathf.MoveTowards(weight, weightTarget, rate * Time.deltaTime);

            if (!Mathf.Approximately(before, weight))
            {
                if (overlay != null)
                {
                    Color c = accent;
                    c.a = overlayStrength * weight;
                    overlay.color = c;
                    overlay.enabled = weight > 0.001f;
                }
                if (volume != null) volume.weight = weight;
                ApplyTint(weight);
                if (weight <= 0f) ClearTint();
            }

            UpdateWave();
        }

        void UpdateWave()
        {
            if (waveTime < 0f || waveRenderer == null) return;
            waveTime += Time.deltaTime;
            float k = waveTime / waveSeconds;
            if (k >= 1f)
            {
                waveTime = -1f;
                waveRenderer.enabled = false;
                return;
            }
            float ease = 1f - (1f - k) * (1f - k);
            float diameter = Mathf.Lerp(0.5f, waveRadius * 2f, ease);
            waveTransform.localScale = new Vector3(diameter, diameter, 1f);
            Color c = accent;
            c.a = (1f - k) * 0.9f;
            waveRenderer.color = c;
        }

        void CollectTargets()
        {
            ClearTint();
            targets.Clear();
            foreach (var a in FindObjectsByType<TimeAffectableBase>(FindObjectsSortMode.None))
            {
                foreach (var r in a.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer) continue;
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null) continue;
                        int p = m.HasProperty(BaseColorId) ? BaseColorId : (m.HasProperty(ColorId) ? ColorId : -1);
                        if (p < 0) continue;
                        targets.Add(new TintTarget { renderer = r, index = i, prop = p, original = m.GetColor(p) });
                    }
                }
            }
        }

        void ApplyTint(float t)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var tg = targets[i];
                if (tg.renderer == null) continue;
                tg.renderer.GetPropertyBlock(block, tg.index);
                block.SetColor(tg.prop, Color.Lerp(tg.original, accent, frozenTint * t));
                tg.renderer.SetPropertyBlock(block, tg.index);
            }
        }

        void ClearTint()
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].renderer != null) targets[i].renderer.SetPropertyBlock(null, targets[i].index);
            }
            targets.Clear();
        }

        void BuildOverlay()
        {
            var go = new GameObject("TimeStopOverlay", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -10;   // debajo del HUD y de los dialogos

            const int n = 128;
            vignetteTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            vignetteTex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 0.15f + 0.85f * Mathf.SmoothStep(0.3f, 1.15f, d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            vignetteTex.SetPixels32(px);
            vignetteTex.Apply();

            var imgGo = new GameObject("Vineta", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            overlay = imgGo.GetComponent<RawImage>();
            overlay.texture = vignetteTex;
            overlay.raycastTarget = false;
            overlay.color = new Color(accent.r, accent.g, accent.b, 0f);
            overlay.enabled = false;
        }

        void BuildWave()
        {
            const int n = 256;
            ringTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            ringTex.wrapMode = TextureWrapMode.Clamp;
            ringTex.filterMode = FilterMode.Bilinear;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Exp(-Mathf.Pow((d - 0.92f) / 0.045f, 2f));
                    float fill = d < 0.92f ? 0.10f * (1f - d) : 0f;
                    float a = Mathf.Clamp01(ring + fill);
                    if (d > 1f) a = 0f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            ringTex.SetPixels32(px);
            ringTex.Apply();
            ringSprite = Sprite.Create(ringTex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);

            var go = new GameObject("TimeStopWave");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            waveTransform = go.transform;
            waveRenderer = go.AddComponent<SpriteRenderer>();
            waveRenderer.sprite = ringSprite;
            waveRenderer.sortingOrder = 50;
            waveRenderer.enabled = false;
        }

        void BuildVolume()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(worldSaturation);
            ca.contrast.Override(10f);
            var vg = profile.Add<Vignette>(true);
            vg.color.Override(accent);
            vg.intensity.Override(0.3f);
            vg.smoothness.Override(0.6f);

            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 0f;
            volume.sharedProfile = profile;
        }
    }
}

using UnityEngine;

namespace Reloj.Stealth
{
    /// <summary>
    /// Senal visible del ataque del Guardian (I-32): durante la Anticipacion dibuja en el suelo la zona de peligro
    /// (aro exterior fijo + disco interior que crece de 0 a 1). Cuando el disco llega al aro, cae el golpe.
    /// Si la Parada Temporal congela al Guardian, el disco se detiene y toma un tono azul. Tambien tine el
    /// sprite del Guardian (rojo parpadeante al anunciar, apagado al recuperarse).
    /// Lee todo de GuardianAI (mismo patron que EnemyVisionCone).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GuardianAI))]
    public class GuardianTelegraph : MonoBehaviour
    {
        [Header("Zona de peligro")]
        [SerializeField] Color dangerColor = new Color(1f, 0.18f, 0.08f, 1f);
        [SerializeField] Color frozenColor = new Color(0.65f, 0.78f, 1f, 1f);
        [SerializeField] Color strikeColor = new Color(1f, 0.95f, 0.85f, 1f);
        [SerializeField, Range(0f, 1f)] float fillAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] float ringAlpha = 0.9f;
        [Tooltip("Grosor del aro exterior, en metros.")]
        [SerializeField, Range(0.1f, 2f)] float ringThickness = 0.4f;
        [Tooltip("Altura sobre el piso (evita parpadeo con el suelo y con el cono de vision).")]
        [SerializeField, Range(0.02f, 0.5f)] float groundOffset = 0.15f;
        [SerializeField, Range(24, 128)] int segments = 72;

        [Header("Cuerpo del Guardian")]
        [Tooltip("Sprite a teñir (si esta vacio se busca en los hijos).")]
        [SerializeField] SpriteRenderer body;
        [SerializeField] Color recoveryTint = new Color(0.55f, 0.6f, 0.72f, 1f);

        GuardianAI ai;
        Transform root;
        MeshRenderer ringRenderer, fillRenderer;
        MeshFilter ringFilter;
        Material ringMat, fillMat;
        Mesh ringMesh, fillMesh;
        float builtRadius = -1f;
        Color bodyBase = Color.white;

        void Awake()
        {
            ai = GetComponent<GuardianAI>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body != null) bodyBase = body.color;

            root = new GameObject("GuardianTelegraph").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, groundOffset, 0f);

            fillMesh = BuildDisc(segments);
            ringMat = NewMaterial(3001);
            fillMat = NewMaterial(3000);

            var fillGo = new GameObject("ZoneFill");
            fillGo.transform.SetParent(root, false);
            fillGo.AddComponent<MeshFilter>().sharedMesh = fillMesh;
            fillRenderer = fillGo.AddComponent<MeshRenderer>();
            Setup(fillRenderer, fillMat);

            var ringGo = new GameObject("ZoneRing");
            ringGo.transform.SetParent(root, false);
            ringFilter = ringGo.AddComponent<MeshFilter>();
            ringRenderer = ringGo.AddComponent<MeshRenderer>();
            Setup(ringRenderer, ringMat);

            SetVisible(false);
        }

        void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
            if (fillMat != null) Destroy(fillMat);
            if (ringMesh != null) Destroy(ringMesh);
            if (fillMesh != null) Destroy(fillMesh);
        }

        void OnDisable()
        {
            if (body != null) body.color = bodyBase;
        }

        static Material NewMaterial(int queue)
        {
            return new Material(Shader.Find("Sprites/Default")) { renderQueue = queue };
        }

        static void Setup(MeshRenderer r, Material m)
        {
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void SetVisible(bool on)
        {
            if (ringRenderer.enabled != on) ringRenderer.enabled = on;
            if (fillRenderer.enabled != on) fillRenderer.enabled = on;
        }

        void LateUpdate()
        {
            GuardianPhase phase = ai.Phase;
            float p = ai.PhaseProgress;

            // ---- Zona en el suelo ----
            if (phase == GuardianPhase.Anticipacion || phase == GuardianPhase.Ataque)
            {
                RebuildRingIfNeeded(ai.AttackRadius);
                SetVisible(true);

                if (phase == GuardianPhase.Anticipacion)
                {
                    bool frozen = ai.IsFrozen;
                    Color c = frozen ? frozenColor : dangerColor;
                    // El parpadeo se acelera al acercarse el golpe; congelado, la zona queda fija.
                    float pulse = frozen ? 0.85f : 0.7f + 0.3f * Mathf.Sin(Time.time * Mathf.Lerp(5f, 18f, p));
                    SetFill(c, fillAlpha, Mathf.Max(0.001f, p));
                    SetRing(c, ringAlpha * pulse);
                }
                else
                {
                    float fade = 1f - p;
                    SetFill(strikeColor, 0.7f * fade, 1f);
                    SetRing(strikeColor, fade);
                }
            }
            else
            {
                SetVisible(false);
            }

            // ---- Cuerpo ----
            if (body != null)
            {
                if (phase == GuardianPhase.Anticipacion && !ai.IsFrozen)
                    body.color = Color.Lerp(bodyBase, dangerColor, 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(6f, 20f, p)));
                else if (phase == GuardianPhase.Anticipacion)
                    body.color = Color.Lerp(bodyBase, frozenColor, 0.5f);
                else if (phase == GuardianPhase.Recuperacion)
                    body.color = recoveryTint;
                else
                    body.color = bodyBase;
            }
        }

        void SetFill(Color c, float alpha, float scale01)
        {
            c.a = alpha;
            fillMat.color = c;
            float r = Mathf.Max(0.001f, ai.AttackRadius * scale01);
            fillRenderer.transform.localScale = new Vector3(r, 1f, r);
        }

        void SetRing(Color c, float alpha)
        {
            c.a = alpha;
            ringMat.color = c;
        }

        void RebuildRingIfNeeded(float radius)
        {
            if (Mathf.Abs(radius - builtRadius) < 0.01f) return;
            builtRadius = radius;
            if (ringMesh != null) Destroy(ringMesh);
            ringMesh = BuildRing(segments, radius, Mathf.Min(ringThickness, radius * 0.5f));
            ringFilter.sharedMesh = ringMesh;
        }

        // ---------- Mallas ----------
        /// <summary>Disco de radio 1 sobre el plano XZ.</summary>
        static Mesh BuildDisc(int seg)
        {
            var verts = new Vector3[seg + 1];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                verts[i + 1] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % seg + 1;
            }
            var m = new Mesh { name = "GuardianZoneFill" };
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Aro de radio exterior 'outer' y grosor 'thickness' (en metros) sobre el plano XZ.</summary>
        static Mesh BuildRing(int seg, float outer, float thickness)
        {
            float inner = Mathf.Max(0f, outer - thickness);
            var verts = new Vector3[seg * 2];
            var tris = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts[i * 2] = d * outer;
                verts[i * 2 + 1] = d * inner;

                int n = (i + 1) % seg;
                int o0 = i * 2, i0 = i * 2 + 1, o1 = n * 2, i1 = n * 2 + 1;
                int t = i * 6;
                tris[t] = o0; tris[t + 1] = o1; tris[t + 2] = i0;
                tris[t + 3] = i0; tris[t + 4] = o1; tris[t + 5] = i1;
            }
            var m = new Mesh { name = "GuardianZoneRing" };
            m.vertices = verts;
            m.triangles = tris;
            m.RecalculateBounds();
            return m;
        }
    }
}

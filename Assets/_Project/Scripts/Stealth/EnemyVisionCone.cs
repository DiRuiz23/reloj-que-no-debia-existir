using UnityEngine;
using Reloj.Player;

namespace Reloj.Stealth
{
    /// <summary>
    /// Dibuja en el piso el cono de vision del enemigo (luz proyectada). Usa el mismo angulo, distancia y mascara
    /// que EnemyAI, y las paredes lo recortan. Cambia de color segun el estado (Ignorante / Sospechoso / Alerta / Congelado).
    /// Los colores se editan en el prefab Enemy_Base (afectan a todos los enemigos).
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyVisionCone : MonoBehaviour
    {
        [Header("Colores por estado (el alfa controla cuanto se nota)")]
        [SerializeField] Color ignorante = new Color(0.45f, 0.85f, 1f, 0.38f);
        [SerializeField] Color sospechoso = new Color(1f, 0.6f, 0.1f, 0.50f);
        [SerializeField] Color alerta = new Color(1f, 0.1f, 0.1f, 0.60f);
        [SerializeField] Color congelado = new Color(0.55f, 0.6f, 0.9f, 0.28f);
        [Header("Forma")]
        [SerializeField, Range(8, 64)] int rays = 36;
        [Tooltip("Altura sobre el piso donde se dibuja (evita parpadeo con el suelo).")]
        [SerializeField, Min(0.02f)] float groundOffset = 0.12f;
        [Tooltip("Opacidad en la punta del cono respecto al origen (0 = se desvanece).")]
        [SerializeField, Range(0f, 1f)] float farAlpha = 0.35f;
        [SerializeField] bool visible = true;

        EnemyAI ai; Mesh mesh; MeshFilter mf; MeshRenderer mr; Material mat;
        Vector3[] verts; Color[] cols; int[] tris;
        Color current;
        readonly RaycastHit[] hits = new RaycastHit[8];

        void Awake()
        {
            ai = GetComponent<EnemyAI>();
            var go = new GameObject("VisionCone");
            go.transform.SetParent(transform, false);
            mf = go.AddComponent<MeshFilter>();
            mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mat = new Material(Shader.Find("Sprites/Default"));
            mat.renderQueue = 3000;
            mr.sharedMaterial = mat;
            mesh = new Mesh { name = "VisionCone" };
            mesh.MarkDynamic();
            mf.sharedMesh = mesh;
            current = ignorante;
            Build();
        }

        void OnDestroy() { if (mat != null) Destroy(mat); if (mesh != null) Destroy(mesh); }

        void Build()
        {
            int n = rays + 1;
            verts = new Vector3[n + 1];
            cols = new Color[n + 1];
            tris = new int[rays * 3];
            for (int i = 0; i < rays; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
        }

        Color TargetColor()
        {
            if (ai.IsFrozen) return congelado;
            switch (ai.State)
            {
                case EnemyState.Sospechoso: return sospechoso;
                case EnemyState.Alerta: return alerta;
                default: return ignorante;
            }
        }

        void LateUpdate()
        {
            mr.enabled = visible;
            if (!visible) return;
            float dist = ai.ViewDistance, half = ai.ViewAngle * 0.5f;
            current = Color.Lerp(current, TargetColor(), 1f - Mathf.Exp(-12f * Time.deltaTime));
            Vector3 origin = transform.position;
            Vector3 eye = origin + Vector3.up * ai.EyeHeight;
            Color near = current, far = current; far.a *= farAlpha;

            verts[0] = transform.InverseTransformPoint(new Vector3(origin.x, groundOffset, origin.z)); cols[0] = near;
            for (int i = 0; i <= rays; i++)
            {
                float ang = Mathf.Lerp(-half, half, i / (float)rays);
                Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * transform.forward; dir.y = 0f; dir.Normalize();
                float d = dist;
                int c = Physics.RaycastNonAlloc(eye, dir, hits, dist, ai.VisionMask, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < c; h++)
                {
                    var col = hits[h].collider;
                    if (col.transform.IsChildOf(transform)) continue;
                    if (col.GetComponentInParent<PlayerMotor>() != null) continue;
                    if (col.GetComponentInParent<EnemyAI>() != null) continue;
                    if (hits[h].distance < d) d = hits[h].distance;
                }
                Vector3 w = origin + dir * d; w.y = groundOffset;
                verts[i + 1] = transform.InverseTransformPoint(w);
                cols[i + 1] = Color.Lerp(near, far, d / dist);
            }
            mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
            mesh.RecalculateBounds();
        }
    }
}

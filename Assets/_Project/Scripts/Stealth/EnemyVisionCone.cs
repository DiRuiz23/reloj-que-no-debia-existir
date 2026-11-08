using UnityEngine;
using Reloj.Player;

namespace Reloj.Stealth
{
    /// <summary>
    /// Cono de vision del enemigo dibujado como luz de linterna en el piso: calida, intensa cerca del soldado
    /// y desvaneciendose hacia el extremo ancho. Usa el mismo angulo, distancia y mascara que EnemyAI (las paredes lo recortan).
    /// El color cambia por estado. Se edita en el prefab Enemy_Base (afecta a todos los enemigos).
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyVisionCone : MonoBehaviour
    {
        [Header("Color de la luz por estado (alfa = intensidad cerca del soldado)")]
        [SerializeField] Color ignorante = new Color(1f, 0.86f, 0.50f, 0.55f);
        [SerializeField] Color sospechoso = new Color(1f, 0.62f, 0.20f, 0.65f);
        [SerializeField] Color alerta = new Color(1f, 0.25f, 0.12f, 0.70f);
        [SerializeField] Color congelado = new Color(0.70f, 0.78f, 1f, 0.35f);
        [Header("Forma de la linterna")]
        [SerializeField, Range(8, 64)] int rays = 40;
        [SerializeField, Range(3, 16)] int rows = 10;
        [Tooltip("Altura sobre el piso (evita parpadeo con el suelo).")]
        [SerializeField, Min(0.02f)] float groundOffset = 0.12f;
        [Tooltip("Mayor valor = la luz se apaga mas rapido hacia el extremo ancho.")]
        [SerializeField, Range(0.5f, 3f)] float fadePower = 1.5f;
        [Tooltip("Suavizado de los bordes laterales (0 = bordes duros).")]
        [SerializeField, Range(0f, 0.9f)] float edgeSoftness = 0.45f;
        [SerializeField] bool visible = true;

        EnemyAI ai; Mesh mesh; MeshRenderer mr; Material mat;
        Vector3[] verts; Color[] cols; int[] tris; float[] colDist;
        Color current;
        readonly RaycastHit[] hits = new RaycastHit[8];

        void Awake()
        {
            ai = GetComponent<EnemyAI>();
            var go = new GameObject("VisionCone");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mat = new Material(Shader.Find("Sprites/Default")) { renderQueue = 3000 };
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
            int c = rays + 1, r = rows + 1;
            verts = new Vector3[c * r]; cols = new Color[c * r]; colDist = new float[c];
            tris = new int[rays * rows * 6];
            int t = 0;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < rays; i++)
                {
                    int a = j * c + i, b = a + 1, d = a + c, e = d + 1;
                    tris[t++] = a; tris[t++] = d; tris[t++] = b;
                    tris[t++] = b; tris[t++] = d; tris[t++] = e;
                }
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
            int c = rays + 1;

            for (int i = 0; i < c; i++)
            {
                float u = i / (float)rays;                       // 0..1 a lo ancho
                float ang = Mathf.Lerp(-half, half, u);
                Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * transform.forward; dir.y = 0f; dir.Normalize();
                float d = dist;
                int n = Physics.RaycastNonAlloc(eye, dir, hits, dist, ai.VisionMask, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < n; h++)
                {
                    var col = hits[h].collider;
                    if (col.transform.IsChildOf(transform)) continue;
                    if (col.GetComponentInParent<PlayerMotor>() != null) continue;
                    if (col.GetComponentInParent<EnemyAI>() != null) continue;
                    if (hits[h].distance < d) d = hits[h].distance;
                }
                colDist[i] = d;
                float side = Mathf.Abs(u * 2f - 1f);                                   // 0 centro, 1 borde
                float edge = 1f - edgeSoftness * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, side));
                for (int j = 0; j <= rows; j++)
                {
                    float f = j / (float)rows;
                    float dj = d * f;
                    Vector3 w = origin + dir * dj; w.y = groundOffset;
                    int idx = j * c + i;
                    verts[idx] = transform.InverseTransformPoint(w);
                    Color col2 = current;
                    float fade = Mathf.Pow(Mathf.Clamp01(1f - dj / dist), fadePower);
                    col2.a = current.a * fade * edge;
                    cols[idx] = col2;
                }
            }
            mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
            mesh.RecalculateBounds();
        }
    }
}

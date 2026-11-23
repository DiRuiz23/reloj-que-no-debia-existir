using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Reloj.Core;
using Reloj.Player;
using Reloj.Stealth;

namespace Reloj.Level
{
    /// <summary>
    /// Persecucion del Guardian (Nivel 4). Se coloca en la instancia del Guardian y NO modifica GuardianAI ni EnemyAI.
    ///
    /// Al detectar al jugador (Alerta del propio Guardian) o cuando un soldado da la alarma (alerta compartida), el Guardian
    /// deja su patrulla y SIGUE al jugador esquivando obstaculos (A* sobre una rejilla de la arena). Si lo alcanza llama a
    /// PlayerRespawn.Kill (ultimo checkpoint, -34 de vida, Game Over solo si la vida llega a 0). Si pierde la linea de vision
    /// durante 'loseSightSeconds', vuelve a patrullar. La Parada Temporal lo congela (EnemyAI.IsFrozen).
    /// Mientras persigue, GuardianAI se desactiva (no hay onda de choque); al terminar se reactiva y se resetea a Patrulla.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAI))]
    public class GuardianChase : MonoBehaviour
    {
        [Header("Persecucion")]
        [Tooltip("Debe ser menor que la velocidad del jugador (5) para que se pueda huir.")]
        [SerializeField, Min(0.5f)] float chaseSpeed = 3.2f;
        [Tooltip("Segundos sin linea de vision antes de rendirse y volver a patrullar.")]
        [SerializeField, Min(0.5f)] float loseSightSeconds = 3f;
        [Tooltip("Distancia maxima a la que sigue 'viendo' al jugador mientras persigue.")]
        [SerializeField, Min(1f)] float chaseSightDistance = 14f;
        [SerializeField, Min(0.3f)] float catchDistance = 1.1f;
        [SerializeField, Min(0.1f)] float repathSeconds = 0.3f;
        [Tooltip("Si la persecucion nace de una alerta de soldado y el Guardian aun no ha visto al jugador, se rinde tras estos segundos (anti soft-lock).")]
        [SerializeField, Min(1f)] float unseenAlertChaseSeconds = 8f;

        [Header("Alerta compartida con soldados")]
        [SerializeField] bool shareSoldierAlert = true;
        [Tooltip("Para soldados dentro de la arena basta con que sospechen (no hace falta esperar a la Alerta).")]
        [SerializeField] bool alertOnSuspicion = true;
        [SerializeField] float arenaMinX = 50f;

        [Header("Rejilla de rutas (limites de la arena)")]
        [SerializeField] Vector2 gridMin = new Vector2(48f, -11.6f);
        [SerializeField] Vector2 gridMax = new Vector2(95f, 15.9f);
        [SerializeField, Min(0.25f)] float cell = 0.5f;
        [SerializeField, Min(0.3f)] float agentRadius = 0.6f;

        EnemyAI enemy;
        GuardianAI guardian;
        Rigidbody body;
        PlayerMotor player;
        PlayerRespawn respawn;
        MethodInfo resetToPatrol;

        bool chasing, acquired;
        float lostTimer, repathTimer, unseenTimer;
        readonly List<Vector3> path = new List<Vector3>();
        int pathIdx;

        bool[,] free;
        int gw, gh;
        readonly Collider[] buf = new Collider[16];
        readonly RaycastHit[] hits = new RaycastHit[24];

        public bool IsChasing => chasing;

        void Awake()
        {
            enemy = GetComponent<EnemyAI>();
            guardian = GetComponent<GuardianAI>();
            body = GetComponent<Rigidbody>();
            resetToPatrol = typeof(GuardianAI).GetMethod("ResetToPatrol", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        void OnEnable()
        {
            EnemyAI.OnEnemyStateChanged += HandleState;
            if (enemy != null) enemy.PlayerRespawned += HandlePlayerRespawned;
        }

        void OnDisable()
        {
            EnemyAI.OnEnemyStateChanged -= HandleState;
            if (enemy != null) enemy.PlayerRespawned -= HandlePlayerRespawned;
            if (chasing) EndChase();
        }

        // ---------------------------------------------------------------- disparadores
        void HandleState(EnemyAI who, EnemyState prev, EnemyState next)
        {
            if (chasing || !isActiveAndEnabled) return;

            if (who == enemy)
            {
                if (next == EnemyState.Alerta) StartChase();     // el propio Guardian te vio por completo
                return;
            }
            if (!shareSoldierAlert || who.GetComponent<GuardianAI>() != null) return;

            var enc = ArenaEncounter.Current;                    // solo mientras el jugador esta en la arena
            if (enc == null || !enc.IsActive || enc.IsCompleted) return;

            bool inArena = who.transform.position.x >= arenaMinX;
            if (next == EnemyState.Alerta || (alertOnSuspicion && inArena && next == EnemyState.Sospechoso)) StartChase();
        }

        void HandlePlayerRespawned()
        {
            if (chasing) EndChase();
        }

        // ---------------------------------------------------------------- ciclo
        void StartChase()
        {
            if (!ResolvePlayer()) return;
            chasing = true;
            lostTimer = 0f; repathTimer = 0f; unseenTimer = 0f; acquired = false; path.Clear(); pathIdx = 0;
            var cfg = LevelDifficultyConfig.Current;            // dificultad del nivel (campos nuevos del Guardian)
            if (cfg != null) { chaseSpeed = cfg.GuardianChaseSpeed; loseSightSeconds = cfg.GuardianLoseSightSeconds; }
            if (guardian != null) guardian.enabled = false;     // sin onda de choque mientras persigue
            enemy.Suspended = true;                             // EnemyAI deja de patrullar: lo movemos nosotros
            enemy.VisionHidden = false;
            BuildGrid();
            enemy.ForceState(EnemyState.Alerta);                // icono rojo (chasing evita reentrada)
        }

        void EndChase()
        {
            chasing = false;
            path.Clear();
            if (guardian != null)
            {
                guardian.enabled = true;
                if (resetToPatrol != null) resetToPatrol.Invoke(guardian, null);
            }
            enemy.Suspended = false;
            enemy.ForceState(EnemyState.Ignorante);
            enemy.BlindFor(1.5f);
        }

        void FixedUpdate()
        {
            if (!chasing || enemy.IsFrozen || !IsPlaying()) return;   // Parada Temporal: se congela
            if (player == null) { EndChase(); return; }

            float dt = Time.fixedDeltaTime;
            Vector3 pos = body != null ? body.position : transform.position;
            Vector3 target = player.transform.position;

            if (Flat(target - pos).magnitude <= catchDistance) { Catch(); return; }

            bool sees = Flat(target - pos).magnitude <= chaseSightDistance && LineOfSight(pos, target);
            if (sees) { acquired = true; lostTimer = 0f; unseenTimer = 0f; }
            else if (acquired) { lostTimer += dt; if (lostTimer >= loseSightSeconds) { EndChase(); return; } }
            else { unseenTimer += dt; if (unseenTimer >= unseenAlertChaseSeconds) { EndChase(); return; } }   // alerta de soldado: persigue sin ver hasta encontrarlo

            repathTimer -= dt;
            if (repathTimer <= 0f) { repathTimer = repathSeconds; Plan(pos, target); }

            Vector3 goal = NextGoal(pos, target);
            Vector3 dir = Flat(goal - pos);
            if (dir.sqrMagnitude > 0.0001f)
            {
                if (path.Count == 0 && !SegmentClear(pos, pos + dir.normalized * 0.6f)) { enemy.TurnTowards(goal, dt); return; }   // sin ruta: no atravesar obstaculos
                Vector3 next = pos + dir.normalized * chaseSpeed * dt;
                if (body != null) body.MovePosition(next); else transform.position = next;
                enemy.TurnTowards(goal, dt);
            }
        }

        void Catch()
        {
            if (respawn != null) respawn.Kill(true);    // como EnemyAI: ultimo checkpoint, -34 de vida
            EndChase();
        }

        // ---------------------------------------------------------------- rutas
        Vector3 NextGoal(Vector3 pos, Vector3 target)
        {
            if (path.Count == 0) return target;
            while (pathIdx < path.Count && Flat(path[pathIdx] - pos).magnitude < 0.4f) pathIdx++;
            if (pathIdx >= path.Count) return target;
            for (int k = Mathf.Min(pathIdx + 3, path.Count - 1); k > pathIdx; k--)   // atajo si hay paso libre
                if (SegmentClear(pos, path[k])) { pathIdx = k; break; }
            return path[pathIdx];
        }

        bool SegmentClear(Vector3 a, Vector3 b)
        {
            Vector3 d = Flat(b - a); float len = d.magnitude; if (len < 0.01f) return true;
            float r = agentRadius * 0.9f;
            int n = Physics.CapsuleCastNonAlloc(a + Vector3.up * (r + 0.12f), a + Vector3.up * 1.7f, r, d / len, hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (IsObstacle(hits[i].collider)) return false;
            return true;
        }

        bool LineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 a = from + Vector3.up * 1.2f, b = to + Vector3.up * 1.0f; Vector3 d = b - a;
            int n = Physics.RaycastNonAlloc(a, d.normalized, hits, d.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (IsObstacle(hits[i].collider)) return false;
            return true;
        }

        static bool IsObstacle(Collider c) =>
            !c.isTrigger && c.GetComponentInParent<EnemyAI>() == null && c.GetComponentInParent<PlayerMotor>() == null;

        void BuildGrid()
        {
            gw = Mathf.CeilToInt((gridMax.x - gridMin.x) / cell);
            gh = Mathf.CeilToInt((gridMax.y - gridMin.y) / cell);
            free = new bool[gw, gh];
            for (int i = 0; i < gw; i++)
                for (int j = 0; j < gh; j++)
                {
                    Vector3 p = CellCenter(i, j);
                    int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * (agentRadius + 0.12f), p + Vector3.up * 1.7f, agentRadius, buf, ~0, QueryTriggerInteraction.Ignore);
                    bool ok = true;
                    for (int k = 0; k < n && ok; k++) if (IsObstacle(buf[k])) ok = false;
                    free[i, j] = ok;
                }
        }

        Vector3 CellCenter(int i, int j) => new Vector3(gridMin.x + (i + 0.5f) * cell, 0f, gridMin.y + (j + 0.5f) * cell);
        Vector2Int ToCell(Vector3 p) => new Vector2Int(Mathf.Clamp(Mathf.FloorToInt((p.x - gridMin.x) / cell), 0, gw - 1),
                                                        Mathf.Clamp(Mathf.FloorToInt((p.z - gridMin.y) / cell), 0, gh - 1));

        bool NearestFree(Vector2Int c, out Vector2Int r)
        {
            for (int rad = 0; rad <= 8; rad++)
                for (int dx = -rad; dx <= rad; dx++)
                    for (int dz = -rad; dz <= rad; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != rad) continue;
                        int x = c.x + dx, z = c.y + dz;
                        if (x < 0 || z < 0 || x >= gw || z >= gh || !free[x, z]) continue;
                        r = new Vector2Int(x, z); return true;
                    }
            r = c; return false;
        }

        /// <summary>A* (8 vecinos) sobre la rejilla. Devuelve false si no hay ruta.</summary>
        bool Plan(Vector3 from, Vector3 to)
        {
            path.Clear(); pathIdx = 0;
            if (free == null) return false;
            if (!NearestFree(ToCell(from), out var s) || !NearestFree(ToCell(to), out var g)) return false;

            int W = gw, H = gh, N = W * H;
            var gCost = new float[N]; var from_ = new int[N]; var closed = new bool[N];
            for (int i = 0; i < N; i++) { gCost[i] = float.MaxValue; from_[i] = -1; }
            var heap = new List<(float f, int id)>();
            Action<float, int> push = (f, id) => { heap.Add((f, id)); int c = heap.Count - 1; while (c > 0) { int p = (c - 1) / 2; if (heap[p].f <= heap[c].f) break; var t = heap[p]; heap[p] = heap[c]; heap[c] = t; c = p; } };
            Func<(float f, int id)> pop = () => { var top = heap[0]; var last = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1); if (heap.Count > 0) { heap[0] = last; int c = 0; while (true) { int l = 2 * c + 1, r = l + 1, m = c; if (l < heap.Count && heap[l].f < heap[m].f) m = l; if (r < heap.Count && heap[r].f < heap[m].f) m = r; if (m == c) break; var t = heap[m]; heap[m] = heap[c]; heap[c] = t; c = m; } } return top; };

            int sid = s.x + s.y * W, gid = g.x + g.y * W;
            gCost[sid] = 0f; push(Heur(s, g), sid);
            while (heap.Count > 0)
            {
                int cur = pop().id; if (closed[cur]) continue; closed[cur] = true;
                if (cur == gid) break;
                int cx = cur % W, cz = cur / W;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (nx < 0 || nz < 0 || nx >= W || nz >= H || !free[nx, nz]) continue;
                        if (dx != 0 && dz != 0 && (!free[cx + dx, cz] || !free[cx, cz + dz])) continue;   // sin cortar esquinas
                        int nid = nx + nz * W; if (closed[nid]) continue;
                        float ng = gCost[cur] + ((dx != 0 && dz != 0) ? 1.4142f : 1f);
                        if (ng < gCost[nid]) { gCost[nid] = ng; from_[nid] = cur; push(ng + Heur(new Vector2Int(nx, nz), g), nid); }
                    }
            }
            if (from_[gid] < 0 && sid != gid) return false;

            var rev = new List<Vector3>(); for (int c = gid; c >= 0; c = from_[c]) { rev.Add(CellCenter(c % W, c / W)); if (c == sid) break; }
            rev.Reverse(); path.AddRange(rev);
            return path.Count > 0;
        }

        static float Heur(Vector2Int a, Vector2Int b) { float dx = Mathf.Abs(a.x - b.x), dz = Mathf.Abs(a.y - b.y); return (dx + dz) + (1.4142f - 2f) * Mathf.Min(dx, dz); }

        /// <summary>Prueba de rutas desde el editor: construye la rejilla y devuelve cuantos nodos tiene la ruta (0 = sin ruta).</summary>
        public int DebugPlan(Vector3 from, Vector3 to)
        {
            BuildGrid();
            return Plan(from, to) ? path.Count : 0;
        }

        // ---------------------------------------------------------------- utilidades
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        bool ResolvePlayer()
        {
            if (player != null) return true;
            player = FindFirstObjectByType<PlayerMotor>();
            if (player == null) return false;
            respawn = player.GetComponent<PlayerRespawn>();
            return true;
        }

        static bool IsPlaying()
        {
            var s = GameServices.Instance;
            return s == null || s.State == null || s.State.CurrentState == GameState.Jugando;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
            for (int i = 0; i + 1 < path.Count; i++) Gizmos.DrawLine(path[i] + Vector3.up * 0.2f, path[i + 1] + Vector3.up * 0.2f);
        }
    }
}

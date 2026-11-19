using System;
using UnityEngine;

namespace Reloj.Stealth
{
    /// <summary>
    /// Sprites animados del Guardian a partir del estado de EnemyAI/GuardianAI:
    ///  - Reposo (primer frame de caminar), Caminar (patrulla) y Correr (si se mueve rapido).
    ///  - Lanzar hechizo en el ataque de onda: Anticipacion (preparacion + canalizacion con el reloj, mira al jugador),
    ///    Ataque (lanzamiento) y Recuperacion (frame de recuperacion).
    ///  - Hay 4 vistas (S, SE, N, NW); SW se obtiene volteando SE y NE volteando NW (volteo por escala, no flipX).
    /// La Parada Temporal congela la animacion (EnemyAI.IsFrozen). El aro rojo de dano lo sigue dibujando GuardianTelegraph.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuardianVisualController : MonoBehaviour
    {
        [Serializable]
        public class DirFrames { public Sprite[] s, se, n, nw; }

        [SerializeField] EnemyAI enemy;
        [SerializeField] GuardianAI guardian;
        [SerializeField] SpriteRenderer body;
        [Tooltip("Reloj magico que aparece al canalizar el hechizo (opcional).")]
        [SerializeField] SpriteRenderer clockSpell;

        [Header("Hojas (frames por direccion)")]
        [SerializeField] DirFrames walk = new DirFrames();
        [SerializeField] DirFrames run = new DirFrames();
        [SerializeField] DirFrames cast = new DirFrames();   // 0 preparacion, 1 canalizacion, 2 lanzamiento, 3 recuperacion
        [SerializeField] Sprite[] clockSpellFrames;

        [Header("Ritmo")]
        [SerializeField, Min(1f)] float walkFps = 8f;
        [SerializeField, Min(1f)] float runFps = 12f;
        [SerializeField, Min(0f)] float walkThreshold = 0.15f;
        [SerializeField, Min(0.5f)] float runThreshold = 3.4f;
        [Tooltip("Tras el golpe, segundos que se ve el frame de recuperacion antes de volver al reposo.")]
        [SerializeField, Min(0f)] float recoveryPoseSeconds = 1.2f;

        Vector3 baseScale = Vector3.one;
        Vector3 lastPos;
        Vector2 facing = Vector2.down;
        bool flip;
        float animTime;
        int lastClip = -1;
        Transform player;

        void Reset()
        {
            enemy = GetComponentInParent<EnemyAI>();
            guardian = GetComponentInParent<GuardianAI>();
            body = GetComponent<SpriteRenderer>();
        }

        void Awake()
        {
            if (enemy == null) enemy = GetComponentInParent<EnemyAI>();
            if (guardian == null) guardian = GetComponentInParent<GuardianAI>();
            if (body == null) body = GetComponent<SpriteRenderer>();
            Vector3 s = transform.localScale;
            baseScale = new Vector3(Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Abs(s.x), s.y, s.z);
            transform.localScale = baseScale;
            if (body != null) body.flipX = false;                 // el material ignora flipX
            if (enemy != null) lastPos = enemy.transform.position;
            if (clockSpell != null) clockSpell.enabled = false;
        }

        void Update()
        {
            if (enemy == null || body == null) return;
            float dt = Time.deltaTime;
            bool frozen = enemy.IsFrozen;
            GuardianPhase phase = guardian != null ? guardian.Phase : GuardianPhase.Patrulla;
            bool casting = phase != GuardianPhase.Patrulla;

            // ---- hacia donde mira: al jugador al lanzar el hechizo, si no hacia donde camina ----
            if (casting && ResolvePlayer())
            {
                Vector3 to = player.position - enemy.transform.position;
                var d = new Vector2(to.x, to.z); if (d.sqrMagnitude > 0.0001f) facing = d.normalized;
            }
            else
            {
                Vector3 f = enemy.transform.forward; var d = new Vector2(f.x, f.z);
                if (d.sqrMagnitude > 0.0001f) facing = d.normalized;
            }

            // ---- velocidad real (0 si la Parada Temporal lo congela) ----
            Vector3 pos = enemy.transform.position;
            Vector2 delta = new Vector2(pos.x - lastPos.x, pos.z - lastPos.z);
            lastPos = pos;
            float speed = frozen ? 0f : delta.magnitude / Mathf.Max(dt, 0.0001f);

            // ---- volteo: SE nativo mira a la derecha, NW nativo mira a la izquierda ----
            if (Mathf.Abs(facing.x) > 0.05f) flip = facing.y < 0f ? facing.x < 0f : facing.x > 0f;
            Vector3 sc = baseScale; sc.x = flip ? -baseScale.x : baseScale.x; transform.localScale = sc;

            // ---- vista segun la direccion ----
            int view = View(facing);

            // ---- clip actual ----
            Sprite[] frames; float fps; bool loop = true; int clipId;
            if (casting)
            {
                var c = Pick(cast, view);
                if (phase == GuardianPhase.Anticipacion)
                {
                    clipId = 10; fps = 0f;
                    float p = guardian.PhaseProgress;
                    frames = c; // preparacion (primer tramo) y canalizacion
                    SetFrame(frames, p < 0.3f ? 0 : 1);
                    DriveClock(p);
                    return;
                }
                if (phase == GuardianPhase.Ataque) { clipId = 11; SetFrame(c, 2); DriveClock(-1f); return; }
                // Recuperacion: frame de recuperacion un rato y luego reposo
                clipId = 12; DriveClock(-1f);
                float rp = guardian != null ? guardian.PhaseProgress * Mathf.Max(guardian.RecoverySeconds, 0.01f) : 0f;
                if (rp < recoveryPoseSeconds) { SetFrame(c, 3); return; }
                SetFrame(Pick(walk, view), 0); return;
            }

            DriveClock(-1f);
            if (speed > runThreshold) { clipId = 2; frames = Pick(run, view); fps = runFps; }
            else if (speed > walkThreshold) { clipId = 1; frames = Pick(walk, view); fps = walkFps; }
            else { clipId = 0; frames = Pick(walk, view); fps = 0f; }

            if (clipId != lastClip) { animTime = 0f; lastClip = clipId; }
            if (frames == null || frames.Length == 0) return;
            if (fps <= 0f) { body.sprite = frames[0]; return; }
            if (!frozen) animTime += dt;
            int idx = loop ? Mathf.FloorToInt(animTime * fps) % frames.Length : Mathf.Min(Mathf.FloorToInt(animTime * fps), frames.Length - 1);
            body.sprite = frames[idx];
        }

        void SetFrame(Sprite[] frames, int i)
        {
            if (frames == null || frames.Length == 0) return;
            body.sprite = frames[Mathf.Clamp(i, 0, frames.Length - 1)];
            lastClip = -1;
        }

        void DriveClock(float progress)
        {
            if (clockSpell == null) return;
            bool on = progress >= 0f && clockSpellFrames != null && clockSpellFrames.Length > 0;
            clockSpell.enabled = on;
            if (!on) return;
            int n = clockSpellFrames.Length;
            clockSpell.sprite = clockSpellFrames[Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(progress) * n), n - 1)];
        }

        // 0 = S, 1 = SE (SW volteado), 2 = N, 3 = NW (NE volteado)
        static int View(Vector2 f)
        {
            if (Mathf.Abs(f.x) < 0.38f) return f.y < 0f ? 0 : 2;
            return f.y <= 0f ? 1 : 3;
        }

        static Sprite[] Pick(DirFrames d, int view)
        {
            switch (view) { case 0: return d.s; case 1: return d.se; case 2: return d.n; default: return d.nw; }
        }

        bool ResolvePlayer()
        {
            if (player != null) return true;
            var m = FindFirstObjectByType<Reloj.Player.PlayerMotor>();
            if (m == null) return false;
            player = m.transform; return true;
        }
    }
}

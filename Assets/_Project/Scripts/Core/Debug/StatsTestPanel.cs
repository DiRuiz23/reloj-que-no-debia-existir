using TMPro;
using UnityEngine;
using Reloj.Player;

namespace Reloj.Core
{
    // Herramienta de prueba: dañar, curar y recoger objetos con botones, y ver los eventos.
    // Dibuja sus propios botones (IMGUI) en la esquina derecha: no necesita cablear UI.
    public class StatsTestPanel : MonoBehaviour
    {
        [Tooltip("Opcional. Si está vacío, el estado se dibuja con IMGUI.")]
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] int damageAmount = 25;
        [SerializeField] int healAmount = 20;
        [SerializeField] int pointsAmount = 100;
        [SerializeField] GameObject collectiblePrefab;
        [Tooltip("Opcional. Si está vacío, el coleccionable aparece sobre el jugador.")]
        [SerializeField] Transform spawnPoint;

        PlayerHealth health;
        ScoreManager score;
        LevelTimer timer;
        int healthEvents, scoreEvents, timeEvents;

        void Start()
        {
            health = PlayerHealth.Instance;
            score = ScoreManager.Instance;
            timer = LevelTimer.Instance;
            if (health != null) health.OnHealthChanged += OnHealth;
            if (score != null) score.OnScoreChanged += OnScore;
            if (timer != null) timer.OnTimeChanged += OnTime;
        }

        void OnDestroy()
        {
            if (health != null) health.OnHealthChanged -= OnHealth;
            if (score != null) score.OnScoreChanged -= OnScore;
            if (timer != null) timer.OnTimeChanged -= OnTime;
        }

        void OnHealth(int c, int m) => healthEvents++;
        void OnScore(int s) => scoreEvents++;
        void OnTime(float t) => timeEvents++;

        // ---- Acciones ----
        public void Damage() { if (health != null) health.TakeDamage(damageAmount); }
        public void Heal() { if (health != null) health.Heal(healAmount); }
        public void AddPoints() { if (score != null) score.Add(pointsAmount); }

        public void SpawnCollectible()
        {
            if (collectiblePrefab == null) return;
            Vector3 pos = spawnPoint != null ? spawnPoint.position
                        : health != null ? health.transform.position
                        : Vector3.zero;
            Instantiate(collectiblePrefab, pos, Quaternion.identity);   // prueba el trigger real
        }

        string BuildStatus()
        {
            var gs = GameServices.Instance;
            string estado = gs != null && gs.State != null ? gs.State.CurrentState.ToString() : "Sin GameServices";
            return
                $"Estado: {estado}\n" +
                $"Vida: {(health != null ? $"{health.Current}/{health.Max}" : "-")}\n" +
                $"Puntaje: {(score != null ? score.Score : 0)}\n" +
                $"Tiempo: {(timer != null ? LevelTimer.Format(timer.Elapsed) : "-")}\n" +
                $"Eventos -> vida: {healthEvents} | puntaje: {scoreEvents} | tiempo: {timeEvents}";
        }

        void Update()
        {
            if (statusLabel != null) statusLabel.text = BuildStatus();
        }

        void OnGUI()
        {
            const float w = 240f;
            float x = Screen.width - w - 10f;
            float y = 10f;

            if (statusLabel == null)
            {
                GUI.Box(new Rect(x, y, w, 100f), BuildStatus());
                y += 110f;
            }

            if (GUI.Button(new Rect(x, y, w, 30f), $"Dañar (-{damageAmount})")) Damage();
            y += 34f;
            if (GUI.Button(new Rect(x, y, w, 30f), $"Curar (+{healAmount})")) Heal();
            y += 34f;
            if (GUI.Button(new Rect(x, y, w, 30f), $"Sumar puntos (+{pointsAmount})")) AddPoints();
            y += 34f;
            if (GUI.Button(new Rect(x, y, w, 30f), "Spawnear coleccionable")) SpawnCollectible();
        }
    }
}

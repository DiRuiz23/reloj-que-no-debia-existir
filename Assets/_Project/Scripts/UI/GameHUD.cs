using Reloj.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reloj.UI
{
    /// <summary>
    /// HUD de juego (I-20): vida, puntaje y tiempo. Se coloca como prefab en cada nivel
    /// porque PlayerHealth, ScoreManager y LevelTimer viven en la escena y se reinician con ella.
    /// El indicador de la Parada Temporal (TimeStopCooldownHUD, I-08) va anidado dentro de este prefab (HUD_TimeStop).
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] UIPalette palette;

        [Header("Vida")]
        [SerializeField] Image healthBackground;
        [SerializeField] Image healthFill;
        [SerializeField] TMP_Text healthLabel;

        [Header("Puntaje y tiempo")]
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text timeLabel;

        PlayerHealth health;
        ScoreManager score;
        LevelTimer timer;

        void Start()
        {
            ApplyPalette();

            health = PlayerHealth.Instance;
            score = ScoreManager.Instance;
            timer = LevelTimer.Instance;

            // Los managers publican su valor inicial en su propio Start(), que puede
            // ocurrir antes de que nos suscribamos: leemos el estado actual a mano.
            if (health != null)
            {
                health.OnHealthChanged += SetHealth;
                SetHealth(health.Current, health.Max);
            }
            else Debug.LogWarning("GameHUD: no hay PlayerHealth en la escena.", this);

            if (score != null)
            {
                score.OnScoreChanged += SetScore;
                SetScore(score.Score);
            }
            else Debug.LogWarning("GameHUD: no hay ScoreManager en la escena.", this);

            if (timer != null)
            {
                timer.OnTimeChanged += SetTime;
                SetTime(timer.Elapsed);
            }
            else Debug.LogWarning("GameHUD: no hay LevelTimer en la escena.", this);
        }

        void OnDestroy()
        {
            if (health != null) health.OnHealthChanged -= SetHealth;
            if (score != null) score.OnScoreChanged -= SetScore;
            if (timer != null) timer.OnTimeChanged -= SetTime;
        }

        void SetHealth(int current, int max)
        {
            if (healthFill != null)
                {
                // Relleno con sprite 9-slice: se escala por anclas para no deformar las esquinas.
                float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                healthFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                healthFill.enabled = ratio > 0f;
            }
            if (healthLabel != null)
                healthLabel.text = $"{current}/{max}";
        }

        void SetScore(int value)
        {
            if (scoreLabel != null) scoreLabel.text = $"Puntaje  {value}";
        }

        void SetTime(float seconds)
        {
            if (timeLabel != null) timeLabel.text = $"Tiempo  {LevelTimer.Format(seconds)}";
        }

        // El acento naranja es solo para el reloj y la Parada Temporal: aquí no se usa.
        void ApplyPalette()
        {
            if (palette == null) return;

            // El fondo usa un sprite oscuro propio (UI_PixelRect_Corner1_Dark): sin tinte
            // para que se vea su color real y contraste con el relleno.
            if (healthBackground != null) healthBackground.color = Color.white;
            if (healthFill != null) healthFill.color = palette.SepiaMedio;

            if (healthLabel != null) healthLabel.color = palette.Pergamino;
            if (scoreLabel != null) scoreLabel.color = palette.Pergamino;
            if (timeLabel != null) timeLabel.color = palette.Pergamino;
        }
    }
}

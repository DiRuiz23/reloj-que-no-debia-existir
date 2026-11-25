using System;
using Reloj.Core;
using Reloj.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Reloj.UI
{
    public class LevelCompletePanel : StatePanel
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button menuButton;

        [Header("Habilidades (I-38)")]
        [Tooltip("Texto con los puntos de habilidad ganados en este nivel y los disponibles.")]
        [SerializeField] private TMP_Text skillPointsText;
        [Tooltip("Boton que abre el menu de habilidades (GameStateUI.OpenSkills).")]
        [SerializeField] private Button skillsButton;

        private void OnEnable()
        {
            var sp = SkillPointManager.Instance;
            if (sp != null) sp.Changed += RefreshSkillPoints;
        }

        private void OnDisable()
        {
            var sp = SkillPointManager.Instance;
            if (sp != null) sp.Changed -= RefreshSkillPoints;
        }

        // El puntaje de los recolectables (100 c/u) es PUNTAJE; los puntos de habilidad son aparte: 1 por nivel.
        private void RefreshSkillPoints()
        {
            if (skillPointsText == null) return;
            var sp = SkillPointManager.Instance;
            if (sp == null) { skillPointsText.text = ""; return; }
            skillPointsText.text = sp.LastLevelAward > 0
                ? $"Puntos de habilidad: +{sp.LastLevelAward}   (disponibles: {sp.Available})"
                : $"Puntos de habilidad disponibles: {sp.Available}";
        }

        public override void Show()
        {
            var gs = GameServices.Instance;
            if (gs != null)
            {
                var t = TimeSpan.FromSeconds(gs.Session.LastLevelTime);
                if (scoreText != null) scoreText.text = $"Puntaje: {gs.Session.LastLevelScore}";
                if (timeText != null) timeText.text = $"Tiempo: {(int)t.TotalMinutes:00}:{t.Seconds:00}";
                if (nextButton != null) nextButton.gameObject.SetActive(gs.State.HasNextLevel);
            }
            RefreshSkillPoints();
            base.Show();
        }

        protected override Selectable GetFirstSelectable()
        {
            if (nextButton != null && nextButton.gameObject.activeSelf) return nextButton;
            return menuButton;
        }
    }
}
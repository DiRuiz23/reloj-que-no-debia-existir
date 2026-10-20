using System;
using Reloj.Core;
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
            base.Show();
        }

        protected override Selectable GetFirstSelectable()
        {
            if (nextButton != null && nextButton.gameObject.activeSelf) return nextButton;
            return menuButton;
        }
    }
}
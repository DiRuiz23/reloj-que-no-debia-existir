using Reloj.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloj.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Nivel 1 (provisional hasta que exista la escena real)")]
        [SerializeField] private string level1SceneName = "Test_Sandbox";

        [Header("Botones")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Paneles")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Button creditsBackButton;

        private Button _opener;
        private bool _loading;
        private Button[] _menuButtons;
        private Navigation.Mode[] _savedNavModes;

        private void Awake()
        {
#if UNITY_WEBGL
            quitButton.gameObject.SetActive(false);   // Application.Quit() no hace nada en WebGL
#endif
            _menuButtons = new[] { playButton, continueButton, optionsButton, creditsButton, quitButton };
            _savedNavModes = new Navigation.Mode[_menuButtons.Length];
        }

        private void Start()
        {
            var save = GameServices.Instance != null ? GameServices.Instance.Save : null;
            continueButton.interactable = save != null && save.HasSaveGame;   // se activa en I-34

            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            Select(playButton);
        }

        // Recupera el foco si un clic en el vacío lo deja sin selección (teclado/gamepad).
        private void Update()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject != null) return;
            if (optionsPanel.activeSelf) Select(optionsBackButton);
            else if (creditsPanel.activeSelf) Select(creditsBackButton);
            else Select(playButton);
        }

        // ---- On Click ----
        public void Play()
        {
            if (_loading) return;   // evita doble carga por doble clic / doble Submit

            var services = GameServices.Instance;
            if (services == null || services.Loader == null)
            {
                Debug.LogWarning("[MainMenu] No hay GameServices. Inicia desde Boot.");
                return;
            }
            if (services.Loader.IsLoading) return;

            _loading = true;
            services.Loader.LoadSingle(level1SceneName);
        }

        public void Continue()
        {
            Debug.Log("[MainMenu] Continuar: pendiente (I-34).");
        }

        public void OpenOptions() => OpenPanel(optionsPanel, optionsBackButton, optionsButton);
        public void OpenCredits() => OpenPanel(creditsPanel, creditsBackButton, creditsButton);

        public void ClosePanels()
        {
            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            SetMenuNavigationBlocked(false);
            Select(_opener != null ? _opener : playButton);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OpenPanel(GameObject panel, Button back, Button opener)
        {
            if (_loading) return;
            _opener = opener;
            SetMenuNavigationBlocked(true);
            panel.SetActive(true);
            Select(back);
        }

        // Con un panel abierto, teclado/gamepad no deben poder llegar a los botones de detrás.
        // Navigation.None los excluye de la navegación automática sin cambiar su aspecto.
        private void SetMenuNavigationBlocked(bool blocked)
        {
            for (int i = 0; i < _menuButtons.Length; i++)
            {
                var b = _menuButtons[i];
                if (b == null) continue;

                var nav = b.navigation;
                if (blocked)
                {
                    if (nav.mode == Navigation.Mode.None) continue;   // ya bloqueado
                    _savedNavModes[i] = nav.mode;
                    nav.mode = Navigation.Mode.None;
                }
                else
                {
                    if (nav.mode != Navigation.Mode.None) continue;   // no estaba bloqueado
                    nav.mode = _savedNavModes[i];
                }
                b.navigation = nav;
            }
        }

        private static void Select(Button b)
        {
            if (b != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
    }
}

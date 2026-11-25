using System.Collections;
using Reloj.Core;
using Reloj.Sound;
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
        [Header("Nivel 1")]
        [SerializeField] private string level1SceneName = "Level01_Gameplay";

        [Header("Transicion de musica")]
        [Tooltip("Segundos que tarda en bajar la musica del menu antes de cargar el nivel.")]
        [SerializeField, Min(0f)] private float musicFadeOutSeconds = 1f;

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

        [Header("Confirmación de partida nueva (I-34)")]
        [Tooltip("Panel que aparece al pulsar Jugar si ya existe un guardado. Debe tapar el menú (fondo con Raycast Target).")]
        [SerializeField] private GameObject newGameConfirmPanel;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

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

            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(StartNewGame);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);
        }

        private void Start()
        {
            var save = GameServices.Instance != null ? GameServices.Instance.Save : null;
            continueButton.interactable = save != null && save.HasSaveGame;

            optionsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
            Select(playButton);
        }

        // Recupera el foco si un clic en el vacío lo deja sin selección (teclado/gamepad).
        private void Update()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject != null) return;
            if (ConfirmOpen) Select(confirmNoButton);
            else if (optionsPanel.activeSelf) Select(optionsBackButton);
            else if (creditsPanel.activeSelf) Select(creditsBackButton);
            else Select(playButton);
        }

        private bool ConfirmOpen => newGameConfirmPanel != null && newGameConfirmPanel.activeSelf;

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

            // Si ya hay un guardado, Jugar pide confirmación antes de borrarlo.
            if (services.Save != null && services.Save.HasSaveGame)
            {
                if (newGameConfirmPanel == null || confirmYesButton == null || confirmNoButton == null)
                {
                    Debug.LogError("[MainMenu] Hay un guardado pero falta el panel de confirmación: no se inicia la partida para no borrarlo.");
                    return;
                }
                OpenConfirm();
                return;
            }

            StartNewGame();
        }

        public void Continue()
        {
            if (_loading) return;

            var services = GameServices.Instance;
            if (services == null || services.Loader == null || services.Loader.IsLoading) return;

            if (services.Save == null || !services.Save.TryContinue(out string scene))
            {
                Debug.LogWarning("[MainMenu] No hay un guardado válido para continuar.");
                continueButton.interactable = false;
                Select(playButton);
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogWarning($"[MainMenu] La escena '{scene}' no está en el build.");
                return;
            }

            if (services.State != null) services.State.ResetToPlaying();
            _loading = true;
            StartCoroutine(FadeThenLoad(services, scene));
        }

        private void StartNewGame()
        {
            if (_loading) return;

            var services = GameServices.Instance;
            if (services == null || services.Loader == null || services.Loader.IsLoading) return;

            // Partida nueva: sin pistas, flags ni progreso de partidas anteriores (se conservan las opciones).
            DialogueFlagManager.Clear();
            if (services.Clues != null) services.Clues.Clear();
            if (services.Session != null) services.Session.ResetSession();
            if (services.Save != null) services.Save.ResetProgress();
            else if (services.SkillPoints != null) services.SkillPoints.ResetAll();
            if (services.State != null) services.State.ResetToPlaying();

            if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
            _loading = true;
            StartCoroutine(FadeThenLoad(services, LevelSequence.Order[0]));
        }

        // La musica del menu se apaga suave y despues se carga el nivel (que sube su propia musica con fade-in).
        private IEnumerator FadeThenLoad(GameServices services, string targetScene)
        {
            var audio = AudioManager.Instance;
            if (audio != null && musicFadeOutSeconds > 0f)
            {
                audio.FadeOutMusic(musicFadeOutSeconds);
                yield return new WaitForSecondsRealtime(musicFadeOutSeconds);
            }
            string destination = !string.IsNullOrEmpty(targetScene) ? targetScene : level1SceneName;
            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogError($"[MainMenu] La escena '{destination}' no está en el build.");
                _loading = false;
                yield break;
            }
            services.Loader.LoadSingle(destination);
        }

        private void OpenConfirm()
        {
            SetMenuNavigationBlocked(true);
            newGameConfirmPanel.SetActive(true);
            Select(confirmNoButton);   // por defecto "No": evita borrar el guardado por accidente
        }

        private void CloseConfirm()
        {
            newGameConfirmPanel.SetActive(false);
            SetMenuNavigationBlocked(false);
            Select(playButton);
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
            if (_loading || ConfirmOpen) return;
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

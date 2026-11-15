using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Reloj.Core;
using Reloj.Dialogue;
using Reloj.UI;

namespace Reloj.Clues
{
    /// <summary>
    /// Menu de pistas (I-27). NO deja ningun elemento fijo en pantalla: se abre desde el boton "Pistas"
    /// del menu de pausa (<see cref="ClueMenuButton"/>) o con un atajo (Tab / Select del mando) durante el juego.
    /// Si se abre en juego, detiene el tiempo mientras esta abierto; si se abre desde la pausa, el tiempo
    /// ya esta detenido y al cerrar se vuelve al menu de pausa. Va en el prefab HUD_Game y construye su UI por codigo.
    /// </summary>
    public class ClueMenuUI : MonoBehaviour
    {
        public static ClueMenuUI Instance { get; private set; }

        [SerializeField] UIPalette palette;
        [SerializeField] TMP_FontAsset font;
        [Tooltip("Atajo opcional para abrir/cerrar durante el juego. Si esta vacio se usan Tab y Select del mando.")]
        [SerializeField] InputAction toggleAction = new InputAction("ToggleClues", InputActionType.Button);
        [SerializeField, Min(100f)] float scrollSpeed = 900f;

        public bool IsOpen { get; private set; }

        Canvas canvas;
        GameObject panelRoot;
        RectTransform content;
        ScrollRect scroll;
        TMP_Text titleLabel;
        Button closeButton;
        ClueInventory inventory;
        bool frozeTime;
        bool openedFromPause;
        GameObject previousSelection;

        Color Tinta => palette != null ? palette.Tinta : new Color(0.17f, 0.11f, 0.08f);
        Color Tierra => palette != null ? palette.TierraOscura : new Color(0.31f, 0.21f, 0.15f);
        Color Perga => palette != null ? palette.Pergamino : new Color(0.92f, 0.85f, 0.71f);
        Color Arena => palette != null ? palette.Arena : new Color(0.79f, 0.66f, 0.47f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            if (toggleAction.bindings.Count == 0)
            {
                toggleAction.AddBinding("<Keyboard>/tab");
                toggleAction.AddBinding("<Gamepad>/select");
            }
            BuildUI();
            panelRoot.SetActive(false);
        }

        void OnEnable()
        {
            toggleAction.performed += OnTogglePerformed;
            toggleAction.Enable();
        }

        void OnDisable()
        {
            toggleAction.performed -= OnTogglePerformed;
            toggleAction.Disable();
            if (IsOpen) Close();
        }

        void Start()
        {
            Bind();
            var gs = GameServices.Instance;
            if (gs != null && gs.State != null) gs.State.OnStateChanged += OnGameStateChanged;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (inventory != null) inventory.OnChanged -= Refresh;
            var gs = GameServices.Instance;
            if (gs != null && gs.State != null) gs.State.OnStateChanged -= OnGameStateChanged;
        }

        void Bind()
        {
            var current = ClueInventory.Current;
            if (current == null || current == inventory) return;
            if (inventory != null) inventory.OnChanged -= Refresh;
            inventory = current;
            inventory.OnChanged += Refresh;
        }

        void OnTogglePerformed(InputAction.CallbackContext ctx) => Toggle();

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        /// <summary>Abre el menu. Valido en juego (detiene el tiempo) o desde la pausa (el tiempo ya esta detenido).</summary>
        public void Open()
        {
            if (IsOpen) return;
            if (DialogueRunner.IsRunning) return;                 // no se abre en medio de un dialogo

            var gs = GameServices.Instance;
            var state = gs != null ? gs.State : null;
            var current = state != null ? state.CurrentState : GameState.Jugando;
            if (current != GameState.Jugando && current != GameState.Pausado) return;   // ni fin de nivel ni game over

            Bind();
            Refresh();
            IsOpen = true;
            openedFromPause = current == GameState.Pausado;
            panelRoot.SetActive(true);

            if (!openedFromPause && Time.timeScale > 0f) { Time.timeScale = 0f; frozeTime = true; }
            scroll.verticalNormalizedPosition = 1f;

            var es = EventSystem.current;
            if (es != null)
            {
                previousSelection = es.currentSelectedGameObject;
                es.SetSelectedGameObject(closeButton.gameObject);
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);
            RestoreTime();
            RestoreSelection();
            openedFromPause = false;
        }

        void RestoreSelection()
        {
            var es = EventSystem.current;
            if (es != null && previousSelection != null && previousSelection.activeInHierarchy)
                es.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }

        void RestoreTime()
        {
            if (!frozeTime) return;
            frozeTime = false;
            var gs = GameServices.Instance;
            // Solo se devuelve el tiempo si seguimos en 'Jugando': en pausa/fin de nivel manda GameStateManager.
            if (gs == null || gs.State == null || gs.State.CurrentState == GameState.Jugando) Time.timeScale = 1f;
        }

        void OnGameStateChanged(GameState prev, GameState next)
        {
            if (!IsOpen) return;
            // Abierto en juego y se pausa / termina el nivel: se cierra sin tocar el tiempo (manda GameStateManager).
            // Abierto desde la pausa y se reanuda (Esc / Start): tambien se cierra para no quedar sobre el juego.
            bool close = openedFromPause ? next != GameState.Pausado : next != GameState.Jugando;
            if (!close) return;
            frozeTime = false;
            IsOpen = false;
            panelRoot.SetActive(false);
            previousSelection = null;
            openedFromPause = false;
        }

        void Update()
        {
            if (!IsOpen) return;

            // Cerrar con "atras" del mando; desplazar la lista con stick / flechas.
            var pad = Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) { Close(); return; }

            float v = 0f;
            if (pad != null) v += pad.leftStick.y.ReadValue() + pad.dpad.y.ReadValue();
            var kb = Keyboard.current;
            if (kb != null) v += (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(v) > 0.1f && content != null)
            {
                float range = Mathf.Max(1f, content.rect.height - ((RectTransform)scroll.transform).rect.height);
                scroll.verticalNormalizedPosition = Mathf.Clamp01(
                    scroll.verticalNormalizedPosition + v * scrollSpeed / range * Time.unscaledDeltaTime);
            }
        }

        // ------------------------------------------------------------------ lista
        void Refresh()
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

            int n = inventory != null ? inventory.Count : 0;
            titleLabel.text = $"Pistas ({n})";

            if (n == 0)
            {
                AddText("Aún no has reunido pistas.", 22, FontStyles.Italic, Arena);
                return;
            }
            foreach (var c in inventory.Clues)
            {
                if (c == null) continue;
                AddEntry(c);
            }
        }

        void AddEntry(ClueData c)
        {
            var row = new GameObject("Pista_" + c.Id, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            row.transform.SetParent(content, false);
            row.GetComponent<Image>().color = new Color(Tierra.r, Tierra.g, Tierra.b, 0.55f);
            var vl = row.GetComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(16, 16, 10, 12);
            vl.spacing = 4;
            vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddText(c.Title, 24, FontStyles.Bold, Perga, row.transform);
            if (!string.IsNullOrEmpty(c.Summary)) AddText(c.Summary, 20, FontStyles.Normal, Arena, row.transform);
        }

        TMP_Text AddText(string text, float size, FontStyles style, Color color, Transform parent = null)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : content, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        // ------------------------------------------------------------------ construccion de la UI
        void BuildUI()
        {
            var cgo = new GameObject("ClueMenuCanvas", typeof(RectTransform));
            var crt = (RectTransform)cgo.transform;
            crt.SetParent(transform, false);
            Stretch(crt);   // sin esto el RectTransform queda de 100x100 centrado y todo se dibuja en medio de la pantalla
            canvas = cgo.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 40;   // sobre el HUD (5) y el menu de pausa (10)
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            // Panel: fondo oscuro a pantalla completa + ventana central. Todo oculto hasta que se abre.
            panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var prt = (RectTransform)panelRoot.transform;
            prt.SetParent(cgo.transform, false);
            Stretch(prt);
            panelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            var win = new GameObject("Ventana", typeof(RectTransform), typeof(Image));
            var wrt = (RectTransform)win.transform;
            wrt.SetParent(prt, false);
            wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(900f, 760f);
            win.GetComponent<Image>().color = new Color(Tinta.r, Tinta.g, Tinta.b, 0.97f);

            titleLabel = AddFixedText(wrt, "Titulo", "Pistas", 34, FontStyles.Bold, Perga, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -24f), new Vector2(-200f, 50f));

            AddFixedText(wrt, "Ayuda", "Tab / Select / B: cerrar", 18, FontStyles.Italic, Arena, TextAlignmentOptions.Right,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -34f), new Vector2(280f, 30f), new Vector2(1f, 1f));

            // Lista con scroll
            var sgo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            var srt = (RectTransform)sgo.transform;
            srt.SetParent(wrt, false);
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(32f, 90f); srt.offsetMax = new Vector2(-32f, -90f);
            scroll = sgo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var cont = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content = (RectTransform)cont.transform;
            content.SetParent(srt, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var vl = cont.GetComponent<VerticalLayoutGroup>();
            vl.spacing = 12; vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            cont.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = srt;

            // Boton cerrar
            var bgo = new GameObject("Cerrar", typeof(RectTransform), typeof(Image), typeof(Button));
            var brt = (RectTransform)bgo.transform;
            brt.SetParent(wrt, false);
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 24f);
            brt.sizeDelta = new Vector2(240f, 52f);
            bgo.GetComponent<Image>().color = Tierra;
            closeButton = bgo.GetComponent<Button>();
            closeButton.targetGraphic = bgo.GetComponent<Image>();
            var cb = closeButton.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.selectedColor = new Color(1.25f, 1.15f, 1f, 1f);
            closeButton.colors = cb;
            closeButton.onClick.AddListener(Close);
            AddFixedText(brt, "Texto", "Cerrar", 24, FontStyles.Normal, Perga, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        TMP_Text AddFixedText(RectTransform parent, string name, string text, float size, FontStyles style, Color color,
            TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = pivot ?? new Vector2(0f, 1f);
            if (aMin == Vector2.zero && aMax == Vector2.one) { rt.pivot = new Vector2(0.5f, 0.5f); Stretch(rt); }
            else { rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta; }
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}

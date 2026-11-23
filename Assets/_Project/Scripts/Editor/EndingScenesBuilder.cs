using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Reloj.Endings;
using Reloj.Sound;
using Reloj.UI;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Construye las escenas Decision_Final, Ending_A y Ending_B con el estilo del menu
    /// (paleta sepia, fuente Alegreya, Button_Base) y las agrega al Build Settings.
    /// Menu: Reloj > Finales > Construir escenas de decision y finales
    /// </summary>
    public static class EndingScenesBuilder
    {
        const string ScenesFolder = "Assets/_Project/Scenes/";
        const string PalettePath = "Assets/_Project/Data/UI/UIPalette.asset";
        const string FontPath = "Assets/_Project/Art/UI/Fonts/Alegreya SDF.asset";
        const string ButtonPath = "Assets/_Project/Art/UI/Button_Base.prefab";
        const string MixerPath = "Assets/_Project/Audio/MainMixer.mixer";
        const string ClickPath = "Assets/_Project/Audio/SFX/sfx_ui_click.wav";
        const string TickPath = "Assets/_Project/Audio/SFX/sfx_ticktock_loop.wav";
        const string MusicA = "Assets/_Project/Audio/Music/mus_ending_a_loop.wav";
        const string MusicB = "Assets/_Project/Audio/Music/mus_ending_b_loop.wav";

        static UIPalette palette;
        static TMP_FontAsset font;

        [MenuItem("Reloj/Finales/Construir escenas de decision y finales")]
        public static void BuildAll()
        {
            palette = AssetDatabase.LoadAssetAtPath<UIPalette>(PalettePath);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (palette == null || font == null)
            {
                Debug.LogError("[Finales] Falta UIPalette o la fuente Alegreya.");
                return;
            }

            BuildDecision();
            BuildEnding("Ending_A", "ending_a", "FINAL A", "La hora corregida", MusicA, EndingClock.Mode.SpinForward,
                new Color(0.17f, 0.11f, 0.08f), new Color(0.36f, 0.26f, 0.17f));
            BuildEnding("Ending_B", "ending_b", "FINAL B", "La vida que conozco", MusicB, EndingClock.Mode.Stopped,
                new Color(0.12f, 0.09f, 0.08f), new Color(0.24f, 0.17f, 0.13f));

            foreach (var n in new[] { "Decision_Final", "Ending_A", "Ending_B" })
                AddToBuild(ScenesFolder + n + ".unity");

            AssetDatabase.SaveAssets();
            Debug.Log("[Finales] Escenas Decision_Final, Ending_A y Ending_B construidas.");
        }

        // ------------------------------------------------------------------ escenas

        static void BuildDecision()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateCommon(scene, MusicA, null, true);   // la musica de la decision es la del tic-tac (sin pista propia)

            var clock = CreateClock(canvas, EndingClock.Mode.Stopped, 560f, 0.35f, new Vector2(0f, 60f));

            var panel = Panel(canvas, "Panel_Texto", new Vector2(0.5f, 0.62f), new Vector2(1400f, 470f), 0.78f);
            var speaker = Label(panel, "Lbl_Hablante", "Juan", 42, palette.Arena, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-80f, 60f));
            var body = Label(panel, "Lbl_Texto", "", 38, palette.Pergamino, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(-120f, -150f));
            body.alignment = TextAlignmentOptions.Center;

            var options = NewRect("Opciones", canvas);
            options.anchorMin = options.anchorMax = new Vector2(0.5f, 0.2f);
            options.sizeDelta = new Vector2(1200f, 230f);
            var group = options.gameObject.AddComponent<CanvasGroup>();
            var layout = options.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            var btnA = MakeButton(options, "Btn_Opcion_A", "Opcion A", 108f, 34);
            var btnB = MakeButton(options, "Btn_Opcion_B", "Opcion B", 108f, 34);
            // Navegacion explicita para teclado y gamepad.
            LinkVertical(btnA, btnB);

            var fade = CreateFade(canvas);

            var ctl = new GameObject("DecisionController").AddComponent<DecisionSceneController>();
            var so = new SerializedObject(ctl);
            so.FindProperty("speakerLabel").objectReferenceValue = speaker;
            so.FindProperty("bodyLabel").objectReferenceValue = body;
            so.FindProperty("optionsGroup").objectReferenceValue = group;
            so.FindProperty("optionAButton").objectReferenceValue = btnA;
            so.FindProperty("optionBButton").objectReferenceValue = btnB;
            so.FindProperty("fade").objectReferenceValue = fade;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Audio de la decision: sin musica, solo el tic-tac del reloj.
            var audio = Object.FindFirstObjectByType<AudioManager>();
            var aso = new SerializedObject(audio);
            aso.FindProperty("music").objectReferenceValue = null;
            aso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenesFolder + "Decision_Final.unity");
        }

        static void BuildEnding(string sceneName, string dialogueId, string tag, string title, string musicPath,
            EndingClock.Mode clockMode, Color from, Color to)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateCommon(scene, musicPath, null, false);

            var backdrop = canvas.Find("Img_Fondo").GetComponent<Image>();
            backdrop.color = from;

            var endClock = CreateClock(canvas, clockMode, 620f, 0.5f, new Vector2(0f, 120f));
            var dialSo = new SerializedObject(endClock);
            dialSo.FindProperty("dialSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Images/reloj_fondo.png");
            dialSo.ApplyModifiedPropertiesWithoutUndo();

            // Tarjeta de titulo
            var intro = NewRect("Titulo", canvas);
            Stretch(intro);
            var introGroup = intro.gameObject.AddComponent<CanvasGroup>();
            introGroup.blocksRaycasts = false;
            Label(intro, "Lbl_Etiqueta", tag, 44, palette.Arena, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1200f, 70f)).characterSpacing = 18f;
            Label(intro, "Lbl_Titulo", title, 110, palette.Pergamino, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1500f, 170f));

            // Paginas de texto
            var pageRect = Panel(canvas, "Pagina", new Vector2(0.5f, 0.25f), new Vector2(1500f, 400f), 0.8f);
            var pageGroup = pageRect.gameObject.AddComponent<CanvasGroup>();
            pageGroup.blocksRaycasts = false;
            var speaker = Label(pageRect, "Lbl_Hablante", "Juan", 40, palette.Arena, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(-80f, 56f));
            var body = Label(pageRect, "Lbl_Texto", "", 36, palette.Pergamino, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(-120f, -130f));

            var hint = NewRect("Pista_Continuar", canvas);
            hint.anchorMin = hint.anchorMax = new Vector2(0.5f, 0.045f);
            hint.sizeDelta = new Vector2(1200f, 50f);
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            Label(hint, "Lbl", "Clic, toque o tecla para continuar", 28, palette.Arena, FontStyles.Italic, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Cierre
            var fin = NewRect("Cierre", canvas);
            Stretch(fin);
            var finGroup = fin.gameObject.AddComponent<CanvasGroup>();
            var finBg = fin.gameObject.AddComponent<Image>();
            finBg.color = new Color(palette.Tinta.r, palette.Tinta.g, palette.Tinta.b, 0.88f);
            finBg.raycastTarget = false;
            Label(fin, "Lbl_Fin", "FIN", 130, palette.Pergamino, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 190f)).characterSpacing = 30f;
            var holder = NewRect("Boton", fin);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
            holder.sizeDelta = new Vector2(520f, 84f);
            holder.anchoredPosition = new Vector2(0f, -90f);
            var menuBtn = MakeButton(holder, "Btn_Menu", "Volver al menú principal", 84f, 36);
            var mrt = (RectTransform)menuBtn.transform;
            Stretch(mrt);
            var nav = menuBtn.navigation; nav.mode = Navigation.Mode.None; menuBtn.navigation = nav;

            var fade = CreateFade(canvas);

            var ctl = new GameObject("EndingController").AddComponent<EndingController>();
            var so = new SerializedObject(ctl);
            so.FindProperty("endingId").stringValue = sceneName;
            so.FindProperty("dialogueId").stringValue = dialogueId;
            so.FindProperty("introGroup").objectReferenceValue = introGroup;
            so.FindProperty("pageGroup").objectReferenceValue = pageGroup;
            so.FindProperty("speakerLabel").objectReferenceValue = speaker;
            so.FindProperty("bodyLabel").objectReferenceValue = body;
            so.FindProperty("hintGroup").objectReferenceValue = hintGroup;
            so.FindProperty("finalGroup").objectReferenceValue = finGroup;
            so.FindProperty("menuButton").objectReferenceValue = menuBtn;
            so.FindProperty("fade").objectReferenceValue = fade;
            so.FindProperty("backdrop").objectReferenceValue = backdrop;
            so.FindProperty("backdropFrom").colorValue = from;
            so.FindProperty("backdropTo").colorValue = to;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenesFolder + sceneName + ".unity");
        }

        // ------------------------------------------------------------------ piezas comunes

        static RectTransform CreateCommon(Scene scene, string musicPath, string unused, bool decision)
        {
            // Camara (fondo solido) con AudioListener
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = palette.Tinta;
            cam.orthographic = true;
            cam.cullingMask = 0;
            camGo.AddComponent<AudioListener>();

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rt = (RectTransform)canvasGo.transform;
            var bg = NewRect("Img_Fondo", rt);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = palette.Tinta;
            bgImg.raycastTarget = false;

            // EventSystem (raton, teclado, gamepad y toque)
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            // Se copia la configuracion del EventSystem del menu (misma asignacion de acciones: raton, teclado, gamepad y toque).
            var menuScene = EditorSceneManager.OpenScene(ScenesFolder + "MainMenu.unity", OpenSceneMode.Additive);
            foreach (var r in menuScene.GetRootGameObjects())
            {
                var src = r.GetComponent<InputSystemUIInputModule>();
                if (src != null) EditorUtility.CopySerialized(src, module);
            }
            EditorSceneManager.CloseScene(menuScene, true);

            // Audio
            var audioGo = new GameObject("AudioManager");
            audioGo.SetActive(false);   // se configura antes de que corra Awake
            var am = audioGo.AddComponent<AudioManager>();
            var aso = new SerializedObject(am);
            aso.FindProperty("mixer").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(MixerPath);
            aso.FindProperty("music").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
            aso.FindProperty("uiClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
            aso.FindProperty("tickTock").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(TickPath);
            aso.FindProperty("musicVolume").floatValue = 0.5f;
            aso.FindProperty("musicFadeIn").floatValue = 3f;
            aso.ApplyModifiedPropertiesWithoutUndo();
            audioGo.SetActive(true);

            return rt;
        }

        static EndingClock CreateClock(RectTransform canvas, EndingClock.Mode mode, float size, float alphaScale, Vector2 pos)
        {
            var rt = NewRect("Reloj", canvas);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.SetSiblingIndex(1);   // justo encima del fondo, detras del texto
            var clock = rt.gameObject.AddComponent<EndingClock>();
            var so = new SerializedObject(clock);
            so.FindProperty("mode").enumValueIndex = (int)mode;
            so.FindProperty("palette").objectReferenceValue = palette;
            so.FindProperty("circleSprite").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            so.FindProperty("size").floatValue = size;
            so.FindProperty("alphaScale").floatValue = alphaScale;
            so.ApplyModifiedPropertiesWithoutUndo();
            return clock;
        }

        static CanvasGroup CreateFade(RectTransform canvas)
        {
            var rt = NewRect("Fundido", canvas);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 1f; g.blocksRaycasts = false; g.interactable = false;
            return g;
        }

        static RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 size, float alpha)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            Color c = palette.Tinta; c.a = alpha;
            img.color = c;
            img.raycastTarget = false;
            return rt;
        }

        static Button MakeButton(RectTransform parent, string name, string text, float height, int fontSize)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            var rt = (RectTransform)go.transform;
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            if (parent.GetComponent<LayoutGroup>() == null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            var label = go.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = text;
                label.font = font;
                label.fontSize = fontSize;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.alignment = TextAlignmentOptions.Center;
            }
            var style = go.GetComponent<PaletteButtonStyle>();
            if (style != null) style.Apply();
            return go.GetComponent<Button>();
        }

        static void LinkVertical(Button a, Button b)
        {
            var na = a.navigation; na.mode = Navigation.Mode.Explicit; na.selectOnDown = b; na.selectOnUp = b; a.navigation = na;
            var nb = b.navigation; nb.mode = Navigation.Mode.Explicit; nb.selectOnUp = a; nb.selectOnDown = a; b.navigation = nb;
        }

        static TextMeshProUGUI Label(RectTransform parent, string name, string text, float size, Color color, FontStyles style,
            TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void AddToBuild(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list)
                if (s.path == path) { s.enabled = true; EditorBuildSettings.scenes = list.ToArray(); return; }
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}

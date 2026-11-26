using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloj.Tests.PlayMode
{
    public class AndroidBackAndTouchTests
    {
        private const float StepTimeoutSeconds = 15.0f;
        private const string SaveKey = "reloj.save";
        private const string TutorialKey = "tutorial_completado";

        private bool _hadSaveKey;
        private string _savedSaveJson;
        private bool _hadTutorialKey;
        private int _savedTutorialInt;
        private InputSettings.BackgroundBehavior _savedBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _savedEditorInputBehavior;
        private Keyboard _virtualKeyboard;
        private readonly List<string> _consoleErrors = new List<string>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hadSaveKey = PlayerPrefs.HasKey(SaveKey);
            _savedSaveJson = _hadSaveKey ? PlayerPrefs.GetString(SaveKey, "") : "";
            _hadTutorialKey = PlayerPrefs.HasKey(TutorialKey);
            _savedTutorialInt = _hadTutorialKey ? PlayerPrefs.GetInt(TutorialKey, 0) : 0;

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(TutorialKey);
            PlayerPrefs.Save();

            _consoleErrors.Clear();
            Application.logMessageReceived += CaptureLogErrors;

            _savedBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            _savedEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            _virtualKeyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(_virtualKeyboard);
            _virtualKeyboard.MakeCurrent();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CaptureLogErrors;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (_virtualKeyboard != null)
            {
                InputSystem.RemoveDevice(_virtualKeyboard);
                _virtualKeyboard = null;
            }
            InputSystem.settings.backgroundBehavior = _savedBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _savedEditorInputBehavior;
            SetInputScheme("Keyboard");

            if (_hadSaveKey) PlayerPrefs.SetString(SaveKey, _savedSaveJson);
            else PlayerPrefs.DeleteKey(SaveKey);

            if (_hadTutorialKey) PlayerPrefs.SetInt(TutorialKey, _savedTutorialInt);
            else PlayerPrefs.DeleteKey(TutorialKey);

            PlayerPrefs.Save();
            yield return null;
        }

        private void CaptureLogErrors(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _consoleErrors.Add($"[{type}] {condition}\n{stackTrace}");
        }

        [UnityTest]
        public IEnumerator AndroidBack_InGameplay_OpensPause_ClosesSubpanelsFirst_PreservesTouchScheme()
        {
            yield return BootToMainMenu();
            yield return LoadLevelFromMainMenu(1);

            SetInputScheme("Touch");
            yield return null;

            var touchUi = FindComponent("Reloj.UI.TouchControlsUI");
            Assert.IsNotNull(touchUi, "Falta TouchControlsUI en Level01_Gameplay.");
            var controlsRoot = GetField<GameObject>(touchUi, "controlsRoot");
            Assert.IsNotNull(controlsRoot, "controlsRoot no asignado en TouchControlsUI.");
            Assert.IsTrue(controlsRoot.activeSelf, "Los controles tactiles deben verse en Jugando con esquema Touch.");

            // 1. Atras durante Jugando -> abre Pausa y mantiene esquema Touch
            yield return TapEscape();
            AssertGameState("Pausado");
            Assert.IsFalse(controlsRoot.activeSelf, "Los controles tactiles deben ocultarse al pausar.");
            Assert.AreEqual("Touch", GetCurrentInputScheme(), "El boton Atras no debe cambiar el esquema Touch a Keyboard.");

            var stateUi = FindComponent("Reloj.UI.GameStateUI");
            Assert.IsNotNull(stateUi, "Falta GameStateUI en Level01_Gameplay.");

            // 2. Opciones abierto en Pausa + Atras -> cierra Opciones y sigue en Pausa
            var optionsPanel = GetField<GameObject>(stateUi, "optionsPanel");
            Assert.IsNotNull(optionsPanel, "Falta optionsPanel en GameStateUI.");
            Invoke(stateUi, "OpenOptions");
            yield return null;
            Assert.IsTrue(optionsPanel.activeSelf, "OptionsPanel debe abrirse desde Pausa.");

            yield return TapEscape();
            Assert.IsFalse(optionsPanel.activeSelf, "Atras debe cerrar OptionsPanel.");
            AssertGameState("Pausado");

            // 3. Habilidades abierto en Pausa + Atras -> cierra Habilidades y sigue en Pausa
            var skillPanel = GetField<Component>(stateUi, "skillPanel");
            Assert.IsNotNull(skillPanel, "Falta skillPanel en GameStateUI.");
            Invoke(stateUi, "OpenSkills");
            yield return null;
            Assert.IsTrue(skillPanel.gameObject.activeSelf, "SkillMenuPanel debe abrirse desde Pausa.");

            yield return TapEscape();
            Assert.IsFalse(skillPanel.gameObject.activeSelf, "Atras debe cerrar SkillMenuPanel.");
            AssertGameState("Pausado");

            // 4. Pistas abierto en Pausa + Atras -> cierra Pistas y sigue en Pausa
            var clueMenu = GetStaticProp<object>("Reloj.Clues.ClueMenuUI", "Instance");
            Assert.IsNotNull(clueMenu, "Falta ClueMenuUI.Instance en Level01_Gameplay.");
            Invoke(clueMenu, "Open");
            yield return null;
            Assert.IsTrue(GetProp<bool>(clueMenu, "IsOpen"), "ClueMenuUI debe abrirse desde Pausa.");

            yield return TapEscape();
            Assert.IsFalse(GetProp<bool>(clueMenu, "IsOpen"), "Atras debe cerrar ClueMenuUI.");
            AssertGameState("Pausado");

            // 5. Atras en raiz de Pausa -> reanuda juego y restaura controles tactiles
            yield return TapEscape();
            AssertGameState("Jugando");
            Assert.AreEqual("Touch", GetCurrentInputScheme(), "Al reanudar con Atras debe seguir activo el esquema Touch.");
            Assert.IsTrue(controlsRoot.activeSelf, "Los controles tactiles deben reaparecer al reanudar.");

            AssertZeroConsoleErrors();
        }

        [UnityTest]
        public IEnumerator AndroidBack_InMainMenu_ClosesOptionsAndCredits_WithoutErrors()
        {
            yield return BootToMainMenu();

            SetInputScheme("Touch");
            var menu = FindComponent("Reloj.UI.MainMenuController");
            Assert.IsNotNull(menu, "Falta MainMenuController en MainMenu.");

            var optionsPanel = GetField<GameObject>(menu, "optionsPanel");
            var creditsPanel = GetField<GameObject>(menu, "creditsPanel");
            Assert.IsNotNull(optionsPanel, "Falta optionsPanel en MainMenuController.");
            Assert.IsNotNull(creditsPanel, "Falta creditsPanel en MainMenuController.");

            Invoke(menu, "OpenOptions");
            yield return null;
            Assert.IsTrue(optionsPanel.activeSelf, "OptionsPanel debe abrirse en MainMenu.");

            yield return TapEscape();
            Assert.IsFalse(optionsPanel.activeSelf, "Atras debe cerrar OptionsPanel en MainMenu.");

            Invoke(menu, "OpenCredits");
            yield return null;
            Assert.IsTrue(creditsPanel.activeSelf, "CreditsPanel debe abrirse en MainMenu.");

            yield return TapEscape();
            Assert.IsFalse(creditsPanel.activeSelf, "Atras debe cerrar CreditsPanel en MainMenu.");

            AssertZeroConsoleErrors();
        }

        [UnityTest]
        public IEnumerator TouchControls_PresentAndVisible_InAllFourLevels()
        {
            yield return BootToMainMenu();
            SetInputScheme("Touch");

            for (int level = 1; level <= 4; level++)
            {
                if (level == 1)
                    yield return LoadLevelFromMainMenu(1);
                else
                    yield return LoadLevelViaLoader(level);

                SetInputScheme("Touch");
                yield return null;

                var touchUi = FindComponent("Reloj.UI.TouchControlsUI");
                Assert.IsNotNull(touchUi, $"Falta TouchControlsUI en Level{level:00}_Gameplay.");
                var controlsRoot = GetField<GameObject>(touchUi, "controlsRoot");
                Assert.IsNotNull(controlsRoot, $"controlsRoot nulo en Level{level:00}_Gameplay.");
                Assert.IsTrue(controlsRoot.activeSelf, $"Los controles tactiles no se mostraron en Level{level:00}_Gameplay.");
            }

            AssertZeroConsoleErrors();
        }

        // ------------------------------------------------------------------ Helpers

        private IEnumerator BootToMainMenu()
        {
            var bootOp = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            while (!bootOp.isDone) yield return null;
            yield return WaitForSceneActive("Splash");

            var splash = FindComponent("SplashController");
            Assert.IsNotNull(splash, "Falta SplashController en Splash.");
            SetField(splash, "duracionEntrada", 0.01f);
            SetField(splash, "duracionEspera", 0.01f);
            SetField(splash, "duracionSalida", 0.01f);

            yield return WaitForSceneActive("MainMenu");
            var loader = GetProp<object>(GetStaticProp<object>("Reloj.Core.GameServices", "Instance"), "Loader");
            yield return WaitUntil(() => !GetProp<bool>(loader, "IsLoading"), "Loader no termino en MainMenu.");
        }

        private IEnumerator LoadLevelFromMainMenu(int levelNumber)
        {
            var menu = FindComponent("Reloj.UI.MainMenuController");
            Assert.IsNotNull(menu, "Falta MainMenuController.");
            SetField(menu, "musicFadeOutSeconds", 0f);
            Invoke(menu, "Play");
            yield return WaitForLevelLoaded(levelNumber);
        }

        private IEnumerator LoadLevelViaLoader(int levelNumber)
        {
            var gs = GetStaticProp<object>("Reloj.Core.GameServices", "Instance");
            var state = GetProp<object>(gs, "State");
            Invoke(state, "ResetToPlaying");
            var loader = GetProp<object>(gs, "Loader");
            Invoke(loader, "LoadSingle", $"Level{levelNumber:00}_Gameplay");
            yield return WaitForLevelLoaded(levelNumber);
        }

        private IEnumerator WaitForLevelLoaded(int levelNumber)
        {
            string gameplay = $"Level{levelNumber:00}_Gameplay";
            string art = $"Level{levelNumber:00}_Art";
            var gs = GetStaticProp<object>("Reloj.Core.GameServices", "Instance");
            var loader = GetProp<object>(gs, "Loader");

            yield return WaitUntil(() => SceneManager.GetActiveScene().name == gameplay && !GetProp<bool>(loader, "IsLoading"),
                $"Timeout cargando {gameplay}.");
            yield return WaitUntil(() => SceneManager.GetSceneByName(art).isLoaded && !GetProp<bool>(loader, "IsLoading"),
                $"Timeout cargando escena aditiva {art}.");
            yield return null;
        }

        private IEnumerator WaitForSceneActive(string sceneName)
        {
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == sceneName,
                $"Timeout esperando escena '{sceneName}'.");
        }

        private IEnumerator WaitUntil(Func<bool> predicate, string failMessage)
        {
            float timeoutAt = Time.realtimeSinceStartup + StepTimeoutSeconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail(failMessage);
                yield return null;
            }
        }

        private IEnumerator TapEscape()
        {
            if (_virtualKeyboard == null) yield break;
            if (!_virtualKeyboard.enabled) InputSystem.EnableDevice(_virtualKeyboard);
            _virtualKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_virtualKeyboard, new KeyboardState(Key.Escape));
            yield return null;
            InputSystem.QueueStateEvent(_virtualKeyboard, new KeyboardState());
            yield return null;
        }

        private void AssertGameState(string expectedStateName)
        {
            var gs = GetStaticProp<object>("Reloj.Core.GameServices", "Instance");
            var state = GetProp<object>(gs, "State");
            var current = GetProp<object>(state, "CurrentState");
            Assert.AreEqual(expectedStateName, current.ToString());
        }

        private void AssertZeroConsoleErrors()
        {
            Assert.AreEqual(0, _consoleErrors.Count,
                $"Se detectaron {_consoleErrors.Count} errores en consola:\n{string.Join("\n---\n", _consoleErrors)}");
        }

        private static void SetInputScheme(string schemeName)
        {
            var trackerType = FindType("Reloj.Tutorial.InputDeviceTracker");
            var schemeType = FindType("Reloj.Tutorial.InputScheme");
            var scheme = Enum.Parse(schemeType, schemeName);
            var m = trackerType.GetMethod("Set", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(m);
            m.Invoke(null, new[] { scheme });
        }

        private static string GetCurrentInputScheme() =>
            GetStaticProp<object>("Reloj.Tutorial.InputDeviceTracker", "Current").ToString();

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            Assert.Fail($"No se encontro el tipo '{fullName}'.");
            return null;
        }

        private static Component FindComponent(string fullTypeName)
        {
            var t = FindType(fullTypeName);
            return UnityEngine.Object.FindFirstObjectByType(t) as Component;
        }

        private static T GetStaticProp<T>(string typeName, string propName)
        {
            var t = FindType(typeName);
            var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(p, $"Propiedad estatica '{typeName}.{propName}' no encontrada.");
            return (T)p.GetValue(null);
        }

        private static T GetProp<T>(object target, string propName)
        {
            Assert.IsNotNull(target, $"Target nulo leyendo propiedad '{propName}'.");
            var p = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(p, $"Propiedad '{target.GetType().Name}.{propName}' no encontrada.");
            return (T)p.GetValue(target);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            Assert.IsNotNull(target, $"Target nulo leyendo campo '{fieldName}'.");
            var f = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Campo '{target.GetType().Name}.{fieldName}' no encontrado.");
            return (T)f.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            Assert.IsNotNull(target, $"Target nulo escribiendo campo '{fieldName}'.");
            var f = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Campo '{target.GetType().Name}.{fieldName}' no encontrado.");
            f.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            Assert.IsNotNull(target, $"Target nulo invocando '{methodName}'.");
            foreach (var m in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name != methodName) continue;
                var ps = m.GetParameters();
                if (ps.Length != args.Length) continue;
                bool match = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (args[i] != null && !ps[i].ParameterType.IsInstanceOfType(args[i]))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return m.Invoke(target, args);
            }
            Assert.Fail($"Metodo '{target.GetType().Name}.{methodName}' con {args.Length} arg(s) no encontrado.");
            return null;
        }
    }
}

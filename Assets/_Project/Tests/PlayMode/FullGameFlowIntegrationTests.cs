using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Reloj.Tests.PlayMode
{
    public class FullGameFlowIntegrationTests
    {
        private const float MaxLoadSeconds = 5.0f;
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
        private readonly List<(string transition, float seconds)> _loadTimings = new List<(string, float)>();

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
            _loadTimings.Clear();
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
        public IEnumerator TwoConsecutiveFullRuns_PersistProgress_UnderFiveSeconds_ZeroConsoleErrors()
        {
            yield return RunFullPlaythrough(runIndex: 1, chooseEndingA: true);
            yield return RunFullPlaythrough(runIndex: 2, chooseEndingA: false);

            var sb = new StringBuilder();
            sb.AppendLine("[I-41] Tiempos de carga medidos:");
            foreach (var (transition, seconds) in _loadTimings)
            {
                sb.AppendLine($"  - {transition}: {seconds:F3} s (limite < {MaxLoadSeconds:F1} s)");
                Assert.Less(seconds, MaxLoadSeconds, $"La transicion '{transition}' excedio {MaxLoadSeconds}s ({seconds:F3}s).");
            }
            Debug.Log(sb.ToString());

            Assert.AreEqual(0, _consoleErrors.Count,
                $"Se detectaron {_consoleErrors.Count} errores en consola:\n{string.Join("\n---\n", _consoleErrors)}");
        }

        private IEnumerator RunFullPlaythrough(int runIndex, bool chooseEndingA)
        {
            string tag = $"Corrida {runIndex}";

            if (runIndex == 1)
            {
                // 1. Boot -> Splash -> MainMenu
                var bootSw = Stopwatch.StartNew();
                var bootOp = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
                while (!bootOp.isDone) yield return null;
                yield return WaitForSceneActive("Splash");
                bootSw.Stop();
                RecordTiming($"{tag}: Boot -> Splash", (float)bootSw.Elapsed.TotalSeconds);

                // Acelera animacion del Splash sin tocar el asset para medir la carga real Splash -> MainMenu
                var splash = FindComponent("SplashController");
                Assert.IsNotNull(splash, "Falta SplashController en Splash.");
                SetField(splash, "duracionEntrada", 0.02f);
                SetField(splash, "duracionEspera", 0.02f);
                SetField(splash, "duracionSalida", 0.02f);

                yield return WaitForLoaderStartAndScene("MainMenu", $"{tag}: Splash -> MainMenu");
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            var menu = FindComponent("Reloj.UI.MainMenuController");
            Assert.IsNotNull(menu, "Falta MainMenuController en MainMenu.");
            // Evita esperar 1s de fade de musica antes de empezar la carga
            SetField(menu, "musicFadeOutSeconds", 0f);

            var continueBtn = GetField<Button>(menu, "continueButton");
            Assert.IsNotNull(continueBtn, "Falta continueButton en MainMenuController.");
            if (runIndex == 1)
            {
                Assert.IsFalse(continueBtn.interactable, "En partida limpia Continuar debe estar deshabilitado.");
                Invoke(menu, "Play");
            }
            else
            {
                Assert.IsTrue(continueBtn.interactable, "Tras completar la Corrida 1, Continuar debe estar habilitado.");
                Invoke(menu, "Play");
                yield return null;
                var confirmPanel = GetField<GameObject>(menu, "newGameConfirmPanel");
                Assert.IsNotNull(confirmPanel, "Falta newGameConfirmPanel en MainMenuController.");
                Assert.IsTrue(confirmPanel.activeSelf, "Jugar con guardado previo debe abrir el panel de confirmacion.");
                var yesBtn = GetField<Button>(menu, "confirmYesButton");
                Assert.IsNotNull(yesBtn, "Falta confirmYesButton en MainMenuController.");
                yesBtn.onClick.Invoke();
            }

            // 2. MainMenu -> Nivel 1 (Gameplay + Art)
            yield return WaitForLevelLoaded(1, $"{tag}: MainMenu -> Nivel 1 (Gameplay+Art)");
            AssertState(expectedScore: 0, expectedAvailableSp: 0, expectedTimeStopLvl: 0);

            // Verifica que no quedaron flags/pistas de la corrida anterior
            Assert.IsFalse(GetFlag("candelaria_confio"), "candelaria_confio debe estar limpio al iniciar Nivel 1.");
            Assert.IsFalse(HasClue("n4_baul_torre"), "ClueInventory debe estar limpio al iniciar Nivel 1.");

            SetFlag("candelaria_confio", true);
            SetFlag("panfilo_mencionado", true);
            SetFlag("telegrama_dia20_leido", true);
            Assert.IsTrue(HasClue("telegrama_dia20"), "telegrama_dia20 debe entrar en ClueInventory en Nivel 1.");
            AddLevelScore(200);
            CompleteCurrentLevel();
            AssertGameState("FinDelNivel");
            AssertState(expectedScore: 200, expectedAvailableSp: 1, expectedTimeStopLvl: 0);

            if (runIndex == 1)
            {
                var stateUi = FindComponent("Reloj.UI.GameStateUI");
                Assert.IsNotNull(stateUi, "Falta GameStateUI en Nivel 1.");
                var skillPanel = GetField<Component>(stateUi, "skillPanel");
                Assert.IsNotNull(skillPanel, "Falta skillPanel en GameStateUI.");
                var backBtn = GetField<Button>(skillPanel, "backButton");
                Assert.IsNotNull(backBtn, "Falta backButton en SkillMenuPanel.");

                Invoke(stateUi, "OpenSkills");
                yield return null;
                Assert.IsTrue(skillPanel.gameObject.activeInHierarchy, "Panel_Habilidades debe abrirse al pulsar Habilidades.");
                Assert.IsTrue(backBtn.gameObject.activeInHierarchy, "Btn_Volver dentro de Panel_Habilidades debe estar activo y visible.");

                backBtn.onClick.Invoke();
                yield return null;
                Assert.IsFalse(skillPanel.gameObject.activeInHierarchy, "Panel_Habilidades debe cerrarse al pulsar Volver.");
            }

            // Gasta 1 punto de habilidad en Parada Temporal
            Assert.IsTrue(UpgradeSkill("TimeStopDuration"), "Debe poder mejorar TimeStopDuration en Nivel 1.");
            AssertState(expectedScore: 200, expectedAvailableSp: 0, expectedTimeStopLvl: 1);

            LoadNextFromGameState();

            // 3. Nivel 1 -> Nivel 2 (Gameplay + Art)
            yield return WaitForLevelLoaded(2, $"{tag}: Nivel 1 -> Nivel 2 (Gameplay+Art)");
            Assert.IsTrue(GetFlag("candelaria_confio"), "candelaria_confio debe persistir en Nivel 2.");
            Assert.IsTrue(GetFlag("panfilo_mencionado"), "panfilo_mencionado debe persistir en Nivel 2.");
            Assert.IsTrue(GetFlag("telegrama_dia20_leido"), "telegrama_dia20_leido debe persistir en Nivel 2.");
            Assert.IsTrue(HasClue("telegrama_dia20"), "Pista telegrama_dia20 debe persistir en Nivel 2.");
            AssertState(expectedScore: 200, expectedAvailableSp: 0, expectedTimeStopLvl: 1);

            SetFlag("n2_pista_libro_hacienda", true);
            Assert.IsTrue(HasClue("n2_libro_hacienda"), "n2_libro_hacienda debe entrar en ClueInventory en Nivel 2.");
            AddLevelScore(150);
            CompleteCurrentLevel();
            AssertGameState("FinDelNivel");
            AssertState(expectedScore: 350, expectedAvailableSp: 1, expectedTimeStopLvl: 1);
            LoadNextFromGameState();

            // 4. Nivel 2 -> Nivel 3 (Gameplay + Art)
            yield return WaitForLevelLoaded(3, $"{tag}: Nivel 2 -> Nivel 3 (Gameplay+Art)");
            Assert.IsTrue(GetFlag("candelaria_confio"), "candelaria_confio debe persistir en Nivel 3.");
            Assert.IsTrue(GetFlag("n2_pista_libro_hacienda"), "n2_pista_libro_hacienda debe persistir en Nivel 3.");
            Assert.IsTrue(GetFlag("rep_n3_alta"), "rep_n3_alta debe calcularse y estar activo en Nivel 3.");
            Assert.IsTrue(HasClue("telegrama_dia20") && HasClue("n2_libro_hacienda"), "Pistas previas deben persistir en Nivel 3.");
            AssertState(expectedScore: 350, expectedAvailableSp: 1, expectedTimeStopLvl: 1);

            SetFlag("n3_lupe_confia", true);
            SetFlag("n3_lupe_hablado", true);
            SetFlag("n3_pista_caja_lacre", true);
            Assert.IsTrue(HasClue("n3_caja_lacre"), "n3_caja_lacre debe entrar en ClueInventory en Nivel 3.");
            AddLevelScore(100);
            CompleteCurrentLevel();
            AssertGameState("FinDelNivel");
            AssertState(expectedScore: 450, expectedAvailableSp: 2, expectedTimeStopLvl: 1);
            LoadNextFromGameState();

            // 5. Nivel 3 -> Nivel 4 (Gameplay + Art)
            yield return WaitForLevelLoaded(4, $"{tag}: Nivel 3 -> Nivel 4 (Gameplay+Art)");
            Assert.IsTrue(GetFlag("rep_n3_alta") && GetFlag("n3_lupe_confia"), "Flags de Nivel 3 deben persistir en Nivel 4.");
            Assert.IsTrue(HasClue("telegrama_dia20") && HasClue("n2_libro_hacienda") && HasClue("n3_caja_lacre"),
                "Pistas de niveles 1-3 deben persistir en Nivel 4.");
            AssertState(expectedScore: 450, expectedAvailableSp: 2, expectedTimeStopLvl: 1);

            SetFlag("n4_pista_aviso_estacion", true);
            SetFlag("n4_pista_horario_317", true);
            SetFlag("n4_pista_baul_torre", true);
            Assert.IsTrue(HasClue("n4_aviso_estacion") && HasClue("n4_horario_317") && HasClue("n4_baul_torre"),
                "Todas las pistas de Nivel 4 deben registrarse en ClueInventory (GameService.prefab).");

            AddLevelScore(250);
            SetFlag("n4_encuentro_superado", true);

            var lvl4Ctrl = FindComponent("Reloj.Level.Level01Controller");
            Assert.IsNotNull(lvl4Ctrl, "Falta Level01Controller en Nivel 4.");
            SetField(lvl4Ctrl, "sceneAfterCompleteDelay", 0.05f);
            Invoke(lvl4Ctrl, "Complete");

            // 6. Nivel 4 -> Decision_Final
            yield return WaitForLoaderStartAndScene("Decision_Final", $"{tag}: Nivel 4 -> Decision_Final");
            AssertState(expectedScore: 700, expectedAvailableSp: 3, expectedTimeStopLvl: 1);
            Assert.IsTrue(GetFlag("candelaria_confio") && GetFlag("n3_lupe_confia") && GetFlag("n4_pista_baul_torre"),
                "Flags para los finales deben seguir activos en Decision_Final.");

            var decisionCtrl = FindComponent("Reloj.Endings.DecisionSceneController");
            Assert.IsNotNull(decisionCtrl, "Falta DecisionSceneController en Decision_Final.");
            SetField(decisionCtrl, "charsPerSecond", 5000f);

            var targetBtn = GetField<Button>(decisionCtrl, chooseEndingA ? "optionAButton" : "optionBButton");
            Assert.IsNotNull(targetBtn, "Falta boton de opcion en DecisionSceneController.");
            var optionsGroup = GetField<CanvasGroup>(decisionCtrl, "optionsGroup");
            yield return WaitUntil(() => optionsGroup == null || optionsGroup.interactable, "Opciones de decision no se volvieron interactuables.");
            targetBtn.onClick.Invoke();

            // Avanza WaitPress de la linea de cierre en Decision_Final
            yield return WaitAndTapThroughToScene(chooseEndingA ? "Ending_A" : "Ending_B",
                $"{tag}: Decision_Final -> {(chooseEndingA ? "Ending_A" : "Ending_B")}");

            // 7. Ending -> MainMenu
            string expectedEnding = chooseEndingA ? "Ending_A" : "Ending_B";
            Assert.IsTrue(GetFlag(chooseEndingA ? "n4_final_corregir" : "n4_final_proteger"),
                $"Flag del final '{expectedEnding}' debe estar activo.");
            Assert.IsTrue(GetStaticProp<bool>("Reloj.Core.SaveService", "GameCompleted"),
                "SaveService.GameCompleted debe ser true al llegar al final.");
            Assert.AreEqual(expectedEnding, GetStaticProp<string>("Reloj.Core.SaveService", "CompletedEndingId"));

            var endingCtrl = FindComponent("Reloj.Endings.EndingController");
            Assert.IsNotNull(endingCtrl, "Falta EndingController en escena de final.");
            SetField(endingCtrl, "introSeconds", 0.02f);
            SetField(endingCtrl, "charsPerSecond", 5000f);
            SetField(endingCtrl, "finalDelaySeconds", 0.02f);
            SetField(endingCtrl, "finalFadeSeconds", 0.05f);

            var menuBtn = GetField<Button>(endingCtrl, "menuButton");
            var finalGroup = GetField<CanvasGroup>(endingCtrl, "finalGroup");
            Assert.IsNotNull(menuBtn, "Falta menuButton en EndingController.");

            float deadline = Time.realtimeSinceStartup + 25f;
            while ((finalGroup == null || !finalGroup.interactable) && Time.realtimeSinceStartup < deadline)
            {
                yield return TapSpace();
                yield return null;
            }
            Assert.IsTrue(finalGroup == null || finalGroup.interactable, $"Final '{expectedEnding}' no llego al boton de volver al menu.");

            menuBtn.onClick.Invoke();
            yield return WaitForLoaderStartAndScene("MainMenu", $"{tag}: {expectedEnding} -> MainMenu");
        }

        private IEnumerator WaitForLevelLoaded(int levelNumber, string label)
        {
            string gameplay = $"Level{levelNumber:00}_Gameplay";
            string art = $"Level{levelNumber:00}_Art";

            var loader = GetLoader();
            float timeoutAt = Time.realtimeSinceStartup + StepTimeoutSeconds;

            while (!GetProp<bool>(loader, "IsLoading") && SceneManager.GetActiveScene().name != gameplay)
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout esperando inicio de carga de {gameplay}.");
                yield return null;
            }

            var sw = Stopwatch.StartNew();
            // Espera a que Gameplay cargue, luego 2 frames para que Level01Controller.Start() lance LoadAdditive(Art),
            // y a que Art quede cargado y Loader termine.
            while (SceneManager.GetActiveScene().name != gameplay || GetProp<bool>(loader, "IsLoading"))
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout cargando {gameplay}.");
                yield return null;
            }

            // Espera a que Level01Controller.Start() pida la escena Art (yield return null + IsLoading check)
            int frames = 0;
            while (!SceneManager.GetSceneByName(art).isLoaded && frames < 10)
            {
                frames++;
                yield return null;
            }
            while (!SceneManager.GetSceneByName(art).isLoaded || GetProp<bool>(loader, "IsLoading"))
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout cargando escena aditiva {art}.");
                yield return null;
            }
            sw.Stop();

            // Un frame extra para que los Start() de la escena corran y se detecte cualquier error inmediato
            yield return null;

            Assert.IsTrue(SceneManager.GetSceneByName(gameplay).isLoaded, $"{gameplay} debe estar cargada.");
            Assert.IsTrue(SceneManager.GetSceneByName(art).isLoaded, $"{art} debe estar cargada aditivamente.");
            RecordTiming(label, (float)sw.Elapsed.TotalSeconds);
        }

        private IEnumerator WaitForLoaderStartAndScene(string targetScene, string label)
        {
            var loader = GetLoader();
            float timeoutAt = Time.realtimeSinceStartup + StepTimeoutSeconds;

            while (!GetProp<bool>(loader, "IsLoading") && SceneManager.GetActiveScene().name != targetScene)
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout esperando transicion hacia '{targetScene}'.");
                yield return null;
            }

            var sw = Stopwatch.StartNew();
            while (SceneManager.GetActiveScene().name != targetScene || GetProp<bool>(loader, "IsLoading"))
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout cargando '{targetScene}'.");
                yield return null;
            }
            sw.Stop();
            yield return null;
            RecordTiming(label, (float)sw.Elapsed.TotalSeconds);
        }

        private IEnumerator WaitAndTapThroughToScene(string targetScene, string label)
        {
            var loader = GetLoader();
            float timeoutAt = Time.realtimeSinceStartup + StepTimeoutSeconds;

            while (!GetProp<bool>(loader, "IsLoading") && SceneManager.GetActiveScene().name != targetScene)
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout esperando carga de '{targetScene}' tras elegir opcion.");
                yield return TapSpace();
                yield return null;
            }

            var sw = Stopwatch.StartNew();
            while (SceneManager.GetActiveScene().name != targetScene || GetProp<bool>(loader, "IsLoading"))
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout cargando '{targetScene}'.");
                yield return null;
            }
            sw.Stop();
            yield return null;
            RecordTiming(label, (float)sw.Elapsed.TotalSeconds);
        }

        private IEnumerator WaitForSceneActive(string sceneName)
        {
            float timeoutAt = Time.realtimeSinceStartup + StepTimeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneName)
            {
                if (Time.realtimeSinceStartup > timeoutAt) Assert.Fail($"Timeout esperando escena '{sceneName}'.");
                yield return null;
            }
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

        private IEnumerator TapSpace()
        {
            if (_virtualKeyboard == null) yield break;
            if (!_virtualKeyboard.enabled) InputSystem.EnableDevice(_virtualKeyboard);
            _virtualKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_virtualKeyboard, new KeyboardState(Key.Space));
            yield return null;
            InputSystem.QueueStateEvent(_virtualKeyboard, new KeyboardState());
        }

        private void RecordTiming(string label, float seconds)
        {
            _loadTimings.Add((label, seconds));
        }

        // ------------------------------------------------------------------ Helpers de dominio

        private object GetServices() => GetStaticProp<object>("Reloj.Core.GameServices", "Instance");
        private object GetLoader() => GetProp<object>(GetServices(), "Loader");
        private object GetSession() => GetProp<object>(GetServices(), "Session");
        private object GetState() => GetProp<object>(GetServices(), "State");
        private object GetClues() => GetProp<object>(GetServices(), "Clues");
        private object GetSkillPoints() => GetProp<object>(GetServices(), "SkillPoints");

        private void SetFlag(string key, bool value) =>
            InvokeStatic("Reloj.Core.DialogueFlagManager", "SetFlag", key, value);

        private bool GetFlag(string key) =>
            (bool)InvokeStatic("Reloj.Core.DialogueFlagManager", "GetFlag", key);

        private bool HasClue(string clueId) =>
            (bool)Invoke(GetClues(), "Has", clueId);

        private void AddLevelScore(int points)
        {
            var mgr = FindComponent("Reloj.Player.ScoreManager");
            Assert.IsNotNull(mgr, "Falta ScoreManager en la escena de nivel.");
            Invoke(mgr, "Add", points);
        }

        private void CompleteCurrentLevel()
        {
            var ctrl = FindComponent("Reloj.Level.Level01Controller");
            Assert.IsNotNull(ctrl, "Falta Level01Controller en la escena de nivel.");
            Invoke(ctrl, "Complete");
        }

        private void LoadNextFromGameState() => Invoke(GetState(), "LoadNext");

        private bool UpgradeSkill(string categoryName)
        {
            var catType = FindType("Reloj.Progression.SkillCategory");
            var cat = Enum.Parse(catType, categoryName);
            return (bool)Invoke(GetSkillPoints(), "TryUpgrade", cat);
        }

        private void AssertGameState(string expectedStateName)
        {
            var current = GetProp<object>(GetState(), "CurrentState");
            Assert.AreEqual(expectedStateName, current.ToString());
        }

        private void AssertState(int expectedScore, int expectedAvailableSp, int expectedTimeStopLvl)
        {
            int actualScore = GetProp<int>(GetSession(), "Score");
            int actualSp = GetProp<int>(GetSkillPoints(), "Available");
            var catType = FindType("Reloj.Progression.SkillCategory");
            var cat = Enum.Parse(catType, "TimeStopDuration");
            int actualLvl = (int)Invoke(GetSkillPoints(), "GetLevel", cat);

            Assert.AreEqual(expectedScore, actualScore, "Puntaje acumulado en GameSession no coincide.");
            Assert.AreEqual(expectedAvailableSp, actualSp, "Puntos de habilidad disponibles no coinciden.");
            Assert.AreEqual(expectedTimeStopLvl, actualLvl, "Nivel de mejora TimeStopDuration no coincide.");
        }

        // ------------------------------------------------------------------ Reflexion

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

        private static object InvokeStatic(string typeName, string methodName, params object[] args)
        {
            var t = FindType(typeName);
            var m = FindMethod(t, methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, args);
            return m.Invoke(null, args);
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
            var m = FindMethod(target.GetType(), methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, args);
            return m.Invoke(target, args);
        }

        private static MethodInfo FindMethod(Type t, string name, BindingFlags flags, object[] args)
        {
            foreach (var m in t.GetMethods(flags))
            {
                if (m.Name != name) continue;
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
                if (match) return m;
            }
            Assert.Fail($"Metodo '{t.Name}.{name}' con {args.Length} arg(s) no encontrado.");
            return null;
        }
    }
}

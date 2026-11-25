using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloj.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public float LastLoadDurationSeconds { get; private set; }
        public string LastLoadedScene { get; private set; }
        public event Action<string> SceneLoaded;
        public event Action<string> SceneUnloaded;

        public void LoadSingle(int buildIndex) => StartCoroutine(LoadSingleRoutine(buildIndex));
        public void LoadSingle(string sceneName) => StartCoroutine(LoadSingleRoutine(sceneName));

        public void LoadAdditive(string sceneName, bool setActive = false) =>
            StartCoroutine(LoadAdditiveRoutine(sceneName, setActive));

        public void Unload(string sceneName) => StartCoroutine(UnloadRoutine(sceneName));

        // Recarga el nivel actual: la escena activa y las aditivas que tenga cargadas.
        public void ReloadCurrent() => StartCoroutine(ReloadRoutine());

        // Conveniencia: LevelNN_Gameplay + LevelNN_Art
        public IEnumerator LoadLevel(int n)
        {
            string id = $"Level{n:00}";
            yield return LoadAdditiveRoutine($"{id}_Gameplay", true);
            yield return LoadAdditiveRoutine($"{id}_Art", false);
        }

        private IEnumerator LoadSingleRoutine(int buildIndex)
        {
            IsLoading = true;
            float start = Time.realtimeSinceStartup;
            var op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            if (op == null) { IsLoading = false; yield break; }
            while (!op.isDone) yield return null;
            string loadedName = SceneManager.GetActiveScene().name;
            LastLoadDurationSeconds = Time.realtimeSinceStartup - start;
            LastLoadedScene = loadedName;
            IsLoading = false;
            SceneLoaded?.Invoke(loadedName);
        }

        private IEnumerator LoadSingleRoutine(string sceneName)
        {
            IsLoading = true;
            float start = Time.realtimeSinceStartup;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null) { IsLoading = false; yield break; }
            while (!op.isDone) yield return null;
            LastLoadDurationSeconds = Time.realtimeSinceStartup - start;
            LastLoadedScene = sceneName;
            IsLoading = false;
            SceneLoaded?.Invoke(sceneName);
        }

        private IEnumerator ReloadRoutine()
        {
            IsLoading = true;
            float start = Time.realtimeSinceStartup;
            string activeName = SceneManager.GetActiveScene().name;

            var others = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string n = SceneManager.GetSceneAt(i).name;
                if (n != activeName) others.Add(n);
            }

            var op = SceneManager.LoadSceneAsync(activeName, LoadSceneMode.Single);
            if (op == null) { IsLoading = false; yield break; }
            while (!op.isDone) yield return null;

            foreach (var n in others)
                yield return LoadAdditiveRoutine(n, false);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(activeName));
            LastLoadDurationSeconds = Time.realtimeSinceStartup - start;
            LastLoadedScene = activeName;
            IsLoading = false;
        }

        private IEnumerator LoadAdditiveRoutine(string sceneName, bool setActive)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded) yield break;
            IsLoading = true;
            float start = Time.realtimeSinceStartup;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null) { IsLoading = false; yield break; }
            while (!op.isDone) yield return null;
            if (setActive) SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            LastLoadDurationSeconds = Time.realtimeSinceStartup - start;
            LastLoadedScene = sceneName;
            IsLoading = false;
            SceneLoaded?.Invoke(sceneName);
        }

        private IEnumerator UnloadRoutine(string sceneName)
        {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded) yield break;
            var op = SceneManager.UnloadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            SceneUnloaded?.Invoke(sceneName);
        }
    }
}
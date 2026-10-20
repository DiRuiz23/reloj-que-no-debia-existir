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
            var op = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            IsLoading = false;
        }

        private IEnumerator LoadSingleRoutine(string sceneName)
        {
            IsLoading = true;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            IsLoading = false;
        }

        private IEnumerator ReloadRoutine()
        {
            IsLoading = true;
            string activeName = SceneManager.GetActiveScene().name;

            var others = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string n = SceneManager.GetSceneAt(i).name;
                if (n != activeName) others.Add(n);
            }

            var op = SceneManager.LoadSceneAsync(activeName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            foreach (var n in others)
                yield return LoadAdditiveRoutine(n, false);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(activeName));
            IsLoading = false;
        }

        private IEnumerator LoadAdditiveRoutine(string sceneName, bool setActive)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded) yield break;
            IsLoading = true;
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            if (setActive) SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
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
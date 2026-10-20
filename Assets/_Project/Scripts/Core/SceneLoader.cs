using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloj.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public event Action<string> SceneLoaded;
        public event Action<string> SceneUnloaded;

        // Carga normal (reemplaza todo menos GameServices, que es DontDestroyOnLoad)
        public void LoadSingle(int buildIndex) => StartCoroutine(LoadSingleRoutine(buildIndex));

        // Aditiva
        public void LoadAdditive(string sceneName, bool setActive = false) =>
            StartCoroutine(LoadAdditiveRoutine(sceneName, setActive));

        public void Unload(string sceneName) => StartCoroutine(UnloadRoutine(sceneName));

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
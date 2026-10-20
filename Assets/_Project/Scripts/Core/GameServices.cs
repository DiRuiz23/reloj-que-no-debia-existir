using UnityEngine;

namespace Reloj.Core
{
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        [SerializeField] private DialogueFlagManager flags;
        [SerializeField] private GameSession session;
        [SerializeField] private SaveService save;
        [SerializeField] private SceneLoader sceneLoader;

        public DialogueFlagManager Flags => flags;
        public GameSession Session => session;
        public SaveService Save => save;
        public SceneLoader Loader => sceneLoader;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);   // duplicado al volver a Boot
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
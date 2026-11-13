using UnityEngine;
using Reloj.Clues;

namespace Reloj.Core
{
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        [SerializeField] private DialogueFlagManager flags;
        [SerializeField] private GameSession session;
        [SerializeField] private SaveService save;
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private GameStateManager state;
        [SerializeField] private ClueInventory clues;

        public DialogueFlagManager Flags => flags;
        public GameSession Session => session;
        public SaveService Save => save;
        public SceneLoader Loader => sceneLoader;
        public GameStateManager State => state;
        public ClueInventory Clues => clues;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);   // duplicado al volver a Boot
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (clues == null) clues = GetComponent<ClueInventory>();   // I-27: no exige cablearlo a mano
        }
    }
}

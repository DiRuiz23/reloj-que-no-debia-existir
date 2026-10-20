using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloj.UI
{
    // PROVISIONAL: se reemplaza por el menú real.
    public class MainMenuPlaceholder : MonoBehaviour
    {
        public void Play() => SceneManager.LoadScene(1);   // índice 1 = Test_Sandbox
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloj.Core
{
    // TEMPORAL (I-02): carga la siguiente escena del build.
    // Se elimina cuando I-10 (Boot + SceneLoader) esté integrado.
    public class BootTemporal : MonoBehaviour
    {
        private void Start()
        {
            SceneManager.LoadScene(1); // índice 1 = Test_Sandbox
        }
    }
}
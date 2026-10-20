using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloj.Core
{
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            GameServices.Instance.Loader.LoadSingle(next);
        }
    }
}
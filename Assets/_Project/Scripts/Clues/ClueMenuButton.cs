using UnityEngine;
using UnityEngine.UI;

namespace Reloj.Clues
{
    /// <summary>
    /// Boton "Pistas" del menu de pausa (I-27): abre el <see cref="ClueMenuUI"/>.
    /// Se oculta solo si la escena no tiene menu de pistas (p. ej. una escena de prueba sin HUD).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ClueMenuButton : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Open);
        }

        void OnEnable()
        {
            if (ClueMenuUI.Instance == null) gameObject.SetActive(false);
        }

        static void Open()
        {
            if (ClueMenuUI.Instance != null) ClueMenuUI.Instance.Open();
        }
    }
}

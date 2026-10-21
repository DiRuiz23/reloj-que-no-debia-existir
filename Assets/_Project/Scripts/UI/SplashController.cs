
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashController : MonoBehaviour
{
    [Header("Grupos de transparencia")]
    [SerializeField] private CanvasGroup contenido;
    [SerializeField] private CanvasGroup splashCompleto;

    [Header("Duraciones")]
    [SerializeField] private float duracionEntrada = 1.5f;
    [SerializeField] private float duracionEspera = 2f;
    [SerializeField] private float duracionSalida = 1.5f;

    [Header("Escena siguiente")]
    [SerializeField] private string siguienteEscena = "MainMenu";

    private void Start()
    {
        contenido.alpha = 0f;
        splashCompleto.alpha = 1f;

        StartCoroutine(AnimarSplash());
    }

    private IEnumerator AnimarSplash()
    {
        // Aparecen el título y el logo.
        yield return Desvanecer(
            contenido, 0f, 1f, duracionEntrada
        );

        // Mantiene visible el Splash.
        yield return new WaitForSeconds(duracionEspera);

        // Desaparece toda la pantalla.
        yield return Desvanecer(
            splashCompleto, 1f, 0f, duracionSalida
        );

        SceneManager.LoadScene(siguienteEscena);
    }

    private IEnumerator Desvanecer(
        CanvasGroup grupo,
        float inicio,
        float fin,
        float duracion)
    {
        grupo.alpha = inicio;

        if (duracion <= 0f)
        {
            grupo.alpha = fin;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(
                inicio, fin, tiempo / duracion
            );

            yield return null;
        }

        grupo.alpha = fin;
    }
}

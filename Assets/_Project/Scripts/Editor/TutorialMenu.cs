#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Reloj.Tutorial;

namespace Reloj.EditorTools
{
    public static class TutorialMenu
    {
        [MenuItem("Reloj/Tutorial/Reiniciar progreso del tutorial")]
        static void ResetProgress()
        {
            TutorialProgress.ResetAll();
            Debug.Log("[Tutorial] Progreso borrado: el Nivel 1 volvera a mostrar las indicaciones.");
        }

        [MenuItem("Reloj/Tutorial/Marcar tutorial como completado")]
        static void MarkCompleted()
        {
            TutorialProgress.MarkCompleted();
            Debug.Log("[Tutorial] tutorial_completado = true: el Nivel 1 ya no mostrara las indicaciones.");
        }
    }
}
#endif

using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Configuracion de arranque especifica para Android:
    /// refuerza orientacion horizontal fija, evita apagado de pantalla y desactiva salida directa con Atrás.
    /// </summary>
    public static class AndroidRuntimeConfig
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if ENABLE_LEGACY_INPUT_MANAGER
            Input.backButtonLeavesApp = false;
#endif
#endif
        }
    }
}

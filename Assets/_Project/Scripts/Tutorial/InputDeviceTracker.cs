using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Reloj.Tutorial
{
    public enum InputScheme { Keyboard, Gamepad, Touch }

    /// <summary>
    /// Recuerda con que dispositivo esta jugando la persona (el ultimo que uso de verdad), para que los
    /// textos de ayuda muestren la tecla, el boton o el gesto correcto. No necesita estar en la escena:
    /// se engancha solo al Input System al arrancar.
    /// </summary>
    public static class InputDeviceTracker
    {
        // Un stick en reposo puede vibrar un poco: solo cuenta si se mueve claramente.
        const float StickThreshold = 0.3f;

        static bool hooked;

        public static InputScheme Current { get; private set; } = DefaultScheme();

        /// <summary>Se dispara cuando la persona cambia de dispositivo (teclado, gamepad, tactil).</summary>
        public static event Action<InputScheme> Changed;

        static InputScheme DefaultScheme() =>
            Application.isMobilePlatform ? InputScheme.Touch : InputScheme.Keyboard;

        // Evita estado viejo si el Editor tiene desactivado "Reload Domain".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Changed = null;
            hooked = false;
            Current = DefaultScheme();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (hooked) return;
            hooked = true;
            InputSystem.onEvent -= OnEvent;
            InputSystem.onEvent += OnEvent;

            // Si ya hay un gamepad conectado y no es una plataforma tactil, no lo damos por activo
            // hasta que lo toquen: el teclado sigue siendo el valor por defecto en PC.
        }

        /// <summary>Fuerza un esquema (pruebas, o el boton tactil de I-37).</summary>
        public static void Set(InputScheme scheme)
        {
            if (scheme == Current) return;
            Current = scheme;
            Changed?.Invoke(scheme);
        }

        static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null) return;
            var type = eventPtr.type;
            if (type != StateEvent.Type && type != DeltaStateEvent.Type) return;

            if (device is Keyboard)
            {
                if (eventPtr.HasButtonPress()) Set(InputScheme.Keyboard);
            }
            else if (device is Gamepad pad)
            {
                bool used = eventPtr.HasButtonPress();
                if (!used && pad.leftStick.ReadValueFromEvent(eventPtr, out var stick))
                    used = stick.magnitude > StickThreshold;
                if (used) Set(InputScheme.Gamepad);
            }
            else if (device is Touchscreen)
            {
                Set(InputScheme.Touch);
            }
            // El mouse se ignora: el juego no tiene acciones de mouse y solo generaria falsos cambios.
        }
    }
}

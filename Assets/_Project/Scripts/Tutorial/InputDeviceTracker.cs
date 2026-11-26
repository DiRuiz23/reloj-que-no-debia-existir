using System;
using System.Linq;
using Reloj.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

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
        static readonly InternedString OnScreenUsage = new InternedString("OnScreen");

        static bool hooked;

        public static InputScheme Current { get; private set; } = DefaultScheme();

        /// <summary>Se dispara cuando la persona cambia de dispositivo (teclado, gamepad, tactil).</summary>
        public static event Action<InputScheme> Changed;

        static InputScheme DefaultScheme() =>
            UnityEngine.Device.Application.isMobilePlatform
            || UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld
                ? InputScheme.Touch
                : InputScheme.Keyboard;

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
            Current = DefaultScheme();
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

            if (device is TouchControlsDevice || device.usages.Contains(OnScreenUsage))
            {
                Set(InputScheme.Touch);
                return;
            }

            if (device is Keyboard kb)
            {
                if (eventPtr.HasButtonPress())
                {
                    // En Android el botón/gesto "Atrás" entra como Escape del teclado virtual:
                    // si el jugador está usando controles táctiles, no se debe ocultar la UI táctil.
                    if (IsSystemBackOnly(eventPtr, kb)) return;
                    Set(InputScheme.Keyboard);
                }
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

        static bool IsSystemBackOnly(InputEventPtr eventPtr, Keyboard kb)
        {
            if (Current != InputScheme.Touch || kb == null) return false;

            bool escapePressed = false;
            foreach (var control in eventPtr.EnumerateChangedControls(kb, 0.5f))
            {
                if (control == kb.escapeKey)
                {
                    escapePressed = true;
                    continue;
                }
                if (control is KeyControl) return false;
            }
            return escapePressed;
        }
    }
}

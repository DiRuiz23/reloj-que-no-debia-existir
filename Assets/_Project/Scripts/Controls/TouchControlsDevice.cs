using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace Reloj.Controls
{
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct TouchControlsState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC('T', 'C', 'T', 'L');

        [InputControl(name = "stick", layout = "Stick", format = "VEC2", displayName = "Joystick")]
        [FieldOffset(0)]
        public Vector2 stick;

        [InputControl(name = "jump", layout = "Button", format = "FLT", displayName = "Saltar")]
        [FieldOffset(8)]
        public float jump;

        [InputControl(name = "interact", layout = "Button", format = "FLT", displayName = "Interactuar")]
        [FieldOffset(12)]
        public float interact;

        [InputControl(name = "timestop", layout = "Button", format = "FLT", displayName = "Parada Temporal")]
        [FieldOffset(16)]
        public float timestop;

        [InputControl(name = "pause", layout = "Button", format = "FLT", displayName = "Pausa")]
        [FieldOffset(20)]
        public float pause;
    }

    /// <summary>
    /// Dispositivo virtual dedicado para los controles tactiles en pantalla (OnScreenStick / OnScreenButton).
    /// Evita usar Gamepad virtual para no contaminar Gamepad.current ni interferir con rebindings.
    /// </summary>
    [InputControlLayout(displayName = "Controles táctiles", stateType = typeof(TouchControlsState))]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class TouchControlsDevice : InputDevice
    {
        public StickControl stick { get; private set; }
        public ButtonControl jump { get; private set; }
        public ButtonControl interact { get; private set; }
        public ButtonControl timestop { get; private set; }
        public ButtonControl pause { get; private set; }

        static TouchControlsDevice()
        {
            Register();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            InputSystem.RegisterLayout<TouchControlsDevice>();
        }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            stick = GetChildControl<StickControl>("stick");
            jump = GetChildControl<ButtonControl>("jump");
            interact = GetChildControl<ButtonControl>("interact");
            timestop = GetChildControl<ButtonControl>("timestop");
            pause = GetChildControl<ButtonControl>("pause");
        }
    }
}

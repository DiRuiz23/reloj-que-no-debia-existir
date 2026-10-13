using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloj.Controls
{
    /// <summary>
    /// Herramienta de prueba (I-03): muestra en pantalla cuándo se activa
    /// cada acción y con qué dispositivo (teclado o gamepad).
    /// </summary>
    public class InputDebugDisplay : MonoBehaviour
    {
        private const float FlashSeconds = 0.5f;

        private PlayerControls _controls;
        private Vector2 _move;
        private string _device = "-";

        private float _jumpTime = -10f;
        private float _interactTime = -10f;
        private float _timeStopTime = -10f;
        private float _pauseTime = -10f;

        private GUIStyle _style;

        private void Awake()
        {
            _controls = new PlayerControls();
        }

        private void OnEnable()
        {
            var gameplay = _controls.Gameplay;
            gameplay.Move.performed += OnMove;
            gameplay.Move.canceled += OnMove;
            gameplay.Jump.performed += OnJump;
            gameplay.Interact.performed += OnInteract;
            gameplay.Timestop.performed += OnTimeStop;
            gameplay.Pause.performed += OnPause;
            gameplay.Enable();
        }

        private void OnDisable()
        {
            var gameplay = _controls.Gameplay;
            gameplay.Disable();
            gameplay.Move.performed -= OnMove;
            gameplay.Move.canceled -= OnMove;
            gameplay.Jump.performed -= OnJump;
            gameplay.Interact.performed -= OnInteract;
            gameplay.Timestop.performed -= OnTimeStop;
            gameplay.Pause.performed -= OnPause;
        }

        private void OnDestroy()
        {
            _controls.Dispose();
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            _move = ctx.ReadValue<Vector2>();
            if (ctx.performed) _device = ctx.control.device.displayName;
        }

        private void OnJump(InputAction.CallbackContext ctx) { _jumpTime = Time.unscaledTime; _device = ctx.control.device.displayName; }
        private void OnInteract(InputAction.CallbackContext ctx) { _interactTime = Time.unscaledTime; _device = ctx.control.device.displayName; }
        private void OnTimeStop(InputAction.CallbackContext ctx) { _timeStopTime = Time.unscaledTime; _device = ctx.control.device.displayName; }
        private void OnPause(InputAction.CallbackContext ctx) { _pauseTime = Time.unscaledTime; _device = ctx.control.device.displayName; }

        private static string State(float lastTime)
        {
            return Time.unscaledTime - lastTime < FlashSeconds ? "ACTIVADA" : "-";
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
            }

            GUILayout.BeginArea(new Rect(20, 20, 600, 300));
            GUILayout.Label("Dispositivo: " + _device, _style);
            GUILayout.Label("Move: (" + _move.x.ToString("0.00") + ", " + _move.y.ToString("0.00") + ")", _style);
            GUILayout.Label("Jump: " + State(_jumpTime), _style);
            GUILayout.Label("Interact: " + State(_interactTime), _style);
            GUILayout.Label("TimeStop: " + State(_timeStopTime), _style);
            GUILayout.Label("Pause: " + State(_pauseTime), _style);
            GUILayout.EndArea();
        }
    }
}
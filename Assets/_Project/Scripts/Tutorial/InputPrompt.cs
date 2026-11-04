using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloj.Tutorial
{
    /// <summary>
    /// Convierte una InputAction en el texto de su binding real para el dispositivo activo, usando
    /// <c>InputAction.GetBindingDisplayString</c>. Si el jugador remapea, el texto lo sigue.
    /// </summary>
    public static class InputPrompt
    {
        /// <summary>Arena de la paleta I-09 (UIPalette.Arena): resalta la tecla sobre el pergamino del banner.</summary>
        public const string KeyColorHex = "#C9A878";

        // Nombres genericos del Input System -> como los diria una persona (es).
        // Con un mando real el Input System ya devuelve "A", "Cruz", etc.; esto cubre el mando generico.
        static readonly Dictionary<string, string> Spanish = new Dictionary<string, string>
        {
            { "Space", "Espacio" },
            { "Escape", "Esc" },
            { "Left Stick", "Stick izquierdo" },
            { "Right Stick", "Stick derecho" },
            { "D-Pad", "Cruceta" },
            { "Button South", "A" },
            { "Button East", "B" },
            { "Button West", "X" },
            { "Button North", "Y" },
            { "Left Shoulder", "LB" },
            { "Right Shoulder", "RB" },
            { "Left Trigger", "LT" },
            { "Right Trigger", "RT" },
            { "Start", "Start" },
        };

        /// <summary>Grupo de bindings que corresponde a cada esquema (empieza por: "Keyboard" cubre "Keyboard&amp;Mouse").</summary>
        public static string GroupOf(InputScheme scheme)
        {
            switch (scheme)
            {
                case InputScheme.Gamepad: return "Gamepad";
                case InputScheme.Touch: return "Touch";
                default: return "Keyboard";
            }
        }

        /// <summary>
        /// Texto de la tecla/boton de <paramref name="action"/> para <paramref name="scheme"/>.
        /// Ej.: "[E]", "[W A S D]", "[Stick izquierdo]". Si la accion no tiene binding para ese
        /// dispositivo (hoy: tactil, hasta I-37) devuelve <paramref name="fallback"/> tal cual.
        /// </summary>
        public static string Label(InputAction action, InputScheme scheme, string fallback = null,
                                   bool brackets = true, bool highlight = true)
        {
            string raw = action != null ? Resolve(action, GroupOf(scheme)) : null;

            if (!string.IsNullOrEmpty(raw))
            {
                if (brackets) raw = "[" + raw + "]";
                return highlight ? "<color=" + KeyColorHex + "><b>" + raw + "</b></color>" : raw;
            }

            if (!string.IsNullOrEmpty(fallback)) return fallback;

            // Ultimo recurso: lo que tenga la accion, sin filtrar por dispositivo.
            string any = action != null ? action.GetBindingDisplayString().Replace(" | ", " / ") : "?";
            return brackets ? "[" + any + "]" : any;
        }

        static string Resolve(InputAction action, string group)
        {
            var bindings = action.bindings;
            string up = null, down = null, left = null, right = null;
            var singles = new List<string>();

            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || !InGroup(b.groups, group)) continue;

                string s = Localize(action.GetBindingDisplayString(
                    i, InputBinding.DisplayStringOptions.DontIncludeInteractions));
                if (string.IsNullOrEmpty(s)) continue;

                if (b.isPartOfComposite)
                {
                    // Un 2D Vector tiene varias teclas por direccion (WASD y flechas): mostramos la primera de cada una.
                    switch (b.name)
                    {
                        case "up": if (up == null) up = s; break;
                        case "down": if (down == null) down = s; break;
                        case "left": if (left == null) left = s; break;
                        case "right": if (right == null) right = s; break;
                    }
                }
                else if (!singles.Contains(s))
                {
                    singles.Add(s);
                }
            }

            if (up != null && down != null && left != null && right != null)
                return up + " " + left + " " + down + " " + right;   // "W A S D"

            if (singles.Count > 0) return string.Join(" / ", singles);
            return null;
        }

        static bool InGroup(string groups, string group)
        {
            if (string.IsNullOrEmpty(groups)) return true;   // binding sin grupo: vale para todos
            foreach (var g in groups.Split(';'))
                if (g.Length > 0 && g.StartsWith(group, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static string Localize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return Spanish.TryGetValue(s, out var es) ? es : s;
        }
    }
}

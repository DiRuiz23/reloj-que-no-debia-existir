using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    public class DialogueFlagManager : MonoBehaviour
    {
        private readonly Dictionary<string, bool> _flags = new();

        public void SetFlag(string key, bool value = true) => _flags[key] = value;
        public bool GetFlag(string key) => _flags.TryGetValue(key, out var v) && v;
        public bool HasFlag(string key) => _flags.ContainsKey(key);
        public void Clear() => _flags.Clear();
    }
}
using UnityEngine;

namespace Reloj.Core
{
    // STUB (I-34): la persistencia real se implementa después.
    public class SaveService : MonoBehaviour
    {
        public void Save() => Debug.Log("[SaveService] Save() - stub");
        public bool Load() { Debug.Log("[SaveService] Load() - stub"); return false; }
    }
}
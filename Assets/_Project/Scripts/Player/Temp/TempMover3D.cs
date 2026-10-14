using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORAL: se elimina en I-04 cuando exista el control real del personaje.
public class TempMover3D : MonoBehaviour
{
    Rigidbody rb;
    void Awake() { rb = GetComponent<Rigidbody>(); }

    void FixedUpdate()
    {
        var k = Keyboard.current;
        if (k == null) return;
        Vector3 d = new Vector3(
            (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
            0f,
            (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
        d = d.normalized * 5f;
        rb.linearVelocity = new Vector3(d.x, rb.linearVelocity.y, d.z);
    }
}
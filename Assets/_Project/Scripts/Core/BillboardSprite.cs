using UnityEngine;

namespace Reloj.Core
{
    /// <summary>Mantiene el sprite vertical y de frente a la camara (solo gira en Y).</summary>
    public class BillboardSprite : MonoBehaviour
    {
        Transform camTransform;
        float lastYaw = float.NaN;

        void LateUpdate()
        {
            if (camTransform == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                camTransform = cam.transform;
            }
            float yaw = camTransform.eulerAngles.y;
            if (Mathf.Abs(yaw - lastYaw) > 0.01f)
            {
                lastYaw = yaw;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }
    }
}
using UnityEngine;

namespace Reloj.Core
{
    /// <summary>Mantiene el sprite vertical y de frente a la camara (solo gira en Y).</summary>
    public class BillboardSprite : MonoBehaviour
    {
        Camera cam;

        void LateUpdate()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }
            transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        }
    }
}
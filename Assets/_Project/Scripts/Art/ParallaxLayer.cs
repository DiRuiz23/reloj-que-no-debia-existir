using UnityEngine;

namespace Reloj.Art
{
    /// <summary>
    /// Parallax horizontal para una capa de fondo de Level01_Art.
    /// factor = 0 -> la capa acompana a la camara (infinitamente lejos);
    /// factor = 1 -> la capa queda fija en el mundo (como el plano jugable).
    /// La camara vive en Level01_Gameplay, asi que se busca de forma diferida.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("0 = sigue a la camara, 1 = fija en el mundo. Lejano ~0.85, medio ~0.6.")]
        [SerializeField] float factor = 0.6f;

        Transform cam;
        float startX;
        float camStartX;
        bool initialized;

        void Start() => startX = transform.position.x;

        void LateUpdate()
        {
            if (cam == null)
            {
                var c = Camera.main;
                if (c == null) return;
                cam = c.transform;
                camStartX = cam.position.x;
                initialized = true;
            }
            if (!initialized) return;

            float dx = cam.position.x - camStartX;
            var p = transform.position;
            p.x = startX + dx * (1f - factor);
            transform.position = p;
        }
    }
}

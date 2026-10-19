using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Reloj.UI
{
    public enum GraphicsQuality { Alto, Bajo }

    [RequireComponent(typeof(Volume))]
    public class VolumeProfileSwitcher : MonoBehaviour
    {
        [SerializeField] VolumeProfile perfilAlto;
        [SerializeField] VolumeProfile perfilBajo;
        [SerializeField] GraphicsQuality calidadInicial = GraphicsQuality.Alto;

        [Header("Solo escena de prueba")]
        [SerializeField] bool alternarConF2 = true;

        Volume volume;

        public GraphicsQuality Actual { get; private set; }

        void Awake()
        {
            volume = GetComponent<Volume>();
            Apply(calidadInicial);
        }

        void Update()
        {
            if (!alternarConF2 || Keyboard.current == null) return;
            if (Keyboard.current.f2Key.wasPressedThisFrame)
                Apply(Actual == GraphicsQuality.Alto ? GraphicsQuality.Bajo : GraphicsQuality.Alto);
        }

        /// <summary>Lo usara I-36 (calidad grafica) para cambiar entre HD2D_Alto y HD2D_Bajo.</summary>
        public void Apply(GraphicsQuality q)
        {
            Actual = q;
            volume.sharedProfile = q == GraphicsQuality.Alto ? perfilAlto : perfilBajo;
        }
    }
}

using UnityEngine;

namespace Reloj.UI
{
    [CreateAssetMenu(fileName = "UIPalette", menuName = "Reloj/UI/UIPalette")]
    public class UIPalette : ScriptableObject
    {
        [Header("Tonos base (hex) - sepia / tierra")]
        [SerializeField] string tinta = "#2B1D14";
        [SerializeField] string tierraOscura = "#4E3626";
        [SerializeField] string sepiaMedio = "#8B6A47";
        [SerializeField] string arena = "#C9A878";
        [SerializeField] string pergamino = "#EAD9B5";

        [Header("Acento 'fuera de tiempo' (hex)")]
        [Tooltip("SOLO para el reloj y los efectos de la Parada Temporal. Nunca en UI normal.")]
        [SerializeField] string acento = "#FFA62B";

        public Color Tinta => Parse(tinta);
        public Color TierraOscura => Parse(tierraOscura);
        public Color SepiaMedio => Parse(sepiaMedio);
        public Color Arena => Parse(arena);
        public Color Pergamino => Parse(pergamino);
        public Color Acento => Parse(acento);

        /// <summary>Acento en HDR para materiales emisivos: el bloom solo se activa por encima del umbral.</summary>
        public Color AcentoHDR(float intensidad = 2f)
        {
            Color c = Acento;
            float f = Mathf.Pow(2f, intensidad);
            return new Color(c.r * f, c.g * f, c.b * f, 1f);
        }

        static Color Parse(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        void OnValidate()
        {
            Check(tinta); Check(tierraOscura); Check(sepiaMedio);
            Check(arena); Check(pergamino); Check(acento);
        }

        void Check(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out _))
                Debug.LogWarning($"UIPalette: hex invalido '{hex}'", this);
        }
    }
}
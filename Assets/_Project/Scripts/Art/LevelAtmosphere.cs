using UnityEngine;
using UnityEngine.Rendering;
using Reloj.UI;

namespace Reloj.Art
{
    /// <summary>
    /// Aplica la atmosfera ambiental de Level01_Art (luz ambiente + niebla) usando UIPalette.
    /// Necesario porque, al cargarse de forma aditiva, Unity usa los Lighting Settings de la
    /// escena ACTIVA (Level01_Gameplay) y no los de esta escena.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LevelAtmosphere : MonoBehaviour
    {
        [SerializeField] UIPalette paleta;

        [Header("Niebla (suaviza el fondo sin tapar el plano jugable)")]
        [SerializeField] bool usarNiebla = true;
        [SerializeField] float nieblaInicio = 34f;
        [SerializeField] float nieblaFin = 120f;

        [Header("Noche (reemplaza los colores de la paleta)")]
        [SerializeField] bool noche;
        [SerializeField] Color nocheCielo = new Color(0.16f, 0.20f, 0.40f);
        [SerializeField] Color nocheHorizonte = new Color(0.11f, 0.14f, 0.30f);
        [SerializeField] Color nocheSuelo = new Color(0.05f, 0.05f, 0.11f);
        [SerializeField] Color nocheNiebla = new Color(0.05f, 0.07f, 0.16f);

        void Awake() => Aplicar();

        [ContextMenu("Aplicar ahora")]
        public void Aplicar()
        {
            if (paleta == null) { Debug.LogWarning("LevelAtmosphere: falta asignar UIPalette.", this); return; }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(paleta.SepiaMedio, paleta.Arena, 0.15f);
            RenderSettings.ambientEquatorColor = Color.Lerp(paleta.TierraOscura, paleta.SepiaMedio, 0.6f);
            RenderSettings.ambientGroundColor = Color.Lerp(paleta.Tinta, paleta.TierraOscura, 0.5f);

            if (noche)
            {
                RenderSettings.ambientSkyColor = nocheCielo;
                RenderSettings.ambientEquatorColor = nocheHorizonte;
                RenderSettings.ambientGroundColor = nocheSuelo;
            }

            RenderSettings.fog = usarNiebla;
            if (usarNiebla)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = noche ? nocheNiebla : Color.Lerp(paleta.SepiaMedio, paleta.Arena, 0.5f);
                RenderSettings.fogStartDistance = nieblaInicio;
                RenderSettings.fogEndDistance = nieblaFin;
            }
        }
    }
}

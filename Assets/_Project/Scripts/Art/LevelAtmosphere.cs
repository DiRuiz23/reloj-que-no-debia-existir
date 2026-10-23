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

        void Awake() => Aplicar();

        [ContextMenu("Aplicar ahora")]
        public void Aplicar()
        {
            if (paleta == null) { Debug.LogWarning("LevelAtmosphere: falta asignar UIPalette.", this); return; }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(paleta.SepiaMedio, paleta.Arena, 0.15f);
            RenderSettings.ambientEquatorColor = Color.Lerp(paleta.TierraOscura, paleta.SepiaMedio, 0.6f);
            RenderSettings.ambientGroundColor = Color.Lerp(paleta.Tinta, paleta.TierraOscura, 0.5f);

            RenderSettings.fog = usarNiebla;
            if (usarNiebla)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = Color.Lerp(paleta.SepiaMedio, paleta.Arena, 0.5f);
                RenderSettings.fogStartDistance = nieblaInicio;
                RenderSettings.fogEndDistance = nieblaFin;
            }
        }
    }
}

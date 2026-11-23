using UnityEngine;
using UnityEngine.UI;
using Reloj.UI;

namespace Reloj.Endings
{
    /// <summary>
    /// Reloj de fondo de cada final, dibujado con UI. Se construye solo al iniciar.
    /// SpinForward (Final A): las agujas corren hacia adelante, cada vez mas rapido, y se disuelven: el tiempo se corrige.
    /// Stopped (Final B): las agujas tiemblan y se quedan en las 3:17; solo el segundero sigue andando.
    /// </summary>
    public class EndingClock : MonoBehaviour
    {
        public enum Mode { SpinForward, Stopped }

        [SerializeField] Mode mode = Mode.SpinForward;
        [SerializeField] UIPalette palette;
        [Tooltip("Sprite circular (el Knob integrado de Unity).")]
        [SerializeField] Sprite circleSprite;
        [Tooltip("Imagen de la esfera (circular, con transparencia). Si se deja vacio se dibuja la esfera de codigo.")]
        [SerializeField] Sprite dialSprite;
        [SerializeField, Min(100f)] float size = 420f;
        [Tooltip("Multiplica la opacidad del reloj (para dejarlo discreto detras del texto).")]
        [SerializeField, Range(0f, 1f)] float alphaScale = 1f;

        // 3:17 -> minutero a 17 min (102 grados), horario a 98.5 grados (sentido horario).
        const float StartMinuteDeg = 102f;
        const float StartHourDeg = 98.5f;

        RectTransform hourHand, minuteHand, secondHand;
        CanvasGroup group;
        float t0;

        void Awake()
        {
            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(size, size);
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Color tinta = palette != null ? palette.Tinta : new Color(0.17f, 0.11f, 0.08f);
            Color arena = palette != null ? palette.Arena : new Color(0.79f, 0.66f, 0.47f);
            Color pergamino = palette != null ? palette.Pergamino : new Color(0.92f, 0.85f, 0.71f);
            Color acento = palette != null ? palette.Acento : new Color(1f, 0.65f, 0.17f);

            // Esfera: si hay imagen, ella trae sus numeros; si no, se dibujan aro, cara y marcas.
            if (dialSprite != null)
            {
                var dial = Img("Esfera", root, size, size, dialSprite, Color.white);
                dial.GetComponent<Image>().preserveAspect = true;
            }
            else
            {
                var ring = Img("Aro", root, size, size, circleSprite, new Color(arena.r, arena.g, arena.b, 0.55f));
                Img("Cara", ring, size - 16f, size - 16f, circleSprite, new Color(tinta.r, tinta.g, tinta.b, 0.9f));
                DrawTicks(root, size, pergamino);
            }

            float hs = dialSprite != null ? 0.85f : 1f;   // con la imagen, las agujas se ajustan al radio de la esfera
            hourHand = Hand("Horario", root, 11f, size * 0.26f * hs, pergamino);
            minuteHand = Hand("Minutero", root, 7f, size * 0.38f * hs, pergamino);
            secondHand = Hand("Segundero", root, 3f, size * 0.42f * hs, acento);   // el acento es solo del reloj
            Img("Centro", root, 22f, 22f, circleSprite, pergamino);

            t0 = Time.unscaledTime;
            Apply(StartMinuteDeg, StartHourDeg, 0f);
        }

        void Update()
        {
            float t = Time.unscaledTime - t0;
            if (mode == Mode.SpinForward) UpdateSpin(t);
            else UpdateStopped(t);
        }

        void UpdateSpin(float t)
        {
            // Mismo ritmo que el final B: el segundero da un salto por segundo; minutero y horario avanzan a velocidad real.
            float second = Mathf.Floor(t) * 6f;
            float minute = StartMinuteDeg + t * 0.1f;
            float hour = StartHourDeg + t / 120f;
            Apply(minute, hour, second);
            // El reloj y sus agujas se quedan visibles hasta el final (ya no se disuelven).
            group.alpha = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(t / 3f)) * alphaScale;
        }

        void UpdateStopped(float t)
        {
            // Temblor que se apaga en 3 s; despues, las agujas quedan quietas en las 3:17.
            float shake = Mathf.Clamp01(1f - t / 3f);
            float jitter = (Mathf.PerlinNoise(t * 18f, 0.3f) - 0.5f) * 5f * shake;
            float second = Mathf.Floor(t) * 6f;   // el segundero da un salto por segundo
            Apply(StartMinuteDeg + jitter, StartHourDeg + jitter * 0.3f, second);
            group.alpha = Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(t / 4f)) * alphaScale;
        }

        void Apply(float minuteDeg, float hourDeg, float secondDeg)
        {
            hourHand.localRotation = Quaternion.Euler(0f, 0f, -hourDeg);
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minuteDeg);
            secondHand.localRotation = Quaternion.Euler(0f, 0f, -secondDeg);
        }

        static void DrawTicks(RectTransform root, float size, Color pergamino)
        {
            for (int i = 0; i < 12; i++)
            {
                bool major = i % 3 == 0;
                var tick = Img("Marca" + i, root, major ? 8f : 4f, major ? 30f : 18f, null, new Color(pergamino.r, pergamino.g, pergamino.b, 0.8f));
                tick.pivot = new Vector2(0.5f, 0.5f);
                float ang = i * 30f;
                float radius = size * 0.5f - 34f;
                float rad = ang * Mathf.Deg2Rad;
                tick.anchoredPosition = new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius);
                tick.localRotation = Quaternion.Euler(0f, 0f, -ang);
            }
        }

        static RectTransform Hand(string name, RectTransform parent, float width, float length, Color color)
        {
            var rt = Img(name, parent, width, length, null, color);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static RectTransform Img(string name, RectTransform parent, float w, float h, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }
    }
}

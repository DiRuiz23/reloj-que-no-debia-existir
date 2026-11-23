using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Genera la musica provisional de los dos finales (sintetizada por codigo, sin derechos de autor):
    ///  - mus_ending_a_loop: "la hora corregida", acordes mayores luminosos y campanas que suben.
    ///  - mus_ending_b_loop: "la vida que conozco", acordes menores graves y un reloj que sigue andando.
    /// Menu: Reloj > Audio > Generar musica de los finales
    /// </summary>
    public static class EndingMusicGenerator
    {
        const int SR = 44100;
        const string Folder = "Assets/_Project/Audio/Music";

        [MenuItem("Reloj/Audio/Generar musica de los finales")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            Save("mus_ending_a_loop", MusicA(), 0.8f);
            Save("mus_ending_b_loop", MusicB(), 0.8f);
            AssetDatabase.Refresh();
            Debug.Log("[Audio] Musica de los finales generada en " + Folder);
        }

        static float Sin(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }

        static float[] Pad(float freq, float seconds, float attack, float release, float brightness)
        {
            var s = new float[(int)(seconds * SR)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Clamp01(t / attack) * Mathf.Clamp01((seconds - t) / release);
                float v = Sin(freq, t) + 0.4f * brightness * Sin(freq * 2.003f, t) + 0.18f * brightness * Sin(freq * 3.005f, t);
                s[i] = v * env * env;
            }
            return s;
        }

        static float[] Bell(float freq, float seconds, float decay)
        {
            var s = new float[(int)(seconds * SR)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.008f);
                s[i] = (Sin(freq, t) + 0.3f * Sin(freq * 2.76f, t) * Mathf.Exp(-t * 2f)) * env;
            }
            return s;
        }

        static float[] Tick(float freq)
        {
            var s = new float[(int)(0.12f * SR)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * 60f) * Mathf.Clamp01(t / 0.001f);
                s[i] = (Sin(freq, t) * 0.8f + Sin(freq * 2.3f, t) * 0.3f) * env;
            }
            return s;
        }

        static void AddWrap(float[] buf, int start, float[] src, float gain)
        {
            int n = buf.Length;
            for (int i = 0; i < src.Length; i++)
                buf[((start + i) % n + n) % n] += src[i] * gain;
        }

        static void Echo(float[] m, float delaySec, float amount, int passes)
        {
            int total = m.Length, delay = (int)(delaySec * SR);
            for (int p = 0; p < passes; p++)
                for (int i = 0; i < total; i++)
                    m[i] += m[(i - delay + total) % total] * amount;
        }

        /// <summary>Final A: 32 s en bucle. C - G - Am - F, campanas ascendentes (amanecer).</summary>
        static float[] MusicA()
        {
            const float chordLen = 8f;
            int total = (int)(4 * chordLen * SR);
            var m = new float[total];
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392f },    // C
                new[] { 196f, 246.94f, 293.66f },    // G
                new[] { 220f, 261.63f, 329.63f },    // Am
                new[] { 174.61f, 220f, 261.63f }     // F
            };
            float[] bass = { 130.81f, 98f, 110f, 87.31f };
            float[][] bells =
            {
                new[] { 523.25f, 659.25f, 783.99f },
                new[] { 493.88f, 587.33f, 783.99f },
                new[] { 523.25f, 659.25f, 880f },
                new[] { 523.25f, 698.46f, 880f }
            };
            for (int c = 0; c < 4; c++)
            {
                int start = (int)(c * chordLen * SR);
                foreach (var f in chords[c])
                    AddWrap(m, start - (int)(2f * SR), Pad(f, chordLen + 4f, 3.5f, 4f, 1f), 0.15f);
                AddWrap(m, start, Pad(bass[c], chordLen + 2f, 2f, 3f, 0.6f), 0.26f);
                for (int b = 0; b < 3; b++)   // arpegio ascendente de campanas
                    AddWrap(m, start + (int)((1f + b * 1.6f) * SR), Bell(bells[c][b], 4f, 1.4f), 0.13f + 0.02f * b);
            }
            Echo(m, 0.7f, 0.3f, 2);
            return m;
        }

        /// <summary>Final B: 32 s en bucle. Am - Dm - F - E, graves, campana lejana y tic-tac suave de reloj.</summary>
        static float[] MusicB()
        {
            const float chordLen = 8f;
            int total = (int)(4 * chordLen * SR);
            var m = new float[total];
            float[][] chords =
            {
                new[] { 110f, 164.81f, 220f },       // Am
                new[] { 146.83f, 174.61f, 220f },    // Dm
                new[] { 87.31f, 130.81f, 174.61f },  // F
                new[] { 164.81f, 207.65f, 246.94f }  // E
            };
            float[] bass = { 55f, 73.42f, 43.65f, 82.41f };
            float[] bell = { 440f, 349.23f, 523.25f, 415.3f };
            for (int c = 0; c < 4; c++)
            {
                int start = (int)(c * chordLen * SR);
                foreach (var f in chords[c])
                    AddWrap(m, start - (int)(2f * SR), Pad(f, chordLen + 4f, 3.5f, 4f, 0.7f), 0.17f);
                AddWrap(m, start, Pad(bass[c], chordLen + 2f, 2.5f, 3f, 0.5f), 0.3f);
                AddWrap(m, start + (int)(2.5f * SR), Bell(bell[c], 5f, 0.9f), 0.12f);
            }
            // Reloj que sigue andando: un tic cada segundo, tic y toc alternados, muy bajito.
            var tick = Tick(2200f);
            var tock = Tick(1500f);
            for (int s = 0; s < (int)(4 * chordLen); s++)
                AddWrap(m, (int)(s * SR), s % 2 == 0 ? tick : tock, 0.1f);
            Echo(m, 0.9f, 0.28f, 2);
            return m;
        }

        static void Save(string name, float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            float g = max > 0.0001f ? peak / max : 1f;

            string path = Folder + "/" + name + ".wav";
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int bytes = data.Length * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' }); bw.Write(36 + bytes);
                bw.Write(new[] { 'W', 'A', 'V', 'E' }); bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16); bw.Write((short)1); bw.Write((short)1);
                bw.Write(SR); bw.Write(SR * 2); bw.Write((short)2); bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' }); bw.Write(bytes);
                for (int i = 0; i < data.Length; i++)
                    bw.Write((short)Mathf.RoundToInt(Mathf.Clamp(data[i] * g, -1f, 1f) * 32767f));
            }
        }
    }
}

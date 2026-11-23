using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Genera SOLO los sonidos que faltaban para el issue de audio (salto, recoger objeto, dano,
    /// tic-tac del estado Sospechoso y musica de menu). Sintetizados por codigo, sin derechos de autor.
    /// No toca los 18 WAV ya existentes.
    /// Menu: Reloj > Audio > Generar audio faltante (issue audio)
    /// </summary>
    public static class PlaceholderAudioGeneratorI22
    {
        const int SR = 44100;
        const string Root = "Assets/_Project/Audio";
        static System.Random rng;

        [MenuItem("Reloj/Audio/Generar audio faltante (issue audio)")]
        public static void GenerateMissing()
        {
            rng = new System.Random(20261115);
            Directory.CreateDirectory(Root + "/SFX");
            Directory.CreateDirectory(Root + "/Music");

            Save("SFX/sfx_jump", Jump(), 0.6f);
            Save("SFX/sfx_pickup", Pickup(), 0.6f);
            Save("SFX/sfx_damage", Damage(), 0.8f);
            Save("SFX/sfx_ticktock_loop", TickTock(), 0.55f);
            Save("Music/mus_menu_loop", MusicMenu(), 0.8f);

            AssetDatabase.Refresh();
            Debug.Log("[Audio] Audio faltante generado en " + Root);
        }

        // ------------------------------------------------------------------ utilidades

        static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float[] Buf(float seconds) { return new float[(int)(seconds * SR)]; }
        static float Sin(float freq, float t) { return Mathf.Sin(2f * Mathf.PI * freq * t); }

        static void Save(string relativePath, float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            float g = max > 0.0001f ? peak / max : 1f;

            string path = Root + "/" + relativePath + ".wav";
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int dataBytes = data.Length * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataBytes);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(SR);
                bw.Write(SR * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(dataBytes);
                for (int i = 0; i < data.Length; i++)
                {
                    float v = Mathf.Clamp(data[i] * g, -1f, 1f);
                    bw.Write((short)Mathf.RoundToInt(v * 32767f));
                }
            }
        }

        static void Fade(float[] b, float fadeInSec, float fadeOutSec)
        {
            int fi = Mathf.Min(b.Length, (int)(fadeInSec * SR));
            int fo = Mathf.Min(b.Length, (int)(fadeOutSec * SR));
            for (int i = 0; i < fi; i++) b[i] *= i / (float)fi;
            for (int i = 0; i < fo; i++) b[b.Length - 1 - i] *= i / (float)fo;
        }

        static float[] Scale(float[] s, float g)
        {
            for (int i = 0; i < s.Length; i++) s[i] *= g;
            return s;
        }

        static void AddWrap(float[] buf, int start, float[] src)
        {
            int n = buf.Length;
            for (int i = 0; i < src.Length; i++)
                buf[(start + i) % n] += src[i];
        }

        // ------------------------------------------------------------------ SFX

        /// <summary>Barrido ascendente corto + soplo de aire.</summary>
        static float[] Jump()
        {
            var b = Buf(0.22f);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(220f, 520f, Mathf.Clamp01(t / 0.18f));
                phase += 2f * Mathf.PI * f / SR;
                lp += (Noise() - lp) * 0.08f;
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 11f);
                b[i] = (Mathf.Sin(phase) * 0.7f + lp * 0.5f) * env;
            }
            Fade(b, 0.001f, 0.04f);
            return b;
        }

        /// <summary>Dos notas brillantes ascendentes tipo "moneda".</summary>
        static float[] Pickup()
        {
            var b = Buf(0.45f);
            float[] freqs = { 880f, 1318.5f };
            float[] starts = { 0f, 0.07f };
            for (int n = 0; n < freqs.Length; n++)
            {
                int s0 = (int)(starts[n] * SR);
                for (int i = s0; i < b.Length; i++)
                {
                    float t = (i - s0) / (float)SR;
                    float env = Mathf.Exp(-t * 9f) * Mathf.Clamp01(t / 0.003f);
                    b[i] += (Sin(freqs[n], t) + 0.3f * Sin(freqs[n] * 2f, t)) * env * 0.5f;
                }
            }
            Fade(b, 0.001f, 0.05f);
            return b;
        }

        /// <summary>Golpe sordo con ruido y caida de tono.</summary>
        static float[] Damage()
        {
            var b = Buf(0.35f);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(180f, 55f, 1f - Mathf.Exp(-t * 14f));
                phase += 2f * Mathf.PI * f / SR;
                lp += (Noise() - lp) * 0.25f;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t * 12f);
                float crack = lp * Mathf.Exp(-t * 40f) * 0.8f;
                b[i] = body + crack;
            }
            Fade(b, 0.0005f, 0.05f);
            return b;
        }

        /// <summary>
        /// Loop de 1.0 s: "tic" a 0.0 s y "toc" (mas grave) a 0.5 s. Termina en silencio para repetir sin saltos.
        /// </summary>
        static float[] TickTock()
        {
            var b = Buf(1.0f);
            AddClick(b, 0.0f, 2200f, 0.9f);
            AddClick(b, 0.5f, 1500f, 0.75f);
            return b;
        }

        static void AddClick(float[] b, float startSec, float freq, float amp)
        {
            int s0 = (int)(startSec * SR);
            for (int i = s0; i < b.Length; i++)
            {
                float t = (i - s0) / (float)SR;
                if (t > 0.12f) break;
                float env = Mathf.Exp(-t * 70f) * Mathf.Clamp01(t / 0.0008f);
                float v = Sin(freq, t) * 0.8f + Sin(freq * 2.3f, t) * 0.3f + Noise() * 0.25f * Mathf.Exp(-t * 200f);
                b[i] += v * env * amp;
            }
        }

        // ------------------------------------------------------------------ Musica

        static float[] PadNote(float freq, float seconds)
        {
            var s = new float[(int)(seconds * SR)];
            float attack = 3f, release = 3.5f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Clamp01(t / attack) * Mathf.Clamp01((seconds - t) / release);
                float v = Sin(freq, t) + 0.45f * Sin(freq * 2.002f, t) + 0.2f * Sin(freq * 3.004f, t);
                s[i] = v * env * env;
            }
            return s;
        }

        static float[] Bell(float freq, float seconds)
        {
            var s = new float[(int)(seconds * SR)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * 1.6f) * Mathf.Clamp01(t / 0.01f);
                s[i] = (Sin(freq, t) + 0.3f * Sin(freq * 2.76f, t) * Mathf.Exp(-t * 2f)) * env;
            }
            Fade(s, 0.002f, 0.3f);
            return s;
        }

        /// <summary>
        /// Musica de menu: 24 s en bucle, Am - F - C - G, pad lento + campanas suaves.
        /// Distinta del nivel 1 (mas luminosa y sin arpegio de guitarra) para que el cambio se note.
        /// </summary>
        static float[] MusicMenu()
        {
            const float chordLen = 6f;
            int total = (int)(4 * chordLen * SR);
            var m = new float[total];

            float[][] chords =
            {
                new[] { 220f, 261.63f, 329.63f },   // Am
                new[] { 174.61f, 220f, 261.63f },   // F
                new[] { 261.63f, 329.63f, 392f },   // C
                new[] { 196f, 246.94f, 293.66f }    // G
            };
            float[] bass = { 110f, 87.31f, 130.81f, 98f };
            float[] bellNotes = { 659.25f, 523.25f, 783.99f, 587.33f };

            for (int c = 0; c < 4; c++)
            {
                int start = (int)(c * chordLen * SR);
                foreach (var f in chords[c])
                    AddWrap(m, start - (int)(1.5f * SR) + total, Scale(PadNote(f, chordLen + 3f), 0.15f));
                AddWrap(m, start, Scale(PadNote(bass[c], chordLen + 2f), 0.26f));
                AddWrap(m, start + (int)(1.0f * SR), Scale(Bell(bellNotes[c], 3.5f), 0.14f));
                AddWrap(m, start + (int)(3.5f * SR), Scale(Bell(bellNotes[c] * 1.5f, 3f), 0.09f));
            }

            // Eco circular: une el final con el principio.
            int delay = (int)(0.6f * SR);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < total; i++)
                    m[i] += m[(i - delay + total) % total] * 0.3f;

            return m;
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reloj.EditorTools
{
    /// <summary>
    /// Genera sonidos y musica de relleno (sintetizados, sin derechos de autor) en Assets/_Project/Audio.
    /// Sirven para tener el sistema de audio funcionando; se pueden reemplazar por clips finales
    /// asignando otros AudioClip en el AudioManager.
    /// Menu: Reloj > Audio > Generar audio de relleno
    /// </summary>
    public static class PlaceholderAudioGenerator
    {
        const int SR = 44100;
        const string Root = "Assets/_Project/Audio";
        static System.Random rng;

        [MenuItem("Reloj/Audio/Generar audio de relleno")]
        public static void GenerateAll()
        {
            rng = new System.Random(20260428);
            Directory.CreateDirectory(Root + "/SFX");
            Directory.CreateDirectory(Root + "/Music");
            Directory.CreateDirectory(Root + "/Ambience");

            // --- SFX ---
            Save("SFX/sfx_footstep_1", Footstep(0.30f, 68f), 0.55f);
            Save("SFX/sfx_footstep_2", Footstep(0.38f, 74f), 0.55f);
            Save("SFX/sfx_footstep_3", Footstep(0.25f, 62f), 0.55f);
            Save("SFX/sfx_timestop_start", TimeStopStart(), 0.85f);
            Save("SFX/sfx_timestop_end", TimeStopEnd(), 0.8f);
            Save("SFX/sfx_timestop_ready", Bells(0.7f, new[] { (880f, 0f) }, 6f), 0.5f);
            Save("SFX/sfx_checkpoint", Bells(1.2f, new[] { (659.25f, 0f), (987.77f, 0.18f) }, 4f), 0.65f);
            Save("SFX/sfx_death", Death(), 0.85f);
            Save("SFX/sfx_respawn", Respawn(), 0.5f);
            Save("SFX/sfx_goal", Bells(2.4f, new[] { (440f, 0f), (523.25f, 0.2f), (659.25f, 0.4f), (880f, 0.6f), (1318.5f, 0.85f) }, 2.4f), 0.7f);
            Save("SFX/sfx_dialogue_open", Pluck(0.2f, 520f, 20f), 0.5f);
            Save("SFX/sfx_dialogue_close", Pluck(0.2f, 390f, 20f), 0.5f);
            Save("SFX/sfx_ui_click", UiClick(), 0.6f);
            Save("SFX/sfx_pause", TwoTone(0.28f, 660f, 440f), 0.5f);
            Save("SFX/sfx_resume", TwoTone(0.28f, 440f, 660f), 0.5f);

            // --- Ambiente (loops) ---
            Save("Ambience/amb_wind_loop", LoopWind(14f), 0.6f);
            Save("Ambience/amb_train_rumble_loop", LoopRumble(8f), 0.7f);

            // --- Musica (loop) ---
            Save("Music/mus_level01_loop", MusicLevel01(), 0.8f);

            AssetDatabase.Refresh();
            Debug.Log("[Audio] Audio de relleno generado en " + Root);
        }

        // ------------------------------------------------------------------ utilidades

        static float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
        static float[] Buf(float seconds) { return new float[(int)(seconds * SR)]; }
        static float Sin(float freq, float t) { return Mathf.Sin(2f * Mathf.PI * freq * t); }

        static void Save(string relativePath, float[] data, float peak)
        {
            // Normaliza al pico pedido.
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
                bw.Write((short)1);      // PCM
                bw.Write((short)1);      // mono
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

        /// <summary>Quita el click de inicio/fin con un fundido minimo.</summary>
        static void Fade(float[] b, float fadeInSec, float fadeOutSec)
        {
            int fi = Mathf.Min(b.Length, (int)(fadeInSec * SR));
            int fo = Mathf.Min(b.Length, (int)(fadeOutSec * SR));
            for (int i = 0; i < fi; i++) b[i] *= i / (float)fi;
            for (int i = 0; i < fo; i++) b[b.Length - 1 - i] *= i / (float)fo;
        }

        /// <summary>Hace que un buffer de longitud N + F se pueda repetir sin saltos (fundido cruzado).</summary>
        static float[] MakeLoop(float[] longBuf, int loopLen)
        {
            int f = longBuf.Length - loopLen;
            var o = new float[loopLen];
            Array.Copy(longBuf, o, loopLen);
            for (int i = 0; i < f; i++)
            {
                float w = i / (float)f;                    // 0 -> 1
                float a = Mathf.Sin(w * Mathf.PI * 0.5f);  // entra la cabeza
                float b = Mathf.Cos(w * Mathf.PI * 0.5f);  // sale la cola (continuacion del final)
                o[i] = longBuf[i] * a + longBuf[loopLen + i] * b;
            }
            return o;
        }

        // ------------------------------------------------------------------ SFX

        static float[] Footstep(float lowpass, float thumpHz)
        {
            var b = Buf(0.16f);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                lp += (Noise() - lp) * lowpass;
                float crunch = lp * Mathf.Exp(-t * 34f);
                float thump = Sin(thumpHz, t) * Mathf.Exp(-t * 48f) * 0.8f;
                b[i] = crunch + thump;
            }
            Fade(b, 0.001f, 0.01f);
            return b;
        }

        static float[] TimeStopStart()
        {
            var b = Buf(1.1f);
            float phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(260f, 48f, 1f - Mathf.Exp(-t * 3.2f));
                phase += 2f * Mathf.PI * f / SR;
                float sweep = Mathf.Sin(phase) * Mathf.Exp(-t * 2.8f);
                float shimmer = (Sin(880f * (1f - t * 0.4f), t) + 0.5f * Sin(1320f, t)) * Mathf.Exp(-t * 5f) * 0.25f;
                float tick = Sin(1800f, t) * Mathf.Exp(-t * 70f) * 0.9f;
                b[i] = sweep + shimmer + tick;
            }
            Fade(b, 0.001f, 0.08f);
            return b;
        }

        static float[] TimeStopEnd()
        {
            var b = Buf(0.8f);
            float phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(90f, 520f, Mathf.Clamp01(t / 0.5f));
                phase += 2f * Mathf.PI * f / SR;
                float env = Mathf.Clamp01(t / 0.5f) * Mathf.Exp(-Mathf.Max(0f, t - 0.5f) * 14f);
                float sweep = Mathf.Sin(phase) * env * 0.7f;
                float t2 = Mathf.Max(0f, t - 0.5f);
                float tick = t >= 0.5f ? Sin(1500f, t2) * Mathf.Exp(-t2 * 80f) : 0f;
                float tock = t >= 0.62f ? Sin(1100f, t - 0.62f) * Mathf.Exp(-(t - 0.62f) * 80f) * 0.8f : 0f;
                b[i] = sweep + tick + tock;
            }
            Fade(b, 0.002f, 0.05f);
            return b;
        }

        /// <summary>Notas tipo campana (parciales inarmonicos). notes = (frecuencia, inicio en segundos).</summary>
        static float[] Bells(float seconds, (float freq, float start)[] notes, float decay)
        {
            var b = Buf(seconds);
            foreach (var n in notes)
            {
                int s0 = (int)(n.start * SR);
                for (int i = s0; i < b.Length; i++)
                {
                    float t = (i - s0) / (float)SR;
                    float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.004f);
                    float v = Sin(n.freq, t)
                            + 0.35f * Sin(n.freq * 2.76f, t) * Mathf.Exp(-t * decay * 1.8f)
                            + 0.2f * Sin(n.freq * 5.4f, t) * Mathf.Exp(-t * decay * 3f);
                    b[i] += v * env * 0.4f;
                }
            }
            Fade(b, 0.001f, 0.06f);
            return b;
        }

        static float[] Death()
        {
            var b = Buf(0.9f);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(220f, 38f, 1f - Mathf.Exp(-t * 5f));
                phase += 2f * Mathf.PI * f / SR;
                lp += (Noise() - lp) * 0.12f;
                b[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 4f) + lp * Mathf.Exp(-t * 12f) * 0.9f;
            }
            Fade(b, 0.001f, 0.1f);
            return b;
        }

        static float[] Respawn()
        {
            var b = Buf(0.6f);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float a = Mathf.Lerp(0.03f, 0.35f, t / 0.6f);
                lp += (Noise() - lp) * a;
                float env = Mathf.Sin(Mathf.Clamp01(t / 0.6f) * Mathf.PI);
                b[i] = lp * env + Sin(Mathf.Lerp(300f, 700f, t / 0.6f), t) * env * 0.25f;
            }
            Fade(b, 0.01f, 0.05f);
            return b;
        }

        static float[] Pluck(float seconds, float freq, float decay)
        {
            var b = Buf(seconds);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.002f);
                b[i] = (Sin(freq, t) + 0.4f * Sin(freq * 2f, t) + 0.15f * Sin(freq * 3f, t)) * env;
            }
            Fade(b, 0.001f, 0.02f);
            return b;
        }

        static float[] UiClick()
        {
            var b = Buf(0.08f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                b[i] = Sin(1300f, t) * Mathf.Exp(-t * 60f) + Noise() * Mathf.Exp(-t * 400f) * 0.4f;
            }
            Fade(b, 0.0005f, 0.01f);
            return b;
        }

        static float[] TwoTone(float seconds, float f1, float f2)
        {
            var b = Buf(seconds);
            int half = b.Length / 2;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                bool second = i >= half;
                float ts = second ? (i - half) / (float)SR : t;
                float f = second ? f2 : f1;
                b[i] = Sin(f, ts) * Mathf.Exp(-ts * 18f) * Mathf.Clamp01(ts / 0.003f);
            }
            Fade(b, 0.001f, 0.02f);
            return b;
        }

        // ------------------------------------------------------------------ Ambiente

        static float[] LoopWind(float seconds)
        {
            int loop = (int)(seconds * SR);
            int extra = (int)(2f * SR);
            var b = new float[loop + extra];
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                // LFOs con periodo que divide la duracion del loop.
                float lfo1 = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (1f / seconds) * t);
                float lfo2 = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (3f / seconds) * t + 1.3f);
                float a = Mathf.Lerp(0.012f, 0.07f, 0.65f * lfo1 + 0.35f * lfo2);
                lp1 += (Noise() - lp1) * a;
                lp2 += (lp1 - lp2) * a;
                float amp = Mathf.Lerp(0.35f, 1f, lfo1);
                b[i] = lp2 * amp * 6f;
            }
            return MakeLoop(b, loop);
        }

        static float[] LoopRumble(float seconds)
        {
            int loop = (int)(seconds * SR);
            int extra = (int)(1.5f * SR);
            var b = new float[loop + extra];
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                lp += (Noise() - lp) * 0.02f;
                float chug = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * (24f / seconds) * t);   // traqueteo
                float clack = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * (48f / seconds) * t)), 12f);
                float body = Sin(41f, t) * 0.5f + Sin(82f, t) * 0.2f;
                b[i] = (lp * 9f + body) * chug + clack * lp * 6f;
            }
            return MakeLoop(b, loop);
        }

        // ------------------------------------------------------------------ Musica

        static void AddWrap(float[] buf, int start, float[] src)
        {
            int n = buf.Length;
            for (int i = 0; i < src.Length; i++)
                buf[(start + i) % n] += src[i];
        }

        static float[] PadNote(float freq, float seconds)
        {
            var s = new float[(int)(seconds * SR)];
            float attack = 2.5f, release = 3f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Clamp01(t / attack) * Mathf.Clamp01((seconds - t) / release);
                float v = Sin(freq, t) + 0.5f * Sin(freq * 2.003f, t) + 0.25f * Sin(freq * 3.006f, t) + 0.12f * Sin(freq * 4f, t);
                s[i] = v * env * env;
            }
            return s;
        }

        static float[] KarplusStrong(float freq, float seconds, float damping)
        {
            int n = (int)(seconds * SR);
            int d = Mathf.Max(2, (int)(SR / freq));
            var line = new float[d];
            for (int i = 0; i < d; i++) line[i] = Noise();
            var o = new float[n];
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                int next = (idx + 1) % d;
                float v = line[idx];
                line[idx] = (line[idx] + line[next]) * 0.5f * damping;
                o[i] = v;
                idx = next;
            }
            Fade(o, 0.002f, 0.2f);
            return o;
        }

        static float[] MusicLevel01()
        {
            // 4 acordes x 8 s = 32 s. Progresion andaluza: Am - G - F - E. Tempo lento (60 bpm).
            const float chordLen = 8f;
            int total = (int)(4 * chordLen * SR);
            var m = new float[total];

            // Notas (Hz)
            float A2 = 110f, G2 = 98f, F2 = 87.31f, E2 = 82.41f;
            float[][] chords =
            {
                new[] { 220f, 261.63f, 329.63f },   // Am
                new[] { 196f, 246.94f, 293.66f },   // G
                new[] { 174.61f, 220f, 261.63f },   // F
                new[] { 164.81f, 207.65f, 246.94f } // E (con G#)
            };
            float[] bass = { A2, G2, F2, E2 };
            // Arpegio de guitarra (indices dentro del acorde) por pulso (1 pulso = 1 s)
            int[] pattern = { 0, 2, 1, 2, 0, 2, 1, -1 };
            float[] octaveUp = { 2f, 2f, 2f, 2f };

            for (int c = 0; c < 4; c++)
            {
                int start = (int)(c * chordLen * SR);
                // Pad (con cola que pasa al siguiente acorde y da la vuelta al loop)
                foreach (var f in chords[c])
                    AddWrap(m, start - (int)(1.5f * SR) + total, Scale(PadNote(f, chordLen + 3f), 0.16f));
                // Bajo
                AddWrap(m, start, Scale(PadNote(bass[c], chordLen + 2f), 0.28f));

                // Guitarra
                for (int beat = 0; beat < 8; beat++)
                {
                    int p = pattern[beat];
                    if (p < 0) continue;
                    float f = chords[c][p] * octaveUp[c];
                    var pluck = KarplusStrong(f, 3.2f, 0.9965f);
                    AddWrap(m, start + (int)(beat * SR), Scale(pluck, 0.22f));
                }
            }

            // Eco circular (da profundidad y une el final con el principio).
            int delay = (int)(0.5f * SR);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < total; i++)
                    m[i] += m[(i - delay + total) % total] * 0.33f;

            return m;
        }

        static float[] Scale(float[] s, float g)
        {
            for (int i = 0; i < s.Length; i++) s[i] *= g;
            return s;
        }
    }
}

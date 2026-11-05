using UnityEngine;

namespace VoidConverter.View
{
    /// <summary>
    /// Builds the placeholder sound effects in code (sine/square/saw tones and noise), so the game needs no audio
    /// files. Every clip starts with the same short "thud" of the block landing, then the outcome sound follows.
    /// Real sound files can replace these later without touching the callers.
    /// </summary>
    public static class SfxSynth
    {
        const int Rate = 44100;
        // The outcome sound starts this long after the thud, so the two are heard as "land, then result".
        const float OutcomeStart = 0.1f;

        enum Wave { Sine, Square, Saw }

        public static AudioClip Win(bool magic)
        {
            var b = Landed(0.65f);
            // The hit itself: a sharp crack and a deep thump under the usual landing thud.
            Noise(b, 0f, 0.06f, 0.55f);
            Tone(b, 0f, 0.18f, 95f, 38f, 0.55f, Wave.Sine);
            float k = magic ? 1.5f : 1f;
            Tone(b, OutcomeStart, 0.10f, 523f * k, 523f * k, 0.35f, Wave.Sine);
            Tone(b, OutcomeStart + 0.08f, 0.10f, 659f * k, 659f * k, 0.35f, Wave.Sine);
            Tone(b, OutcomeStart + 0.16f, 0.30f, 784f * k, 784f * k, 0.40f, Wave.Sine);
            Tone(b, OutcomeStart + 0.16f, 0.30f, 1568f * k, 1568f * k, 0.10f, Wave.Sine);
            return Make(magic ? "WinMagic" : "Win", b);
        }

        public static AudioClip Gold()
        {
            var b = Landed(0.5f);
            Tone(b, OutcomeStart, 0.07f, 1319f, 1319f, 0.22f, Wave.Square);
            Tone(b, OutcomeStart + 0.07f, 0.30f, 1760f, 1760f, 0.35f, Wave.Sine);
            Tone(b, OutcomeStart + 0.07f, 0.18f, 3520f, 3520f, 0.10f, Wave.Sine);
            return Make("Gold", b);
        }

        public static AudioClip Heal()
        {
            var b = Landed(0.7f);
            Tone(b, OutcomeStart, 0.45f, 392f, 784f, 0.38f, Wave.Sine);
            Tone(b, OutcomeStart + 0.05f, 0.40f, 523f, 1046f, 0.20f, Wave.Sine);
            return Make("Heal", b);
        }

        public static AudioClip Lose()
        {
            var b = Landed(0.6f);
            Noise(b, OutcomeStart, 0.22f, 0.45f);
            Tone(b, OutcomeStart, 0.40f, 160f, 55f, 0.45f, Wave.Saw);
            return Make("Lose", b);
        }

        public static AudioClip Trap()
        {
            var b = Landed(0.6f);
            Noise(b, OutcomeStart, 0.12f, 0.55f);
            Tone(b, OutcomeStart, 0.28f, 330f, 110f, 0.30f, Wave.Square);
            Tone(b, OutcomeStart + 0.12f, 0.22f, 90f, 90f, 0.30f, Wave.Saw);
            return Make("Trap", b);
        }

        // A buffer that already holds the block-landing thud.
        static float[] Landed(float seconds)
        {
            var b = new float[Mathf.CeilToInt(seconds * Rate)];
            Tone(b, 0f, 0.13f, 130f, 55f, 0.70f, Wave.Sine);
            return b;
        }

        static void Tone(float[] buffer, float start, float duration, float freqFrom, float freqTo, float volume, Wave wave)
        {
            int first = Mathf.FloorToInt(start * Rate);
            int count = Mathf.FloorToInt(duration * Rate);
            double phase = 0.0;
            for (int i = 0; i < count && first + i < buffer.Length; i++)
            {
                float t = (float)i / count;
                float freq = Mathf.Lerp(freqFrom, freqTo, t);
                phase += 2.0 * Mathf.PI * freq / Rate;
                float sample;
                switch (wave)
                {
                    case Wave.Square: sample = Mathf.Sin((float)phase) >= 0f ? 1f : -1f; break;
                    case Wave.Saw: sample = (float)(phase / (2.0 * Mathf.PI) % 1.0) * 2f - 1f; break;
                    default: sample = Mathf.Sin((float)phase); break;
                }
                // Quick attack so the start does not click, then a fade out.
                float attack = Mathf.Min(1f, i / (0.004f * Rate));
                float release = (1f - t) * (1f - t);
                buffer[first + i] += sample * volume * attack * release;
            }
        }

        static void Noise(float[] buffer, float start, float duration, float volume)
        {
            int first = Mathf.FloorToInt(start * Rate);
            int count = Mathf.FloorToInt(duration * Rate);
            var rng = new System.Random(12345);
            for (int i = 0; i < count && first + i < buffer.Length; i++)
            {
                float t = (float)i / count;
                float sample = (float)(rng.NextDouble() * 2.0 - 1.0);
                buffer[first + i] += sample * volume * (1f - t) * (1f - t);
            }
        }

        static AudioClip Make(string name, float[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] = Mathf.Clamp(buffer[i], -1f, 1f);
            var clip = AudioClip.Create(name, buffer.Length, 1, Rate, false);
            clip.SetData(buffer, 0);
            return clip;
        }
    }
}

using UnityEngine;

// Dreamy procedural soundtrack: soft pads, a music-box melody, a gentle bass and ambient wind,
// birds by day and crickets by night. All generated in code and streamed, so no audio files are needed.
public sealed class Music : MonoBehaviour
{
    private const int Rate = 44100;
    private const int Steps = 8; // eighth notes per bar

    // One chord per bar, as semitones above the root, and the roots per theme.
    private static readonly int[][] Chords =
    {
        new[] { 0, 4, 7, 11 }, new[] { 9, 0, 4, 7 }, new[] { 5, 9, 0, 4 }, new[] { 7, 11, 2, 5 },
    };
    private static readonly int[] Scale = { 0, 2, 4, 7, 9 };
    private static readonly float[] ThemeRoot = { 60f, 57f, 55f, 62f, 53f, 59f, 57f, 52f, 64f, 62f, 50f, 54f, 65f, 63f, 48f, 46f };
    private static readonly float[] ThemeBpm = { 84f, 72f, 62f, 92f, 58f, 78f, 70f, 58f, 88f, 80f, 96f, 88f, 98f, 86f, 50f, 54f };

    private static Music instance;

    private volatile float volume = 0.5f;
    private volatile int theme;
    private volatile bool boss;
    private volatile float duck = 1f;

    private double sampleTime;
    private double lastNoteStep = -1;
    private float lowpass, wind, windLfo;
    private System.Random rng = new System.Random(7);

    private struct Voice { public float Freq, Phase, Age, Length, Gain; public int Kind; }
    private readonly Voice[] voices = new Voice[24];
    private float chirpAge = 99f, chirpFreq, chirpPhase;
    private bool chirpBird;

    private void Awake()
    {
        instance = this;
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = AudioClip.Create("music", Rate * 10, 1, Rate, true, OnRead);
        source.loop = true;
        source.volume = 1f;
        source.spatialBlend = 0f;
        source.Play();
    }

    public static void SetTheme(int index) { if (instance != null) instance.theme = ((index % ThemeRoot.Length) + ThemeRoot.Length) % ThemeRoot.Length; }
    public static void SetBoss(bool on) { if (instance != null) instance.boss = on; }
    public static void SetDuck(float amount) { if (instance != null) instance.duck = amount; }

    private static float Hz(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

    private void Trigger(float freq, float length, float gain, int kind)
    {
        var slot = 0;
        var oldest = -1f;
        for (var i = 0; i < voices.Length; i++)
        {
            if (voices[i].Age >= voices[i].Length) { slot = i; break; }
            if (voices[i].Age > oldest) { oldest = voices[i].Age; slot = i; }
        }
        voices[slot] = new Voice { Freq = freq, Length = length, Gain = gain, Kind = kind };
    }

    private void OnRead(float[] data)
    {
        var th = theme;
        var bpm = ThemeBpm[th] * (boss ? 1.25f : 1f);
        var stepLen = 60.0 / bpm / 2.0;
        var barLen = stepLen * Steps;
        var root = ThemeRoot[th];
        var night = th == 2 || th == 4;
        var vol = volume * duck;
        const float dt = 1f / Rate;

        for (var n = 0; n < data.Length; n++)
        {
            var step = (long)(sampleTime / stepLen);
            if (step != lastNoteStep)
            {
                lastNoteStep = step;
                var bar = (int)((step / Steps) % Chords.Length);
                var inBar = (int)(step % Steps);
                var chord = Chords[bar];
                if (inBar == 0)
                {
                    for (var c = 1; c < chord.Length; c++) Trigger(Hz(root + chord[c]), (float)barLen * 1.05f, 0.07f, 0);
                    Trigger(Hz(root - 12 + chord[0]), (float)barLen * 0.9f, 0.16f, 2);
                }
                if (inBar == 4) Trigger(Hz(root - 12 + chord[0] + 7), (float)stepLen * 3f, 0.08f, 2);

                // Music-box arpeggio over the chord, with the odd melody bell.
                var arp = chord[(inBar * (boss ? 3 : 1)) % chord.Length] + 12;
                if (inBar % 2 == 0 || boss) Trigger(Hz(root + arp), 0.9f, 0.09f, 1);
                if (rng.NextDouble() < (night ? 0.18 : 0.3) && inBar % 2 == 1)
                {
                    var note = Scale[rng.Next(Scale.Length)] + 24 + (rng.NextDouble() < 0.3 ? 12 : 0);
                    Trigger(Hz(root + note), 1.4f, 0.07f, 1);
                }
                // Little animal sounds: birds by day, crickets at night.
                if (chirpAge > 1f && rng.NextDouble() < 0.1)
                {
                    chirpAge = 0f;
                    chirpBird = !night;
                    chirpFreq = chirpBird ? 2200f + (float)rng.NextDouble() * 1200f : 4200f;
                    chirpPhase = 0f;
                }
            }

            var s = 0f;
            for (var v = 0; v < voices.Length; v++)
            {
                if (voices[v].Age >= voices[v].Length) continue;
                ref var vo = ref voices[v];
                var t = vo.Age;
                float env, wave;
                vo.Phase += vo.Freq * dt;
                var ph = vo.Phase * Mathf.PI * 2f;
                if (vo.Kind == 0)
                {
                    // Pad: slow swell in and out with a slightly detuned shimmer.
                    env = Mathf.Sin(Mathf.Clamp01(t / vo.Length) * Mathf.PI);
                    env *= env;
                    wave = Mathf.Sin(ph) + 0.5f * Mathf.Sin(ph * 1.003f + 1f);
                }
                else if (vo.Kind == 1)
                {
                    // Music box: bright bell partials with a fast attack and long decay.
                    env = Mathf.Min(t / 0.004f, 1f) * Mathf.Exp(-t * 3.2f);
                    wave = Mathf.Sin(ph) + 0.35f * Mathf.Sin(ph * 2.76f) * Mathf.Exp(-t * 6f) + 0.15f * Mathf.Sin(ph * 5.4f) * Mathf.Exp(-t * 12f);
                }
                else
                {
                    env = Mathf.Min(t / 0.05f, 1f) * Mathf.Exp(-t * 1.3f);
                    wave = Mathf.Sin(ph);
                }
                s += wave * env * vo.Gain;
                vo.Age += dt;
            }

            // Wind: noise through a slow low-pass whose cutoff drifts.
            windLfo += dt * 0.12f;
            var noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            var k = 0.02f + 0.015f * Mathf.Sin(windLfo * Mathf.PI * 2f);
            wind += (noise - wind) * k;
            s += wind * (0.22f + 0.12f * Mathf.Sin(windLfo * 5f));

            if (chirpAge < 0.5f)
            {
                chirpAge += dt;
                if (chirpBird)
                {
                    var f = chirpFreq * (1f + Mathf.Sin(chirpAge * 40f) * 0.25f + chirpAge * 0.8f);
                    chirpPhase += f * dt;
                    var e = Mathf.Sin(Mathf.Clamp01(chirpAge / 0.25f) * Mathf.PI);
                    s += Mathf.Sin(chirpPhase * Mathf.PI * 2f) * e * 0.035f;
                }
                else
                {
                    chirpPhase += chirpFreq * dt;
                    var gate = (chirpAge % 0.09f) < 0.04f ? 1f : 0f;
                    s += Mathf.Sin(chirpPhase * Mathf.PI * 2f) * gate * 0.012f * Mathf.Clamp01(1f - chirpAge * 2f);
                }
            }

            // Soft low-pass to keep everything gentle, then a gentle limiter.
            lowpass += (s - lowpass) * 0.45f;
            data[n] = (float)System.Math.Tanh(lowpass * vol * 1.6f);
            sampleTime += dt;
        }
    }
}

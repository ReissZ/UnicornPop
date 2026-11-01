using UnityEngine;

// Procedural sound effects, so no audio files are needed.
public sealed class Sfx : MonoBehaviour
{
    public static Sfx Instance { get; private set; }

    private AudioSource source;
    private AudioClip pop, golden, boom, flap, jump, dash, lose, gameOver, start, record;

    private void Awake()
    {
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        pop = Clip(0.2f, (t, n) => (Mathf.Sin(t * Mathf.PI * 2f * (500f + t * 3000f)) * 0.5f + n * 0.5f) * Mathf.Exp(-t * 22f));
        golden = Clip(0.6f, (t, n) => (Tone(t, 880f) + Tone(t, 1320f) + Tone(t, 1760f) * 0.6f) * 0.3f * Mathf.Exp(-t * 5f));
        boom = Clip(0.4f, (t, n) => (Mathf.Sin(t * Mathf.PI * 2f * (110f - t * 120f)) * 0.8f + n * 0.35f) * Mathf.Exp(-t * 9f));
        flap = Clip(0.18f, (t, n) => n * 0.35f * Mathf.Sin(t / 0.18f * Mathf.PI) * Mathf.Exp(-t * 6f));
        jump = Clip(0.14f, (t, n) => Mathf.Sin(t * Mathf.PI * 2f * (300f + t * 2500f)) * 0.35f * Mathf.Exp(-t * 12f));
        dash = Clip(0.45f, (t, n) => (n * 0.4f + Mathf.Sin(t * Mathf.PI * 2f * (200f + t * 1400f)) * 0.3f) * Mathf.Exp(-t * 5f));
        lose = Clip(0.5f, (t, n) => Mathf.Sin(t * Mathf.PI * 2f * (330f - t * 420f)) * 0.45f * Mathf.Exp(-t * 4f));
        gameOver = Clip(1.2f, (t, n) => Tone(t, t < 0.4f ? 392f : t < 0.8f ? 330f : 262f) * 0.4f * Mathf.Exp(-(t % 0.4f) * 4f));
        start = Clip(0.5f, (t, n) => Tone(t, t < 0.17f ? 523f : t < 0.34f ? 659f : 784f) * 0.4f * Mathf.Exp(-(t % 0.17f) * 7f));
        record = Clip(1.0f, (t, n) => (Tone(t, 659f + Mathf.Floor(t / 0.125f) * 110f) * 0.35f) * Mathf.Exp(-(t % 0.125f) * 9f));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static float Tone(float t, float freq) => Mathf.Sin(t * Mathf.PI * 2f * freq);

    private static AudioClip Clip(float seconds, System.Func<float, float, float> sample)
    {
        const int rate = 44100;
        var data = new float[(int)(rate * seconds)];
        var rng = new System.Random(7);
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Clamp(sample(i / (float)rate, (float)(rng.NextDouble() * 2 - 1)), -1f, 1f) * 0.6f;
        }
        var clip = AudioClip.Create("sfx", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void Play(AudioClip clip, float volume = 0.6f, float pitch = 1f)
    {
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    // Pop pitch climbs with the combo.
    public static void Pop(int combo) => Instance?.Play(Instance.pop, 0.7f, 0.9f + combo * 0.08f);
    public static void Golden() => Instance?.Play(Instance.golden, 0.7f);
    public static void Boom() => Instance?.Play(Instance.boom, 0.8f);
    public static void Flap() => Instance?.Play(Instance.flap, 0.5f, Random.Range(0.9f, 1.1f));
    public static void Jump() => Instance?.Play(Instance.jump, 0.5f);
    public static void Dash() => Instance?.Play(Instance.dash, 0.6f);
    public static void Lose() => Instance?.Play(Instance.lose, 0.7f);
    public static void GameOver() => Instance?.Play(Instance.gameOver, 0.7f);
    public static void Start() => Instance?.Play(Instance.start, 0.7f);
    public static void Record() => Instance?.Play(Instance.record, 0.7f);
}

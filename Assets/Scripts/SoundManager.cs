using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// 효과음·BGM 절차 생성 매니저(싱글톤). UITween처럼 처음 접근 시 자동 생성 → 씬 배치/Setup 불필요.
// 모든 소리는 코드로 합성하므로 오디오 에셋이 필요 없음(PhotoSystem 셔터음과 동일한 방식).
// 사용: SoundManager.Play(Sfx.Click);  음소거 토글: M키.
public class SoundManager : MonoBehaviour
{
    static SoundManager _inst;
    public static SoundManager Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("SoundManager");
                _inst = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }

    public enum Sfx { Click, Open, Close, Gold, Heart, Gift, LevelUp, Night }

    // 정적 편의 호출 (널 안전, 어디서든 한 줄)
    public static void Play(Sfx s, float vol = 1f) => Instance.PlaySfx(s, vol);

    const int SR = 44100;
    const float PI2 = 2f * Mathf.PI;

    AudioSource sfxSrc;
    AudioSource bgmSrc;
    readonly Dictionary<Sfx, AudioClip> cache = new Dictionary<Sfx, AudioClip>();

    bool muted;
    bool lastNight;

    void Awake()
    {
        sfxSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;

        bgmSrc = gameObject.AddComponent<AudioSource>();
        bgmSrc.loop = true;
        bgmSrc.playOnAwake = false;
        bgmSrc.volume = 0.18f;
        bgmSrc.clip = BuildBgm();

        muted = PlayerPrefs.GetInt("Muted", 0) == 1;
        if (!muted) bgmSrc.Play();

        // 시작 시 현재 밤낮에 맞춰 둠 → 게임 켜자마자 전환음 울리지 않게
        lastNight = DayNightManager.Instance != null && DayNightManager.Instance.IsNight();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.mKey.wasPressedThisFrame) ToggleMute();

        // 밤이면 BGM을 살짝 느리고 작게 → 차분하게 (낮/밤 부드럽게 전환)
        bool night = DayNightManager.Instance != null && DayNightManager.Instance.IsNight();
        if (night != lastNight)
        {
            if (!muted) Play(Sfx.Night, 0.7f); // 낮밤 전환 시 잔잔한 종소리
            lastNight = night;
        }
        if (!muted)
        {
            float targetVol = night ? 0.12f : 0.18f;
            float targetPitch = night ? 0.92f : 1f;
            bgmSrc.volume = Mathf.Lerp(bgmSrc.volume, targetVol, Time.unscaledDeltaTime * 1.5f);
            bgmSrc.pitch = Mathf.Lerp(bgmSrc.pitch, targetPitch, Time.unscaledDeltaTime * 1.5f);
        }
    }

    public void PlaySfx(Sfx s, float vol = 1f)
    {
        if (muted) return;
        sfxSrc.PlayOneShot(GetClip(s), vol);
    }

    public void ToggleMute()
    {
        muted = !muted;
        PlayerPrefs.SetInt("Muted", muted ? 1 : 0);
        PlayerPrefs.Save();
        if (muted) bgmSrc.Pause();
        else bgmSrc.UnPause();
        UIManager.Instance?.ShowFloatingText(Vector3.zero, muted ? "🔇 음소거" : "🔊 소리 켜짐");
    }

    // ---------- 효과음 합성 ----------
    AudioClip GetClip(Sfx s)
    {
        if (cache.TryGetValue(s, out var c)) return c;
        c = Synth(s);
        cache[s] = c;
        return c;
    }

    // 음 높이(Hz). 펜타토닉 위주라 어떤 조합도 어울림.
    const float C5 = 523.25f, D5 = 587.33f, E5 = 659.25f, G5 = 783.99f, A5 = 880f;
    const float C6 = 1046.5f, D6 = 1174.66f, E6 = 1318.51f, G6 = 1567.98f, A6 = 1760f, C7 = 2093f;

    AudioClip Synth(Sfx s)
    {
        switch (s)
        {
            case Sfx.Click: // 한옥 나무 두드림 같은 짧은 "톡"
                return Render(0.08f, d => { AddTone(d, 0f, 900f, 0.07f, 0.28f, 90f, 0.2f); });

            case Sfx.Open: // 살짝 올라가는 경쾌한 두 음
                return Render(0.26f, d => { AddTone(d, 0f, C6, 0.12f, 0.22f, 12f); AddTone(d, 0.07f, E6, 0.16f, 0.22f, 10f); });

            case Sfx.Close: // 내려가는 두 음
                return Render(0.26f, d => { AddTone(d, 0f, E6, 0.12f, 0.20f, 12f); AddTone(d, 0.07f, C6, 0.16f, 0.20f, 10f); });

            case Sfx.Gold: // 동전 종소리 (또랑또랑 세 음 상승)
                return Render(0.4f, d => { AddTone(d, 0f, G5, 0.2f, 0.3f, 9f); AddTone(d, 0.06f, C6, 0.2f, 0.3f, 8f); AddTone(d, 0.12f, E6, 0.3f, 0.3f, 6f); });

            case Sfx.Heart: // 보드라운 반짝 (높은 종)
                return Render(0.45f, d => { AddTone(d, 0f, A6, 0.35f, 0.18f, 7f, 0.4f); AddTone(d, 0.05f, E6, 0.3f, 0.12f, 7f); });

            case Sfx.Gift: // 따뜻한 확인음 (5도 상승)
                return Render(0.4f, d => { AddTone(d, 0f, C6, 0.18f, 0.26f, 9f); AddTone(d, 0.09f, G6, 0.28f, 0.26f, 7f); });

            case Sfx.LevelUp: // 축하 아르페지오 (펜타토닉 4음 상승)
                return Render(0.85f, d => { AddTone(d, 0f, C5, 0.15f, 0.3f, 8f); AddTone(d, 0.1f, E5, 0.15f, 0.3f, 8f); AddTone(d, 0.2f, G5, 0.18f, 0.3f, 7f); AddTone(d, 0.3f, C6, 0.5f, 0.32f, 4.5f); });

            case Sfx.Night: // 밤 전환 — 잔잔한 낮은 종 (느린 어택)
            default:
                return Render(0.9f, d => { AddTone(d, 0f, A5 / 2f, 0.85f, 0.3f, 3f, 0.5f, 0.06f); AddTone(d, 0.04f, E5, 0.6f, 0.18f, 3.5f, 0.4f, 0.06f); });
        }
    }

    // dur초 길이의 모노 클립을 만들고 fill로 채운 뒤 클리핑 방지 정규화.
    AudioClip Render(float dur, System.Action<float[]> fill)
    {
        int n = Mathf.CeilToInt(SR * dur);
        var data = new float[n];
        fill(data);
        Normalize(data, 0.9f);
        var clip = AudioClip.Create("sfx", n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 부드러운 종/오르골 음 하나를 data에 더함. 어택(클릭 방지) + 지수 감쇠 + 2배음.
    static void AddTone(float[] data, float startSec, float freq, float dur, float amp, float decay, float harm2 = 0.3f, float attack = 0.005f)
    {
        int start = (int)(startSec * SR);
        int len = (int)(dur * SR);
        for (int i = 0; i < len; i++)
        {
            int idx = start + i;
            if (idx < 0 || idx >= data.Length) continue;
            float t = (float)i / SR;
            float a = attack > 0f && t < attack ? t / attack : 1f;
            float env = a * Mathf.Exp(-t * decay);
            float wave = Mathf.Sin(PI2 * freq * t) + harm2 * Mathf.Sin(PI2 * freq * 2f * t);
            data[idx] += wave * env * amp;
        }
    }

    static void Normalize(float[] data, float peak)
    {
        float max = 0f;
        for (int i = 0; i < data.Length; i++) { float a = Mathf.Abs(data[i]); if (a > max) max = a; }
        if (max <= peak || max <= 0f) return;
        float scale = peak / max;
        for (int i = 0; i < data.Length; i++) data[i] *= scale;
    }

    // ---------- BGM 합성 (잔잔한 16초 루프) ----------
    // C장조 펜타토닉 멜로디(오르골) + 4코드 패드 진행. 경계가 0이라 루프 이음새가 매끄럽다.
    AudioClip BuildBgm()
    {
        const int barSec = 4, bars = 4, total = barSec * bars;
        int n = SR * total;
        var data = new float[n];

        // 코드 진행: C - Am - F - G (저음 패드)
        const float C4 = 261.63f, D4 = 293.66f, E4 = 329.63f, F4 = 349.23f, G4 = 392f, A4 = 440f, B4 = 493.88f;
        const float A3 = 220f, F3 = 174.61f, G3 = 196f, B3 = 246.94f;
        float[][] chords =
        {
            new[] { C4, E4, G4 },
            new[] { A3, C4, E4 },
            new[] { F3, A4, C4 },
            new[] { G3, B3, D4 },
        };
        for (int b = 0; b < bars; b++)
            foreach (var f in chords[b])
                AddPad(data, b * barSec, f, barSec, 0.06f);

        // 멜로디: 오르골 음색, 펜타토닉, 듬성듬성 (고정 시드라 매번 동일)
        float[] pent = { C5, D5, E5, G5, A5, C6 };
        var rnd = new System.Random(7);
        float t = 0.5f;
        while (t < total - 0.6f)
        {
            float f = pent[rnd.Next(pent.Length)];
            AddTone(data, t, f, 1.3f, 0.16f, 3.0f);
            t += rnd.Next(2) == 0 ? 1.0f : 2.0f;
        }

        Normalize(data, 0.82f);
        var clip = AudioClip.Create("bgm", n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    // 한 마디 동안 지속되는 부드러운 패드. 양 끝이 0인 봉우리 엔벨로프 → 루프 클릭 없음.
    static void AddPad(float[] data, float startSec, float freq, float dur, float amp)
    {
        int start = (int)(startSec * SR);
        int len = (int)(dur * SR);
        for (int i = 0; i < len; i++)
        {
            int idx = start + i;
            if (idx < 0 || idx >= data.Length) continue;
            float t = (float)i / SR;
            float k = (float)i / len;
            float env = Mathf.Sin(k * Mathf.PI);                 // 봉우리 엔벨로프
            float trem = 0.85f + 0.15f * Mathf.Sin(PI2 * 0.3f * t); // 느린 트레몰로
            float wave = Mathf.Sin(PI2 * freq * t) + 0.5f * Mathf.Sin(Mathf.PI * freq * t); // 따뜻한 저배음
            data[idx] += wave * env * trem * amp;
        }
    }
}

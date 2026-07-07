using UnityEngine;
using UnityEngine.UI;

// 실제 시간 1시간마다 낮↔밤 전환. 화면을 부드럽게 어둡게 덮음.
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Refs")]
    public SpriteRenderer overlay;     // 화면을 덮는 스프라이트
    public Image phaseIcon;            // 낮/밤 아이콘 (해/달)

    [Header("Icons (나중에 이미지로 교체 가능)")]
    public Sprite sunSprite;
    public Sprite moonSprite;

    [Header("Colors")]
    public Color dayColor = new Color(0, 0, 0, 0f);
    public Color nightColor = new Color(0.02f, 0.03f, 0.18f, 0.78f); // 더 어둡게
    public float transitionSpeed = 1.5f;

    bool debugOverride;
    bool debugNight;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // 시작 시 즉시 현재 상태로 맞춤
        if (overlay != null) overlay.color = IsNight() ? nightColor : dayColor;
        UpdatePhaseIcon();
        // 밤 반딧불이 연출 자동 부착(에셋·Setup 불필요)
        if (GetComponent<NightAmbience>() == null) gameObject.AddComponent<NightAmbience>();
        // BGM·효과음 매니저 부팅(자동 생성, 게임 시작과 동시에 BGM 재생)
        _ = SoundManager.Instance;
    }

    void Update()
    {
        // 테스트용: N 키로 밤낮 강제 전환
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.nKey.wasPressedThisFrame)
        {
            debugOverride = true;
            debugNight = !debugNight;
            UpdatePhaseIcon();
        }

        if (overlay != null)
        {
            Color target = IsNight() ? nightColor : dayColor;
            overlay.color = Color.Lerp(overlay.color, target, Time.deltaTime * transitionSpeed);
        }

        UpdatePhaseIcon(); // 실제 시간으로 바뀌어도 반영
    }

    // 실제 시간 기준: 1시간마다 토글 (짝수시=낮, 홀수시=밤)
    public bool IsNight()
    {
        if (debugOverride) return debugNight;
        long hours = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600;
        return (hours % 2) == 1;
    }

    void UpdatePhaseIcon()
    {
        if (phaseIcon == null) return;
        var s = IsNight() ? moonSprite : sunSprite;
        if (s != null && phaseIcon.sprite != s) phaseIcon.sprite = s;
    }
}

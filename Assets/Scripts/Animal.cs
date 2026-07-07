using UnityEngine;
using System.Collections;

// 새 타입은 반드시 맨 뒤에 추가 (int 값이 세이브/사진 메타에 쓰여서 순서 바뀌면 깨짐)
public enum AnimalType { Cat, Dog, Bird, Rabbit, Squirrel, Ghost, Kaiju, Robot }

// 피사체 출현 구역 (AnimalSpawner가 구역별 월드 영역을 가짐). Yard=기본 마당 바닥
public enum SpawnZone { Yard, Porch, Well, Grass, Roof }

// 출현 가능 구역 집합(복수 가능). 스폰 때 이 중 하나를 랜덤 선택
[System.Flags]
public enum ZoneMask
{
    None  = 0,
    Yard  = 1 << 0,
    Porch = 1 << 1,
    Well  = 1 << 2,
    Grass = 1 << 3,
    Roof  = 1 << 4,
}
public enum AffinityLevel { Stranger, Acquaintance, Friend, BestFriend, Soulmate }

public class Animal : MonoBehaviour
{
    [Header("Animal Info")]
    public AnimalType animalType;
    public string animalName;
    [Tooltip("동물별 크기 배율. 스폰 시 기본 4.5배에 곱해짐. 1=기본")]
    public float sizeScale = 1f;
    [Tooltip("출현 가능 구역(복수 가능). 스폰 때 이 중 하나를 랜덤 선택. 기본=마당")]
    public ZoneMask zones = ZoneMask.Yard;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float stayDuration = 30f;

    [Header("Affinity")]
    public int affinity;
    public AffinityLevel AffinityLevel => affinity switch
    {
        < 20 => AffinityLevel.Stranger,
        < 40 => AffinityLevel.Acquaintance,
        < 60 => AffinityLevel.Friend,
        < 80 => AffinityLevel.BestFriend,
        _ => AffinityLevel.Soulmate
    };

    [Header("Costume")]
    public string costumeKey; // 코스튬 그림 키 (예: "mycat"). 비어있으면 코스튬 없음

    Animator animator;
    SpriteRenderer sr;
    bool isLeaving;
    float stayTimer;
    float happyTimer;
    float ghostBaseY;
    bool ghostFloatInit;

    // 한 번 스폰된 피사체는 사진 1회만
    public bool HasBeenPhotographed { get; private set; }
    public void MarkPhotographed() => HasBeenPhotographed = true;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        LoadAffinity();
    }

    void Start()
    {
        stayTimer = stayDuration;
        // 스폰되면 가만히 서 있음 (방향만 랜덤)
        SetWalking(false);
        if (sr != null) sr.flipX = Random.value < 0.5f;
        if (sr != null && animalType == AnimalType.Ghost)
            sr.color = new Color(1f, 1f, 1f, 0.85f); // 유령은 살짝 반투명
        // 도감 등록(실루엣 해제)은 스폰이 아니라 "처음 사진 찍을 때" → PhotoSystem에서 호출
        if (IsMaxed) ApplyDressed(); // 친밀도 MAX면 선물 착용한 모습으로 등장
        StartCoroutine(EntranceAnim()); // 통통 튀며 등장
    }

    // 등장: 작게+투명에서 통통(감쇠 진동) 튀어오르며 최종 크기/투명도로. 스폰 시 외부에서
    // 정해준 localScale·alpha를 목표로 삼는다(유령=0.85, 일반=1).
    IEnumerator EntranceAnim()
    {
        Vector3 target = transform.localScale;
        float targetA = sr != null ? sr.color.a : 1f;
        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            // 빠르게 커지며(sqrt) 그 위에 감쇠하는 진동(통통통)을 얹는다
            float grow = Mathf.Sqrt(k);
            float bounce = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * 0.22f;
            transform.localScale = target * Mathf.Max(0.05f, grow + bounce);
            if (sr != null) { var c = sr.color; c.a = Mathf.Lerp(0f, targetA, Mathf.Min(1f, k * 2f)); sr.color = c; }
            yield return null;
        }
        transform.localScale = target;
        if (sr != null) { var c = sr.color; c.a = targetA; sr.color = c; }
    }

    // 친밀도 100(MAX) 여부
    public bool IsMaxed => affinity >= 100;

    // 선물 착용한 모습(코스튬 .bytes)으로 교체. 그림이 아직 없으면 기본 유지(안전).
    void ApplyDressed()
    {
        var loader = GetComponent<RuntimeImageLoader>();
        if (loader != null) loader.TryLoad(DressedResourceName(loader.resourceName));
    }

    // 기본 그림 리소스명 → 코스튬 리소스명. "{key}_body" → "{key}_dressed_body".
    public static string DressedResourceName(string bodyResource)
    {
        if (string.IsNullOrEmpty(bodyResource)) return null;
        return bodyResource.EndsWith("_body")
            ? bodyResource.Substring(0, bodyResource.Length - 5) + "_dressed_body"
            : bodyResource + "_dressed";
    }

    void Update()
    {
        stayTimer -= Time.deltaTime;
        if (stayTimer <= 0 && !isLeaving) StartCoroutine(LeaveYard());

        UpdateHappyHearts();
        UpdateGhostFloat();
    }

    // 유령은 제자리에서 둥실둥실 떠다님
    void UpdateGhostFloat()
    {
        if (animalType != AnimalType.Ghost) return;
        if (!ghostFloatInit) { ghostBaseY = transform.position.y; ghostFloatInit = true; }
        var p = transform.position;
        p.y = ghostBaseY + Mathf.Sin(Time.time * 1.5f) * 0.15f;
        transform.position = p;
    }

    // ---------- 퇴장 (스르륵 페이드아웃) ----------
    IEnumerator LeaveYard()
    {
        isLeaving = true;
        float t = 0f, dur = 0.7f;
        Color start = sr != null ? sr.color : Color.white;
        Vector3 baseScale = transform.localScale;
        while (t < dur && sr != null)
        {
            t += Time.deltaTime;
            float k = t / dur;
            float a = Mathf.Lerp(start.a, 0f, k);
            sr.color = new Color(start.r, start.g, start.b, a);
            transform.localScale = baseScale * Mathf.Lerp(1f, 0.85f, k); // 살짝 쪼그라들며 사라짐
            yield return null;
        }
        Destroy(gameObject);
    }

    void SetWalking(bool walking)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetBool("isWalking", walking);
    }

    // ---------- 친밀도 행동 ----------
    void UpdateHappyHearts()
    {
        if (AffinityLevel < AffinityLevel.Friend) return;
        happyTimer -= Time.deltaTime;
        if (happyTimer <= 0f)
        {
            happyTimer = Random.Range(6f, 12f);
            ShowReaction("♥");
        }
    }

    IEnumerator HappyJump()
    {
        Vector3 baseScale = transform.localScale;
        float t = 0, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t / dur * Mathf.PI) * 0.12f;
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    public void ReceiveGift(int affinityBonus)
    {
        if (IsMaxed) return; // 가득 찬 친구는 더 받지 않음(드래그쪽에서도 막지만 안전망)
        int before = affinity;
        AddAffinity(affinityBonus);
        SoundManager.Play(SoundManager.Sfx.Gift);
        StartCoroutine(HeartBurst(4));
        StartCoroutine(HappyJump());
        if (before < 100 && affinity >= 100) Graduate(); // 방금 친밀도 MAX 달성
    }

    // 테스트용: 친밀도를 즉시 MAX로 (J키). 코스튬/도감 업그레이드/선물 차단 확인용.
    public void DebugSetMaxAffinity()
    {
        affinity = 100;
        SaveAffinity();
        Graduate();
    }

    // 친밀도 MAX 달성: 선물 착용 모습으로 즉시 변신 + 축하 연출
    void Graduate()
    {
        ApplyDressed();
        UIManager.Instance?.ShowFloatingText(transform.position + Vector3.up,
            $"{animalName} 친밀도 MAX!", new Color(1f, 0.85f, 0.3f));
        StartCoroutine(HeartBurst(8));
    }

    public void AddAffinity(int amount)
    {
        affinity = Mathf.Clamp(affinity + amount, 0, 100);
        SaveAffinity();
        // 친밀도 수치는 도감에서만 확인 (팝업 제거)
    }

    // 머리 위로 하트가 뿅뿅
    IEnumerator HeartBurst(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.3f, 1f), 0);
            UIManager.Instance?.ShowFloatingText(p, "♥", HeartColor);
            yield return new WaitForSeconds(0.08f);
        }
    }

    static readonly Color HeartColor = new Color(1f, 0.45f, 0.65f); // 분홍 하트

    void ShowReaction(string emoji) => UIManager.Instance?.ShowFloatingText(transform.position, emoji, HeartColor);

    // 친밀도는 피사체 개별(animalName 단위). 동백·구름이 각자 따로 친밀도를 쌓음.
    void SaveAffinity() => PlayerPrefs.SetInt($"Affinity_{animalName}", affinity);
    void LoadAffinity() => affinity = PlayerPrefs.GetInt($"Affinity_{animalName}", 0);
}

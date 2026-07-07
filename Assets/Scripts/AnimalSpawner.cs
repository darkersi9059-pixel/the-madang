using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    public static AnimalSpawner Instance { get; private set; }

    [Header("Spawn Settings")]
    public GameObject[] animalPrefabs;
    public float minSpawnInterval = 8f;
    public float maxSpawnInterval = 25f;
    public int maxAnimalsAtOnce = 10;

    [Header("Stay Time (초)")]
    public float minStay = 60f;    // 1분
    public float maxStay = 600f;   // 10분

    [Header("Spawn Area (마당 범위, 세로 화면)")]
    public Vector2 areaMin = new Vector2(-2.5f, -3.2f);
    public Vector2 areaMax = new Vector2(2.5f, -0.2f);
    [Tooltip("피사체 간 최소 간격(작을수록 더 겹침 허용). 약간 겹침 OK, 너무 겹침 방지")]
    public float minSpawnDistance = 0.9f;

    // 특수 구역 월드 좌표 (코드 전용=비직렬화. 수정하면 재컴파일만으로 반영. Z키로 화면 보며 보정)
    Vector2 porchMin = new Vector2(0.8f, -1.3f);
    Vector2 porchMax = new Vector2(2.7f, 0.1f);
    Vector2 wellMin = new Vector2(2.1f, -4.5f);
    Vector2 wellMax = new Vector2(3.1f, -3.5f);
    Vector2 grassMin = new Vector2(-2.3f, -1.6f);
    Vector2 grassMax = new Vector2(0.4f, -0.3f);
    Vector2 roofMin = new Vector2(-2.6f, 1.7f);
    Vector2 roofMax = new Vector2(2.6f, 3.8f);

    int currentAnimals;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        LoadZones(); // 사용자가 편집·저장한 구역 좌표 불러오기
        // 시작 시 2~3마리 배치
        int initial = Random.Range(2, 4);
        for (int i = 0; i < initial; i++) SpawnAnimal();
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            if (currentAnimals < maxAnimalsAtOnce) SpawnAnimal();
        }
    }

    // 테스트용: B키를 누르면 손그림 고양이 "동백"을 즉시 호출 (C는 촬영, N은 밤낮이라 충돌 피함)
    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.bKey.wasPressedThisFrame)
        {
            Debug.Log("[테스트] B키 입력 감지 → 동백 호출 시도");
            SpawnMyCat();
        }
        if (kb != null && kb.gKey.wasPressedThisFrame)
        {
            Debug.Log("[테스트] G키 입력 감지 → 랜덤 유령 호출 시도");
            SpawnRandomGhost();
        }
        if (kb != null && kb.kKey.wasPressedThisFrame)
        {
            Debug.Log("[테스트] K키 입력 감지 → 랜덤 거대 괴수·로봇 호출 시도");
            SpawnRandomGiant();
        }
        if (kb != null && kb.jKey.wasPressedThisFrame)
        {
            Debug.Log("[테스트] J키 → 마당의 모든 피사체 친밀도 MAX(코스튬/도감 업그레이드 확인용)");
            foreach (var a in FindObjectsByType<Animal>(FindObjectsSortMode.None)) a?.DebugSetMaxAffinity();
        }
        if (kb != null && kb.zKey.wasPressedThisFrame) ToggleZoneDebug();
        HandleZoneEdit(kb);
    }

    // 테스트용: K키로 거대 괴수·로봇 한 마리 즉시 호출 (희귀 무시, 단 룰1=중복은 지킴)
    void SpawnRandomGiant()
    {
        var giants = new List<GameObject>();
        foreach (var p in animalPrefabs)
        {
            if (p == null) continue;
            var a = p.GetComponent<Animal>();
            if (a != null && (a.animalType == AnimalType.Kaiju || a.animalType == AnimalType.Robot)
                && !IsSubjectPresent(a.animalName))
                giants.Add(p);
        }
        if (giants.Count == 0) { Debug.LogWarning("[테스트] 호출할 거대 피사체가 없어요 (전부 이미 등장 중?)"); return; }
        SpawnPrefab(giants[Random.Range(0, giants.Count)]);
    }

    // 테스트용: G키로 유령 한 마리 즉시 호출 (밤낮·희귀 무시, 단 룰1=중복은 지킴)
    void SpawnRandomGhost()
    {
        var ghosts = new List<GameObject>();
        foreach (var p in animalPrefabs)
        {
            if (p == null) continue;
            var a = p.GetComponent<Animal>();
            if (a != null && a.animalType == AnimalType.Ghost && !IsSubjectPresent(a.animalName))
                ghosts.Add(p);
        }
        if (ghosts.Count == 0) { Debug.LogWarning("[테스트] 호출할 유령이 없어요 (전부 이미 등장 중?)"); return; }
        SpawnPrefab(ghosts[Random.Range(0, ghosts.Count)]);
    }

    // 같은 이름의 피사체가 이미 마당에 있는지
    bool IsSubjectPresent(string animalName)
    {
        if (string.IsNullOrEmpty(animalName)) return false;
        foreach (var a in FindObjectsByType<Animal>(FindObjectsSortMode.None))
            if (a != null && a.animalName == animalName) return true;
        return false;
    }

    void SpawnAnimal()
    {
        if (animalPrefabs.Length == 0)
        {
            Debug.LogWarning("AnimalSpawner: 프리팹이 없어요!");
            return;
        }

        var prefab = PickWeightedPrefab();
        SpawnPrefab(prefab);
    }

    // 동백(손그림 고양이)을 찾아 즉시 스폰 (테스트용). 한글 대신 costumeKey로 매칭해 인코딩 문제 회피
    void SpawnMyCat()
    {
        foreach (var prefab in animalPrefabs)
        {
            if (prefab == null) continue;
            var animal = prefab.GetComponent<Animal>();
            if (animal != null && animal.costumeKey == "mycat")
            {
                if (IsSubjectPresent(animal.animalName)) { Debug.Log("[테스트] 동백이 이미 마당에 있어요"); return; }
                Debug.Log("[테스트] 동백(mycat) 프리팹 찾음 → 스폰");
                SpawnPrefab(prefab);
                return;
            }
        }
        Debug.LogWarning($"[테스트] 동백(mycat) 프리팹을 못 찾음! animalPrefabs 개수={animalPrefabs.Length}");
    }

    void SpawnPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        // 피사체의 출현 가능 구역 중 하나를 랜덤 선택 → 그 안에서 겹치지 않는 자리
        var pAnimal = prefab.GetComponent<Animal>();
        GetZoneRect(PickZone(pAnimal != null ? pAnimal.zones : ZoneMask.Yard), out var zmn, out var zmx);
        var spawnPos = PickSpawnPosition(zmn, zmx);

        var obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        var animal = obj.GetComponent<Animal>();

        // 동물별 크기: 기본 2.7배(이전 4.5의 60%) × 동물 sizeScale
        float s = 2.7f * (animal != null ? animal.sizeScale : 1f);
        obj.transform.localScale = new Vector3(s, s, 1f);

        // 앞에 있는(아래쪽) 피사체가 위로 그려지도록 y에 따라 정렬.
        // 기준값 400: 지붕(y 양수)에 뜨는 유령도 음수가 되지 않아 배경(0) 뒤로 숨지 않도록.
        // 범위 = 우물 y≈-4.5(→850) ~ 지붕 y≈3.8(→20). 둘 다 배경 0 위, 밤 오버레이 900 아래.
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-spawnPos.y * 100) + 400;

        currentAnimals++;
        if (animal != null) animal.stayDuration = Random.Range(minStay, maxStay);
        StartCoroutine(TrackAnimal(obj));
    }

    // 룰2: 주어진 구역 안에서 기존 피사체와 너무 겹치지 않는 위치 선택(약간 겹침 허용).
    Vector3 PickSpawnPosition(Vector2 mn, Vector2 mx)
    {
        var animals = FindObjectsByType<Animal>(FindObjectsSortMode.None);
        Vector3 best = RandomPos(mn, mx);
        float bestMinDist = -1f;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector3 cand = RandomPos(mn, mx);
            float minDist = float.MaxValue;
            foreach (var a in animals)
                if (a != null) minDist = Mathf.Min(minDist, Vector2.Distance(cand, a.transform.position));
            if (animals.Length == 0 || minDist >= minSpawnDistance) return cand;
            if (minDist > bestMinDist) { bestMinDist = minDist; best = cand; }
        }
        return best;
    }

    Vector3 RandomPos(Vector2 mn, Vector2 mx) => new Vector3(
        Random.Range(mn.x, mx.x), Random.Range(mn.y, mx.y), 0f);

    // 출현 가능 구역 마스크 중 하나를 랜덤 선택
    readonly List<SpawnZone> zonePick = new List<SpawnZone>();
    SpawnZone PickZone(ZoneMask mask)
    {
        zonePick.Clear();
        if ((mask & ZoneMask.Yard)  != 0) zonePick.Add(SpawnZone.Yard);
        if ((mask & ZoneMask.Porch) != 0) zonePick.Add(SpawnZone.Porch);
        if ((mask & ZoneMask.Well)  != 0) zonePick.Add(SpawnZone.Well);
        if ((mask & ZoneMask.Grass) != 0) zonePick.Add(SpawnZone.Grass);
        if ((mask & ZoneMask.Roof)  != 0) zonePick.Add(SpawnZone.Roof);
        if (zonePick.Count == 0) return SpawnZone.Yard;
        return zonePick[Random.Range(0, zonePick.Count)];
    }

    // 구역 → 월드 영역
    void GetZoneRect(SpawnZone z, out Vector2 mn, out Vector2 mx)
    {
        switch (z)
        {
            case SpawnZone.Porch: mn = porchMin; mx = porchMax; break;
            case SpawnZone.Well:  mn = wellMin;  mx = wellMax;  break;
            case SpawnZone.Grass: mn = grassMin; mx = grassMax; break;
            case SpawnZone.Roof:  mn = roofMin;  mx = roofMax;  break;
            default:              mn = areaMin;  mx = areaMax;  break; // Yard
        }
    }

    // ================= 구역 표시/편집 =================
    // Z키: 구역 박스 잠깐 보기.  V키: 구역 편집 모드(드래그로 영역 그리기 → PlayerPrefs 저장).
    GameObject zoneDebugRoot;
    void ToggleZoneDebug()
    {
        if (zoneDebugRoot != null) { Destroy(zoneDebugRoot); zoneDebugRoot = null; return; }
        zoneDebugRoot = new GameObject("ZoneDebug");
        foreach (SpawnZone z in System.Enum.GetValues(typeof(SpawnZone)))
        {
            GetZoneRect(z, out var mn, out var mx);
            MakeBox(zoneDebugRoot.transform, mn, mx, ZoneColor(z, 0.28f), 9000);
        }
    }

    // ---- 편집 모드 ----
    bool zoneEdit;
    SpawnZone editZone = SpawnZone.Porch;
    bool dragging;
    Vector3 dragStart;
    GameObject editRoot, previewBox;

    void HandleZoneEdit(UnityEngine.InputSystem.Keyboard kb)
    {
        if (kb != null && kb.vKey.wasPressedThisFrame) ToggleZoneEdit();
        if (!zoneEdit) return;

        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) { editZone = SpawnZone.Yard;  RefreshEditOverlay(); }
            if (kb.digit2Key.wasPressedThisFrame) { editZone = SpawnZone.Porch; RefreshEditOverlay(); }
            if (kb.digit3Key.wasPressedThisFrame) { editZone = SpawnZone.Well;  RefreshEditOverlay(); }
            if (kb.digit4Key.wasPressedThisFrame) { editZone = SpawnZone.Grass; RefreshEditOverlay(); }
            if (kb.digit5Key.wasPressedThisFrame) { editZone = SpawnZone.Roof;  RefreshEditOverlay(); }
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;
        Vector3 wp = ScreenToWorld(mouse.position.ReadValue());
        if (mouse.leftButton.wasPressedThisFrame) { dragging = true; dragStart = wp; }
        if (dragging) UpdatePreview(dragStart, wp);
        if (dragging && mouse.leftButton.wasReleasedThisFrame)
        {
            dragging = false;
            SetZoneRect(editZone, dragStart, wp);
            if (previewBox != null) { Destroy(previewBox); previewBox = null; }
            RefreshEditOverlay();
            GetZoneRect(editZone, out var mn, out var mx);
            Debug.Log($"[구역] {editZone} = ({mn.x:0.00},{mn.y:0.00})~({mx.x:0.00},{mx.y:0.00})");
        }
    }

    void ToggleZoneEdit()
    {
        zoneEdit = !zoneEdit;
        if (zoneEdit) RefreshEditOverlay();
        else
        {
            if (editRoot != null) Destroy(editRoot); editRoot = null;
            if (previewBox != null) Destroy(previewBox); previewBox = null;
            dragging = false;
        }
    }

    void RefreshEditOverlay()
    {
        if (editRoot != null) Destroy(editRoot);
        editRoot = new GameObject("ZoneEdit");
        foreach (SpawnZone z in System.Enum.GetValues(typeof(SpawnZone)))
        {
            GetZoneRect(z, out var mn, out var mx);
            float a = (z == editZone) ? 0.5f : 0.16f;
            MakeBox(editRoot.transform, mn, mx, ZoneColor(z, a), 9000);
        }
    }

    void UpdatePreview(Vector3 a, Vector3 b)
    {
        Vector2 mn = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        Vector2 mx = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        if (previewBox == null) previewBox = MakeBox(null, mn, mx, new Color(1f, 1f, 1f, 0.45f), 9500);
        previewBox.transform.position = new Vector3((mn.x + mx.x) * 0.5f, (mn.y + mx.y) * 0.5f, -1.2f);
        previewBox.transform.localScale = new Vector3(Mathf.Max(0.02f, mx.x - mn.x), Mathf.Max(0.02f, mx.y - mn.y), 1f);
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        var cam = Camera.main;
        if (cam == null) return Vector3.zero;
        var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
        w.z = 0f; return w;
    }

    void OnGUI()
    {
        if (!zoneEdit) return;
        GetZoneRect(editZone, out var mn, out var mx);
        var style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.UpperLeft };
        style.normal.textColor = Color.white;
        string txt = $"[ZONE EDIT]  selected: {editZone}\nDrag to draw the area.\n1 Yard  2 Porch  3 Well  4 Grass  5 Roof    |    V = exit\ncurrent: ({mn.x:0.0},{mn.y:0.0}) ~ ({mx.x:0.0},{mx.y:0.0})";
        GUI.Label(new Rect(16, 16, 600, 150), txt, style);
    }

    // ---- 공용: 박스/색/저장 ----
    GameObject MakeBox(Transform parent, Vector2 mn, Vector2 mx, Color col, int order)
    {
        var go = new GameObject("ZoneBox");
        if (parent != null) go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ZoneWhiteSprite();
        sr.color = col;
        sr.sortingOrder = order;
        go.transform.position = new Vector3((mn.x + mx.x) * 0.5f, (mn.y + mx.y) * 0.5f, -1f);
        go.transform.localScale = new Vector3(Mathf.Abs(mx.x - mn.x), Mathf.Abs(mx.y - mn.y), 1f);
        return go;
    }

    Color ZoneColor(SpawnZone z, float a) => z switch
    {
        SpawnZone.Yard  => new Color(0.2f, 1f,   0.2f, a),
        SpawnZone.Porch => new Color(0.2f, 0.6f, 1f,   a),
        SpawnZone.Well  => new Color(1f,   0.6f, 0.1f, a),
        SpawnZone.Grass => new Color(1f,   1f,   0.2f, a),
        SpawnZone.Roof  => new Color(1f,   0.3f, 1f,   a),
        _ => new Color(1f, 1f, 1f, a)
    };

    void SetZoneRect(SpawnZone z, Vector3 a, Vector3 b)
    {
        Vector2 mn = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
        Vector2 mx = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        switch (z)
        {
            case SpawnZone.Yard:  areaMin = mn;  areaMax = mx;  break;
            case SpawnZone.Porch: porchMin = mn; porchMax = mx; break;
            case SpawnZone.Well:  wellMin = mn;  wellMax = mx;  break;
            case SpawnZone.Grass: grassMin = mn; grassMax = mx; break;
            case SpawnZone.Roof:  roofMin = mn;  roofMax = mx;  break;
        }
        string k = "zone_" + z;
        PlayerPrefs.SetFloat(k + "_minx", mn.x); PlayerPrefs.SetFloat(k + "_miny", mn.y);
        PlayerPrefs.SetFloat(k + "_maxx", mx.x); PlayerPrefs.SetFloat(k + "_maxy", mx.y);
        PlayerPrefs.Save();
    }

    void LoadZones()
    {
        LoadZone(SpawnZone.Yard,  ref areaMin,  ref areaMax);
        LoadZone(SpawnZone.Porch, ref porchMin, ref porchMax);
        LoadZone(SpawnZone.Well,  ref wellMin,  ref wellMax);
        LoadZone(SpawnZone.Grass, ref grassMin, ref grassMax);
        LoadZone(SpawnZone.Roof,  ref roofMin,  ref roofMax);
    }

    void LoadZone(SpawnZone z, ref Vector2 mn, ref Vector2 mx)
    {
        string k = "zone_" + z;
        if (!PlayerPrefs.HasKey(k + "_minx")) return;
        mn = new Vector2(PlayerPrefs.GetFloat(k + "_minx"), PlayerPrefs.GetFloat(k + "_miny"));
        mx = new Vector2(PlayerPrefs.GetFloat(k + "_maxx"), PlayerPrefs.GetFloat(k + "_maxy"));
    }

    static Sprite zoneWhite;
    static Sprite ZoneWhiteSprite()
    {
        if (zoneWhite != null) return zoneWhite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white); tex.Apply();
        zoneWhite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return zoneWhite;
    }

    // 친밀도가 높은 종일수록 더 자주 등장
    GameObject PickWeightedPrefab()
    {
        if (animalPrefabs.Length == 0) return null;
        bool night = DayNightManager.Instance == null || DayNightManager.Instance.IsNight();

        // 룰1: 같은 피사체(이름) 동시 출현 금지 — 이미 마당에 있는 이름은 후보에서 제외
        var present = new HashSet<string>();
        foreach (var a in FindObjectsByType<Animal>(FindObjectsSortMode.None))
            if (a != null && !string.IsNullOrEmpty(a.animalName)) present.Add(a.animalName);

        float total = 0f;
        var weights = new float[animalPrefabs.Length];
        for (int i = 0; i < animalPrefabs.Length; i++)
        {
            var animal = animalPrefabs[i] != null ? animalPrefabs[i].GetComponent<Animal>() : null;
            if (animal == null) { weights[i] = 0f; continue; }

            // 룰1: 이미 등장 중인 피사체는 제외
            if (present.Contains(animal.animalName)) { weights[i] = 0f; continue; }

            // 유령은 밤에만 등장, 그리고 희귀하게
            if (animal.animalType == AnimalType.Ghost && !night) { weights[i] = 0f; continue; }

            int affinity = PlayerPrefs.GetInt($"Affinity_{animal.animalName}", 0);
            float w = 10f + affinity; // 친밀도 0 → 10, 100 → 110
            if (animal.animalType == AnimalType.Ghost) w *= 0.4f; // 희귀하게
            // 거대 괴수·로봇은 밤낮 무관하게 등장하되 아주 드물게(깜짝 등장)
            if (animal.animalType == AnimalType.Kaiju || animal.animalType == AnimalType.Robot) w *= 0.35f;
            weights[i] = w;
            total += w;
        }
        if (total <= 0f) return null;

        float r = Random.Range(0f, total);
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0f) continue;
            r -= weights[i];
            if (r <= 0f) return animalPrefabs[i];
        }
        for (int i = weights.Length - 1; i >= 0; i--)   // 안전망
            if (weights[i] > 0f) return animalPrefabs[i];
        return null;
    }

    IEnumerator TrackAnimal(GameObject animal)
    {
        yield return new WaitUntil(() => animal == null);
        currentAnimals--;
    }
}

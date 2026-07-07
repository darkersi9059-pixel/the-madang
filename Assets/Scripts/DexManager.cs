using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DexManager : MonoBehaviour
{
    public static DexManager Instance { get; private set; }

    [Header("Dex UI")]
    public GameObject dexPanel;
    public Transform gridContainer;
    public TMP_Text titleText;
    public TMP_FontAsset font;

    public struct Entry
    {
        public string name; public AnimalType type; public string resourceName;
        public Entry(string n, AnimalType t, string r) { name = n; type = t; resourceName = r; }
    }

    // 도감 명단은 스폰 명단(AnimalSpawner.animalPrefabs = 손그림 프리팹)에서 런타임에 도출한다.
    // 손그림 동물을 새로 만들어 명단에 추가하면 도감/앨범에 자동 등장.
    // (참고) 예전 에셋팩 동물: 나비/야옹이/치즈/까망이/흰둥이/호랑이(고양이),
    //        골든이/아키타/그레이트/슈나우저/버나드/허스키(강아지) — 프리팹은 파일로 남아있음.
    readonly List<Entry> entries = new();

    // 선택된 카테고리 필터 (null = 전체)
    AnimalType? selectedType;
    GameObject tabRow;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 스폰 명단의 손그림 프리팹들로 도감 엔트리를 새로 구성
    void BuildEntries()
    {
        entries.Clear();
        var spawner = AnimalSpawner.Instance;
        if (spawner == null || spawner.animalPrefabs == null) return;
        foreach (var p in spawner.animalPrefabs)
        {
            if (p == null) continue;
            var a = p.GetComponent<Animal>();
            if (a == null || string.IsNullOrEmpty(a.animalName)) continue;
            var loader = p.GetComponent<RuntimeImageLoader>();
            string res = loader != null ? loader.resourceName : null;
            entries.Add(new Entry(a.animalName, a.animalType, res));
        }
    }

    public void Register(string animalName, AnimalType type)
    {
        if (string.IsNullOrEmpty(animalName)) return;
        if (PlayerPrefs.GetInt(DexKey(animalName), 0) == 1) return;

        PlayerPrefs.SetInt(DexKey(animalName), 1);
        PlayerPrefs.Save();
        UIManager.Instance?.ShowFloatingText(Vector3.zero, $"도감 등록! {animalName}");
    }

    public bool IsMet(string animalName) => PlayerPrefs.GetInt(DexKey(animalName), 0) == 1;
    static string DexKey(string name) => $"Dex_{name}";

    public int MetCount()
    {
        int c = 0;
        foreach (var e in entries) if (IsMet(e.name)) c++;
        return c;
    }

    // 실제 도감 엔트리가 존재하는 카테고리들 (표시 순서대로). 앨범 탭도 이걸 공유.
    public List<AnimalType> ActiveCategories()
    {
        BuildEntries();
        var result = new List<AnimalType>();
        foreach (var t in Categories.Order)
            foreach (var e in entries)
                if (e.type == t) { result.Add(t); break; }
        return result;
    }

    public void ToggleDex()
    {
        if (dexPanel == null) return;
        if (dexPanel.activeSelf) { UITween.Instance.PopClose(dexPanel); return; }
        UITween.Instance.PopOpen(dexPanel); // 먼저 활성화(통통 등장)
        Rebuild();
    }

    void Rebuild()
    {
        BuildEntries();
        BuildTabs();

        if (gridContainer == null) return;
        foreach (Transform c in gridContainer) Destroy(c.gameObject);

        // 선택 카테고리로 필터
        var shown = new List<Entry>();
        foreach (var e in entries)
            if (!selectedType.HasValue || e.type == selectedType.Value) shown.Add(e);

        int met = 0;
        foreach (var e in shown) if (IsMet(e.name)) met++;
        string catName = selectedType.HasValue ? Categories.Name(selectedType.Value) : "전체";
        if (titleText != null) titleText.text = $"도감 · {catName} {met}/{shown.Count}";

        foreach (var e in shown) CreateCard(e);
        ResetScroll();
    }

    void BuildTabs()
    {
        if (dexPanel == null) return;
        if (tabRow != null) Destroy(tabRow);

        var tabs = new List<CategoryTabBar.Tab>();
        tabs.Add(new CategoryTabBar.Tab($"전체 {MetCount()}/{entries.Count}",
            () => { selectedType = null; Rebuild(); }));

        var cats = ActiveCategories();
        int selectedIndex = 0;
        for (int i = 0; i < cats.Count; i++)
        {
            var type = cats[i];
            int total = 0, met = 0;
            foreach (var e in entries)
                if (e.type == type) { total++; if (IsMet(e.name)) met++; }
            tabs.Add(new CategoryTabBar.Tab($"{Categories.Name(type)} {met}/{total}",
                () => { selectedType = type; Rebuild(); }));
            if (selectedType.HasValue && selectedType.Value == type) selectedIndex = i + 1;
        }

        tabRow = CategoryTabBar.Build(dexPanel.transform, tabs, selectedIndex, font);
    }

    // 도감을 열 때마다 스크롤을 맨 위로
    void ResetScroll()
    {
        var sr = gridContainer != null ? gridContainer.GetComponentInParent<ScrollRect>() : null;
        if (sr != null) { Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = 1f; }
    }

    void CreateCard(Entry e)
    {
        bool met = IsMet(e.name);

        var card = new GameObject("DexCard");
        card.transform.SetParent(gridContainer, false);
        var bg = card.AddComponent<Image>();
        bg.color = new Color(0.93f, 0.92f, 0.86f); // 카드 배경(통일, 밝은 크림)

        int affinity = PlayerPrefs.GetInt($"Affinity_{e.name}", 0);
        bool maxed = met && affinity >= 100;

        // 동물 그림: 미수집=검은 실루엣, 수집=원래 컬러, MAX=선물 착용 모습(그림 있으면)
        var sprite = maxed ? (BodyImage.Load(Animal.DressedResourceName(e.resourceName)) ?? BodyImage.Load(e.resourceName))
                           : BodyImage.Load(e.resourceName);
        if (sprite != null)
        {
            var imgObj = new GameObject("Body");
            imgObj.transform.SetParent(card.transform, false);
            var img = imgObj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = met ? Color.white : new Color(0.12f, 0.12f, 0.14f, 1f);
            var ir = imgObj.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.12f, 0.32f);
            ir.anchorMax = new Vector2(0.88f, 0.95f);
            ir.offsetMin = Vector2.zero; ir.offsetMax = Vector2.zero;
        }

        // 이름/친밀도 라벨 (미수집은 ???)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(card.transform, false);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = met ? (maxed ? $"{e.name}\n♥ MAX ✨" : $"{e.name}\n♥ {affinity}/100") : "???";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = maxed ? new Color(0.85f, 0.6f, 0.1f)
                  : met ? new Color(0.15f, 0.12f, 0.1f) : new Color(0.45f, 0.45f, 0.48f);
        if (font != null) tmp.font = font;
        var rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.3f);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
}

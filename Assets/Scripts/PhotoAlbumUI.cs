using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PhotoAlbumUI : MonoBehaviour
{
    public static PhotoAlbumUI Instance { get; private set; }

    [Header("Album UI")]
    public GameObject albumPanel;
    public Transform gridContainer;   // GridLayoutGroup이 붙은 컨테이너
    public TMP_FontAsset font;

    // 선택된 카테고리 필터 (null = 전체)
    AnimalType? selectedType;
    GameObject tabRow;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ToggleAlbum()
    {
        if (albumPanel == null) return;
        if (albumPanel.activeSelf) { UITween.Instance.PopClose(albumPanel); return; }
        UITween.Instance.PopOpen(albumPanel); // 먼저 활성화(통통 등장)
        Rebuild();
    }

    void Rebuild()
    {
        var photos = PhotoStorage.AllMeta(); // 파일에 저장된 사진들 (최신순)
        BuildTabs(photos);

        if (gridContainer == null) return;
        foreach (Transform c in gridContainer) Destroy(c.gameObject);

        var filtered = new List<PhotoMeta>();
        foreach (var p in photos)
            if (!selectedType.HasValue || p.animalType == (int)selectedType.Value) filtered.Add(p);

        if (filtered.Count == 0)
        {
            CreateEmptyNotice();
            return;
        }

        foreach (var meta in filtered)
            CreatePhotoCard(meta);
        ResetScroll();
    }

    void BuildTabs(List<PhotoMeta> photos)
    {
        if (albumPanel == null) return;
        if (tabRow != null) Destroy(tabRow);

        var tabs = new List<CategoryTabBar.Tab>();
        tabs.Add(new CategoryTabBar.Tab($"전체 {photos.Count}",
            () => { selectedType = null; Rebuild(); }));

        var cats = DexManager.Instance != null
            ? DexManager.Instance.ActiveCategories()
            : new List<AnimalType>();
        int selectedIndex = 0;
        for (int i = 0; i < cats.Count; i++)
        {
            var type = cats[i];
            int count = 0;
            foreach (var p in photos) if (p.animalType == (int)type) count++;
            tabs.Add(new CategoryTabBar.Tab($"{Categories.Name(type)} {count}",
                () => { selectedType = type; Rebuild(); }));
            if (selectedType.HasValue && selectedType.Value == type) selectedIndex = i + 1;
        }

        tabRow = CategoryTabBar.Build(albumPanel.transform, tabs, selectedIndex, font);
    }

    // 앨범을 열 때마다 스크롤을 맨 위로
    void ResetScroll()
    {
        var sr = gridContainer != null ? gridContainer.GetComponentInParent<ScrollRect>() : null;
        if (sr != null) { Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = 1f; }
    }

    void CreateEmptyNotice()
    {
        var go = new GameObject("EmptyNotice");
        go.transform.SetParent(gridContainer, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = selectedType.HasValue
            ? $"아직 {Categories.Name(selectedType.Value)} 사진이 없어요!"
            : "아직 찍은 사진이 없어요!";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
    }

    void CreatePhotoCard(PhotoMeta photo)
    {
        // 카드 배경
        var card = new GameObject("PhotoCard");
        card.transform.SetParent(gridContainer, false);
        var bg = card.AddComponent<Image>();
        bg.color = RarityColor((PhotoRarity)photo.rarity);

        // 사진 이미지 (파일에서 로드)
        var imgObj = new GameObject("Photo");
        imgObj.transform.SetParent(card.transform, false);
        var img = imgObj.AddComponent<Image>();
        var sprite = PhotoStorage.LoadSprite(photo.fileName);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
        }
        var imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.1f, 0.3f);
        imgRect.anchorMax = new Vector2(0.9f, 0.95f);
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;

        // 라벨(이름 + 가격)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(card.transform, false);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{photo.animalName}\n{photo.score}G";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        if (font != null) tmp.font = font;
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0.3f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    Color RarityColor(PhotoRarity r) => r switch
    {
        PhotoRarity.Epic => new Color(1f, 0.85f, 0.3f),   // 금색
        PhotoRarity.Rare => new Color(0.6f, 0.8f, 1f),    // 파랑
        _ => Color.white
    };
}

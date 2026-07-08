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

        // 카드를 누르면 사진 확대 보기
        var cardBtn = card.AddComponent<Button>();
        cardBtn.transition = Selectable.Transition.None;
        cardBtn.onClick.AddListener(() => ShowEnlarged(photo));

        // 사진 이미지 (파일에서 로드)
        var imgObj = new GameObject("Photo");
        imgObj.transform.SetParent(card.transform, false);
        var img = imgObj.AddComponent<Image>();
        img.raycastTarget = false; // 클릭은 카드 버튼이 받도록
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
        tmp.raycastTarget = false; // 클릭은 카드 버튼이 받도록
        if (font != null) tmp.font = font;
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0.3f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    // ---------- 사진 확대 보기 (카드를 누르면 전체화면으로 크게) ----------
    GameObject enlargedView;

    void ShowEnlarged(PhotoMeta photo)
    {
        CloseEnlarged();
        var canvas = albumPanel != null ? albumPanel.GetComponentInParent<Canvas>() : null;
        Transform root = canvas != null ? canvas.transform
                       : (albumPanel != null ? albumPanel.transform : transform);

        // 어두운 전체화면 배경 (탭하면 닫힘)
        enlargedView = new GameObject("EnlargedPhoto");
        enlargedView.transform.SetParent(root, false);
        var bgRect = enlargedView.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var bg = enlargedView.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.88f);
        var closeBtn = enlargedView.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(CloseEnlarged);
        enlargedView.transform.SetAsLastSibling(); // 최상단

        // 흰 폴라로이드 프레임
        var frame = new GameObject("Frame");
        frame.transform.SetParent(enlargedView.transform, false);
        var frameImg = frame.AddComponent<Image>();
        frameImg.color = Color.white;
        frameImg.raycastTarget = false;
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(880, 980);
        frameRect.anchoredPosition = new Vector2(0, 40);

        // 큰 사진
        var photoObj = new GameObject("BigPhoto");
        photoObj.transform.SetParent(frame.transform, false);
        var pimg = photoObj.AddComponent<Image>();
        var sprite = PhotoStorage.LoadSprite(photo.fileName);
        if (sprite != null) { pimg.sprite = sprite; pimg.preserveAspect = true; }
        pimg.raycastTarget = false;
        var pr = photoObj.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.05f, 0.14f);
        pr.anchorMax = new Vector2(0.95f, 0.97f);
        pr.offsetMin = Vector2.zero; pr.offsetMax = Vector2.zero;

        // 이름 + 점수
        var infoObj = new GameObject("Info");
        infoObj.transform.SetParent(frame.transform, false);
        var itmp = infoObj.AddComponent<TextMeshProUGUI>();
        itmp.text = $"{photo.animalName}   {photo.score}G";
        itmp.fontSize = 40;
        itmp.alignment = TextAlignmentOptions.Center;
        itmp.color = Color.black;
        itmp.raycastTarget = false;
        if (font != null) itmp.font = font;
        var ir = infoObj.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 0); ir.anchorMax = new Vector2(1, 0.14f);
        ir.offsetMin = Vector2.zero; ir.offsetMax = Vector2.zero;

        // 닫기 안내
        var hintObj = new GameObject("CloseHint");
        hintObj.transform.SetParent(enlargedView.transform, false);
        var htmp = hintObj.AddComponent<TextMeshProUGUI>();
        htmp.text = "탭하면 닫기";
        htmp.fontSize = 30;
        htmp.alignment = TextAlignmentOptions.Center;
        htmp.color = new Color(1f, 1f, 1f, 0.8f);
        htmp.raycastTarget = false;
        if (font != null) htmp.font = font;
        var hr = hintObj.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0);
        hr.pivot = new Vector2(0.5f, 0);
        hr.anchoredPosition = new Vector2(0, 60);
        hr.sizeDelta = new Vector2(400, 50);

        SoundManager.Play(SoundManager.Sfx.Open);
        UITween.Instance?.PopOpen(frame); // 프레임만 통통 등장(배경은 고정)
    }

    void CloseEnlarged()
    {
        if (enlargedView == null) return;
        SoundManager.Play(SoundManager.Sfx.Close);
        Destroy(enlargedView);
        enlargedView = null;
    }

    Color RarityColor(PhotoRarity r) => r switch
    {
        PhotoRarity.Epic => new Color(1f, 0.85f, 0.3f),   // 금색
        PhotoRarity.Rare => new Color(0.6f, 0.8f, 1f),    // 파랑
        _ => Color.white
    };
}

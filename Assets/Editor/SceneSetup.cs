using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SceneSetup : EditorWindow
{
    static TMP_FontAsset koreanFont;

    [MenuItem("TheMadang/Setup Scene")]
    public static void SetupScene()
    {
        koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pretendard SDF.asset");
        if (koreanFont == null) // 폴백
            koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/malgun SDF.asset");
        // 기존 오브젝트 정리
        DestroyIfExists("GameManager");
        DestroyIfExists("AnimalSpawner");
        DestroyIfExists("UIManager");
        DestroyIfExists("PhotoSystem");
        DestroyIfExists("PhotoAlbumUI");
        DestroyIfExists("ShopManager");
        DestroyIfExists("DexManager");
        DestroyIfExists("LevelManager");
        DestroyIfExists("InteractionManager");
        DestroyIfExists("GiftDragController");
        DestroyIfExists("Canvas");
        DestroyIfExists("Background");
        DestroyIfExists("NightOverlay");
        DestroyIfExists("DayNightManager");
        DestroyIfExists("EventSystem");

        // 카메라 설정
        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.53f, 0.81f, 0.98f);
            cam.orthographic = true;
            cam.orthographicSize = 6.5f; // 줌아웃 (시야 넓게)
            cam.transform.position = new Vector3(0, 0, -10);
        }

        // --- GameManager ---
        var gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // --- AnimalSpawner ---
        var spawnerObj = new GameObject("AnimalSpawner");
        var spawner = spawnerObj.AddComponent<AnimalSpawner>();

        // 손그림 프리팹만 스폰 명단에 자동 로드.
        // 손그림 동물 = RuntimeImageLoader를 가진 프리팹(동백 등). 에셋팩 동물(고양이·강아지 12종)은
        // 파일은 그대로 두되 명단에서 제외한다. 에셋팩을 되살리려면 아래 필터 한 줄만 빼고 Setup Scene 재실행.
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Animals" });
        var prefabs = new System.Collections.Generic.List<GameObject>();
        foreach (var guid in prefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Animator")) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            if (prefab.GetComponent<RuntimeImageLoader>() == null) continue; // 손그림만
            prefabs.Add(prefab);
        }
        spawner.animalPrefabs = prefabs.ToArray();

        // --- Background (마당 그림) ---
        var bg = new GameObject("Background");
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bg.transform.position = new Vector3(0, 0, 0);

        string bgPath = "Assets/Sprites/madang_bg_portrait.png";
        EnsureSpriteImport(bgPath);
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
        if (bgSprite == null)
            bgSprite = System.Linq.Enumerable.FirstOrDefault(
                System.Linq.Enumerable.OfType<Sprite>(AssetDatabase.LoadAllAssetsAtPath(bgPath)));
        if (bgSprite != null)
        {
            bgSr.sprite = bgSprite;
            bgSr.sortingOrder = 0;
            // 카메라 화면을 꽉 채우도록 비율 유지하며 스케일
            float spriteW = bgSprite.bounds.size.x;
            float spriteH = bgSprite.bounds.size.y;
            float camH = cam.orthographicSize * 2f;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.78f;
            float camW = camH * aspect;
            float scale = Mathf.Max(camW / spriteW, camH / spriteH) * 1.05f;
            bg.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            // 폴백: 단색
            bgSr.sprite = CreateColorSprite(new Color(0.6f, 0.85f, 0.5f));
            bg.transform.localScale = new Vector3(30, 20, 1);
            Debug.LogWarning($"배경 이미지를 찾지 못했어요: {bgPath}");
        }
        // 런타임에 원본 PNG를 직접 디코딩해 덮어씀 (임포트 체커보드 문제 회피)
        bg.AddComponent<RuntimeImageLoader>().resourceName = "madang_bg_portrait";

        // --- EventSystem (UI 클릭 감지에 필수) ---
        var eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // --- UI Canvas ---
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // 세로 기준
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 하단 버튼 바 (반투명) - 버튼이 배경과 분리되어 잘 보이게
        var bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(canvasObj.transform, false);
        var bbImg = bottomBar.AddComponent<Image>();
        bbImg.color = BarWarm;
        bbImg.raycastTarget = false;
        var bbRect = bottomBar.GetComponent<RectTransform>();
        bbRect.anchorMin = new Vector2(0, 0);
        bbRect.anchorMax = new Vector2(1, 0);
        bbRect.pivot = new Vector2(0.5f, 0);
        bbRect.anchoredPosition = new Vector2(0, 0);
        bbRect.sizeDelta = new Vector2(0, 175);

        // 골드 텍스트
        var goldObj = CreateTextObject("GoldText", canvasObj.transform, "골드: 100G", 34);
        goldObj.GetComponent<TextMeshProUGUI>().color = GoldTint;
        var goldRect = goldObj.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0, 1);
        goldRect.anchorMax = new Vector2(0, 1);
        goldRect.pivot = new Vector2(0, 1);
        goldRect.anchoredPosition = new Vector2(20, -20);
        goldRect.sizeDelta = new Vector2(200, 50);

        // 친밀도 텍스트
        var affinityObj = CreateTextObject("AffinityText", canvasObj.transform, "", 26);
        var affinityRect = affinityObj.GetComponent<RectTransform>();
        affinityRect.anchorMin = new Vector2(0.5f, 0);
        affinityRect.anchorMax = new Vector2(0.5f, 0);
        affinityRect.pivot = new Vector2(0.5f, 0);
        affinityRect.anchoredPosition = new Vector2(0, 30);
        affinityRect.sizeDelta = new Vector2(300, 50);

        // 카메라 버튼
        var camBtnObj = CreateButtonObject("CameraButton", canvasObj.transform, "카메라");
        UseBakedLabelArt(camBtnObj, "btn_camera");
        var camRect = camBtnObj.GetComponent<RectTransform>();
        camRect.anchorMin = new Vector2(0.5f, 0);
        camRect.anchorMax = new Vector2(0.5f, 0);
        camRect.pivot = new Vector2(0.5f, 0);
        camRect.anchoredPosition = new Vector2(135, 50);
        camRect.sizeDelta = new Vector2(240, 90);

        // 상점 버튼
        var shopBtnObj = CreateButtonObject("ShopButton", canvasObj.transform, "상점");
        UseBakedLabelArt(shopBtnObj, "btn_shop");
        var shopRect = shopBtnObj.GetComponent<RectTransform>();
        shopRect.anchorMin = new Vector2(0.5f, 0);
        shopRect.anchorMax = new Vector2(0.5f, 0);
        shopRect.pivot = new Vector2(0.5f, 0);
        shopRect.anchoredPosition = new Vector2(400, 50);
        shopRect.sizeDelta = new Vector2(240, 90);

        // 뷰파인더 UI (카메라 모드) - 어두운 오버레이
        var viewfinderObj = new GameObject("ViewfinderUI");
        viewfinderObj.transform.SetParent(canvasObj.transform, false);
        var vfImage = viewfinderObj.AddComponent<Image>();
        vfImage.color = new Color(0, 0, 0, 0.25f);
        vfImage.raycastTarget = false;
        StretchFull(viewfinderObj.GetComponent<RectTransform>());

        // 안내 문구
        var hintObj = CreateTextObject("Hint", viewfinderObj.transform, "동물을 프레임 안에 꽉 채워 촬영! (ESC 취소)", 28);
        var hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1);
        hintRect.anchorMax = new Vector2(0.5f, 1);
        hintRect.pivot = new Vector2(0.5f, 1);
        hintRect.anchoredPosition = new Vector2(0, -30);
        hintRect.sizeDelta = new Vector2(820, 60);
        hintObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 마우스를 따라다니는 프레임(브래킷 4개를 담는 컨테이너)
        var frameObj = new GameObject("ViewfinderFrame");
        frameObj.transform.SetParent(viewfinderObj.transform, false);
        frameObj.AddComponent<RectTransform>();
        var frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.sizeDelta = new Vector2(280, 280); // 프레임 크기(픽셀)
        CreateFrameBracket(frameObj.transform, new Vector2(0, 1)); // 좌상
        CreateFrameBracket(frameObj.transform, new Vector2(1, 1)); // 우상
        CreateFrameBracket(frameObj.transform, new Vector2(0, 0)); // 좌하
        CreateFrameBracket(frameObj.transform, new Vector2(1, 0)); // 우하

        viewfinderObj.SetActive(false);

        // 플래시 이미지
        var flashObj = new GameObject("CaptureFlash");
        flashObj.transform.SetParent(canvasObj.transform, false);
        var flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(1, 1, 1, 0);
        var flashRect = flashObj.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashObj.SetActive(false);

        // --- UIManager ---
        var uiManagerObj = new GameObject("UIManager");
        var uiManager = uiManagerObj.AddComponent<UIManager>();
        uiManager.goldText = goldObj.GetComponent<TextMeshProUGUI>();
        uiManager.affinityText = affinityObj.GetComponent<TextMeshProUGUI>();
        uiManager.worldCanvas = canvas;

        // 떠다니는 텍스트 템플릿 (점수/하트/메시지)
        var floatTemplate = CreateTextObject("FloatingTextTemplate", canvasObj.transform, "", 36);
        var ftTmp = floatTemplate.GetComponent<TextMeshProUGUI>();
        ftTmp.alignment = TextAlignmentOptions.Center;
        var ftRect = floatTemplate.GetComponent<RectTransform>();
        ftRect.anchorMin = new Vector2(0.5f, 0.5f);
        ftRect.anchorMax = new Vector2(0.5f, 0.5f);
        ftRect.pivot = new Vector2(0.5f, 0.5f);
        ftRect.sizeDelta = new Vector2(300, 60);
        floatTemplate.SetActive(false);
        uiManager.floatingTextPrefab = floatTemplate;

        // 앨범 버튼
        var albumBtnObj = CreateButtonObject("AlbumButton", canvasObj.transform, "앨범");
        UseBakedLabelArt(albumBtnObj, "btn_album");
        var albumRect = albumBtnObj.GetComponent<RectTransform>();
        albumRect.anchorMin = new Vector2(0.5f, 0);
        albumRect.anchorMax = new Vector2(0.5f, 0);
        albumRect.pivot = new Vector2(0.5f, 0);
        albumRect.anchoredPosition = new Vector2(-135, 50);
        albumRect.sizeDelta = new Vector2(240, 90);

        // 도감 버튼
        var dexBtnObj = CreateButtonObject("DexButton", canvasObj.transform, "도감");
        UseBakedLabelArt(dexBtnObj, "btn_dex");
        var dexBtnRect = dexBtnObj.GetComponent<RectTransform>();
        dexBtnRect.anchorMin = new Vector2(0.5f, 0);
        dexBtnRect.anchorMax = new Vector2(0.5f, 0);
        dexBtnRect.pivot = new Vector2(0.5f, 0);
        dexBtnRect.anchoredPosition = new Vector2(-400, 50);
        dexBtnRect.sizeDelta = new Vector2(240, 90);

        // --- 앨범 패널 (사진 모음) ---
        var albumPanel = new GameObject("AlbumPanel");
        albumPanel.transform.SetParent(canvasObj.transform, false);
        var apImg = albumPanel.AddComponent<Image>();
        apImg.color = PanelWarm;
        StretchBelowTop(albumPanel.GetComponent<RectTransform>(), 150f); // 상단 HUD 아래에 배치

        var albumTitle = CreateTextObject("AlbumTitle", albumPanel.transform, "사진 앨범", 46);
        var atRect = albumTitle.GetComponent<RectTransform>();
        atRect.anchorMin = new Vector2(0.5f, 1);
        atRect.anchorMax = new Vector2(0.5f, 1);
        atRect.pivot = new Vector2(0.5f, 1);
        atRect.anchoredPosition = new Vector2(0, -20);
        atRect.sizeDelta = new Vector2(420, 70);
        albumTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 사진 스크롤 그리드 (뷰포트+마스크+ScrollRect > Content)
        var albumContent = BuildScrollGrid(albumPanel.transform, new Vector2(160, 200), new Vector2(15, 15), 5);

        // 앨범 닫기 버튼
        var albumCloseObj = CreateButtonObject("AlbumClose", albumPanel.transform, "닫기", 24);
        var acRect = albumCloseObj.GetComponent<RectTransform>();
        acRect.anchorMin = new Vector2(0.5f, 0);
        acRect.anchorMax = new Vector2(0.5f, 0);
        acRect.pivot = new Vector2(0.5f, 0);
        acRect.anchoredPosition = new Vector2(0, 24);
        acRect.sizeDelta = new Vector2(180, 66);

        albumPanel.SetActive(false);

        // --- PhotoSystem ---
        var photoObj = new GameObject("PhotoSystem");
        var photoSystem = photoObj.AddComponent<PhotoSystem>();
        photoSystem.cameraViewfinderUI = viewfinderObj;
        photoSystem.captureFlash = flashImg;
        photoSystem.viewfinderFrame = frameRect;
        photoSystem.uiCanvas = canvas;

        // --- PhotoAlbumUI ---
        var albumMgrObj = new GameObject("PhotoAlbumUI");
        var albumUI = albumMgrObj.AddComponent<PhotoAlbumUI>();
        albumUI.albumPanel = albumPanel;
        albumUI.gridContainer = albumContent;
        albumUI.font = koreanFont;

        // --- 상점 패널 ---
        var shopPanel = new GameObject("ShopPanel");
        shopPanel.transform.SetParent(canvasObj.transform, false);
        var spImg = shopPanel.AddComponent<Image>();
        spImg.color = PanelWarm;
        StretchBelowTop(shopPanel.GetComponent<RectTransform>(), 150f); // 상단 HUD 아래에 배치

        var shopTitle = CreateTextObject("ShopTitle", shopPanel.transform, "상점", 46);
        var stRect = shopTitle.GetComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.5f, 1);
        stRect.anchorMax = new Vector2(0.5f, 1);
        stRect.pivot = new Vector2(0.5f, 1);
        stRect.anchoredPosition = new Vector2(0, -20);
        stRect.sizeDelta = new Vector2(420, 70);
        shopTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var shopGrid = new GameObject("ShopGrid");
        shopGrid.transform.SetParent(shopPanel.transform, false);
        var shopGridLayout = shopGrid.AddComponent<GridLayoutGroup>();
        shopGridLayout.cellSize = new Vector2(160, 180);
        shopGridLayout.spacing = new Vector2(15, 15);
        shopGridLayout.padding = new RectOffset(20, 20, 20, 20);
        var shopGridRect = shopGrid.GetComponent<RectTransform>();
        shopGridRect.anchorMin = new Vector2(0, 0);
        shopGridRect.anchorMax = new Vector2(1, 1);
        shopGridRect.offsetMin = new Vector2(20, 80);
        shopGridRect.offsetMax = new Vector2(-20, -100);

        var shopCloseObj = CreateButtonObject("ShopClose", shopPanel.transform, "닫기", 24);
        var scRect = shopCloseObj.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0.5f, 0);
        scRect.anchorMax = new Vector2(0.5f, 0);
        scRect.pivot = new Vector2(0.5f, 0);
        scRect.anchoredPosition = new Vector2(0, 24);
        scRect.sizeDelta = new Vector2(180, 66);

        // 광고 새로고침 버튼
        var adRefreshObj = CreateButtonObject("AdRefresh", shopPanel.transform, "광고 보고 새로고침", 22);
        var arRect = adRefreshObj.GetComponent<RectTransform>();
        arRect.anchorMin = new Vector2(0.5f, 0);
        arRect.anchorMax = new Vector2(0.5f, 0);
        arRect.pivot = new Vector2(0.5f, 0);
        arRect.anchoredPosition = new Vector2(0, 90);
        arRect.sizeDelta = new Vector2(300, 60);

        shopPanel.SetActive(false);

        // --- ShopManager ---
        var shopObj = new GameObject("ShopManager");
        var shopManager = shopObj.AddComponent<ShopManager>();
        shopManager.shopPanel = shopPanel;
        shopManager.itemContainer = shopGrid.transform;
        shopManager.titleText = shopTitle.GetComponent<TextMeshProUGUI>();
        shopManager.font = koreanFont;

        // --- 도감 패널 ---
        var dexPanel = new GameObject("DexPanel");
        dexPanel.transform.SetParent(canvasObj.transform, false);
        var dpImg = dexPanel.AddComponent<Image>();
        dpImg.color = PanelWarm;
        StretchBelowTop(dexPanel.GetComponent<RectTransform>(), 150f); // 상단 HUD 아래에 배치

        var dexTitle = CreateTextObject("DexTitle", dexPanel.transform, "도감", 46);
        var dtRect = dexTitle.GetComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0.5f, 1);
        dtRect.anchorMax = new Vector2(0.5f, 1);
        dtRect.pivot = new Vector2(0.5f, 1);
        dtRect.anchoredPosition = new Vector2(0, -20);
        dtRect.sizeDelta = new Vector2(420, 70);
        dexTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // 도감 스크롤 그리드
        var dexContent = BuildScrollGrid(dexPanel.transform, new Vector2(150, 175), new Vector2(12, 12), 6);

        var dexCloseObj = CreateButtonObject("DexClose", dexPanel.transform, "닫기", 24);
        var dcRect = dexCloseObj.GetComponent<RectTransform>();
        dcRect.anchorMin = new Vector2(0.5f, 0);
        dcRect.anchorMax = new Vector2(0.5f, 0);
        dcRect.pivot = new Vector2(0.5f, 0);
        dcRect.anchoredPosition = new Vector2(0, 24);
        dcRect.sizeDelta = new Vector2(180, 66);

        dexPanel.SetActive(false);

        // --- DexManager ---
        var dexObj = new GameObject("DexManager");
        var dexManager = dexObj.AddComponent<DexManager>();
        dexManager.dexPanel = dexPanel;
        dexManager.gridContainer = dexContent;
        dexManager.titleText = dexTitle.GetComponent<TextMeshProUGUI>();
        dexManager.font = koreanFont;

        // --- 레벨 버튼 (좌하단) ---
        var levelBtnObj = CreateButtonObject("LevelButton", canvasObj.transform, "Lv.1");
        var lvRect = levelBtnObj.GetComponent<RectTransform>();
        lvRect.anchorMin = new Vector2(0, 1);
        lvRect.anchorMax = new Vector2(0, 1);
        lvRect.pivot = new Vector2(0, 1);
        lvRect.anchoredPosition = new Vector2(20, -80);
        lvRect.sizeDelta = new Vector2(160, 60);

        // --- 레벨 패널 (가운데 모달) ---
        var levelPanel = new GameObject("LevelPanel");
        levelPanel.transform.SetParent(canvasObj.transform, false);
        var lpBg = levelPanel.AddComponent<Image>();
        lpBg.color = new Color(0, 0, 0, 0.6f);
        StretchFull(levelPanel.GetComponent<RectTransform>());

        var lpBox = new GameObject("Box");
        lpBox.transform.SetParent(levelPanel.transform, false);
        var lpBoxImg = lpBox.AddComponent<Image>();
        lpBoxImg.color = PanelWarm;
        lpBox.AddComponent<RoundedBox>().cornerRadius = 28; // 모달 박스 둥근 모서리
        var lpBoxRect = lpBox.GetComponent<RectTransform>();
        lpBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        lpBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        lpBoxRect.pivot = new Vector2(0.5f, 0.5f);
        lpBoxRect.sizeDelta = new Vector2(480, 420);

        var lpTitle = CreateTextObject("LevelTitle", lpBox.transform, "레벨", 42);
        var lpTitleRect = lpTitle.GetComponent<RectTransform>();
        lpTitleRect.anchorMin = new Vector2(0.5f, 1); lpTitleRect.anchorMax = new Vector2(0.5f, 1);
        lpTitleRect.pivot = new Vector2(0.5f, 1);
        lpTitleRect.anchoredPosition = new Vector2(0, -20);
        lpTitleRect.sizeDelta = new Vector2(400, 50);
        lpTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var lpInfo = CreateTextObject("LevelInfo", lpBox.transform, "", 26);
        var lpInfoRect = lpInfo.GetComponent<RectTransform>();
        lpInfoRect.anchorMin = new Vector2(0.5f, 0.5f); lpInfoRect.anchorMax = new Vector2(0.5f, 0.5f);
        lpInfoRect.pivot = new Vector2(0.5f, 0.5f);
        lpInfoRect.anchoredPosition = new Vector2(0, 30);
        lpInfoRect.sizeDelta = new Vector2(420, 180);
        lpInfo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var levelUpBtnObj = CreateButtonObject("LevelUpButton", lpBox.transform, "레벨 업!", 26);
        var luRect = levelUpBtnObj.GetComponent<RectTransform>();
        luRect.anchorMin = new Vector2(0.5f, 0); luRect.anchorMax = new Vector2(0.5f, 0);
        luRect.pivot = new Vector2(0.5f, 0);
        luRect.anchoredPosition = new Vector2(0, 88);
        luRect.sizeDelta = new Vector2(220, 64);

        var levelCloseObj = CreateButtonObject("LevelClose", lpBox.transform, "닫기", 24);
        var lcRect = levelCloseObj.GetComponent<RectTransform>();
        lcRect.anchorMin = new Vector2(0.5f, 0); lcRect.anchorMax = new Vector2(0.5f, 0);
        lcRect.pivot = new Vector2(0.5f, 0);
        lcRect.anchoredPosition = new Vector2(0, 22);
        lcRect.sizeDelta = new Vector2(180, 58);

        levelPanel.SetActive(false);

        // --- LevelManager ---
        var levelMgrObj = new GameObject("LevelManager");
        var levelManager = levelMgrObj.AddComponent<LevelManager>();
        levelManager.levelButtonText = levelBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        levelManager.levelPanel = levelPanel;
        levelManager.infoText = lpInfo.GetComponent<TextMeshProUGUI>();
        levelManager.levelUpButton = levelUpBtnObj.GetComponent<Button>();
        levelManager.levelUpButtonText = levelUpBtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // --- 밤 오버레이 (월드 스프라이트, 동물 위/UI 아래) ---
        var nightObj = new GameObject("NightOverlay");
        var nightSr = nightObj.AddComponent<SpriteRenderer>();
        nightSr.sprite = CreateColorSprite(Color.white);
        nightSr.color = new Color(0, 0, 0, 0);
        nightSr.sortingOrder = 900;
        nightObj.transform.position = new Vector3(0, 0, -1);
        nightObj.transform.localScale = new Vector3(60, 40, 1);

        // 낮/밤 아이콘 (우상단) - 나중에 이미지 교체 쉽게 Image로
        var phaseObj = new GameObject("PhaseIcon");
        phaseObj.transform.SetParent(canvasObj.transform, false);
        var phaseImg = phaseObj.AddComponent<Image>();
        var phaseRect = phaseObj.GetComponent<RectTransform>();
        phaseRect.anchorMin = new Vector2(1, 1);
        phaseRect.anchorMax = new Vector2(1, 1);
        phaseRect.pivot = new Vector2(1, 1);
        phaseRect.anchoredPosition = new Vector2(-20, -20);
        phaseRect.sizeDelta = new Vector2(56, 56);

        // --- DayNightManager ---
        var dayNightObj = new GameObject("DayNightManager");
        var dayNight = dayNightObj.AddComponent<DayNightManager>();
        dayNight.overlay = nightSr;
        dayNight.phaseIcon = phaseImg;
        dayNight.sunSprite = CreateSunSprite();
        dayNight.moonSprite = CreateMoonSprite();

        // --- GiftDragController (드래그 선물) ---
        var dragObj = new GameObject("GiftDragController");
        var dragCtrl = dragObj.AddComponent<GiftDragController>();
        dragCtrl.uiCanvas = canvas;
        dragCtrl.font = koreanFont;

        // --- 버튼 연결 ---
        var camBtn = camBtnObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(camBtn.onClick, photoSystem.ToggleCameraMode);

        var shopBtn = shopBtnObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(shopBtn.onClick, shopManager.ToggleShop);

        var albumBtn = albumBtnObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(albumBtn.onClick, albumUI.ToggleAlbum);

        var albumCloseBtn = albumCloseObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(albumCloseBtn.onClick, albumUI.ToggleAlbum);

        var shopCloseBtn = shopCloseObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(shopCloseBtn.onClick, shopManager.ToggleShop);

        var dexBtn = dexBtnObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(dexBtn.onClick, dexManager.ToggleDex);

        var dexCloseBtn = dexCloseObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(dexCloseBtn.onClick, dexManager.ToggleDex);

        var adRefreshBtn = adRefreshObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(adRefreshBtn.onClick, shopManager.AdRefresh);

        var levelBtn = levelBtnObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(levelBtn.onClick, levelManager.TogglePanel);

        var levelUpBtn = levelUpBtnObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(levelUpBtn.onClick, levelManager.OnLevelUpButton);

        var levelCloseBtn = levelCloseObj.GetComponent<Button>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(levelCloseBtn.onClick, levelManager.TogglePanel);

        // 씬 저장
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("✅ TheMadang 씬 구성 완료! Ctrl+S로 저장하세요.");
        EditorUtility.DisplayDialog("완료!", "씬 구성이 완료됐어요!\nCtrl+S로 저장하세요.", "확인");
    }

    [MenuItem("TheMadang/Create Pretendard Font")]
    public static void CreatePretendardFont()
    {
        string srcPath = "Assets/Fonts/Pretendard-Regular.otf";
        var font = AssetDatabase.LoadAssetAtPath<Font>(srcPath);
        if (font == null)
        {
            EditorUtility.DisplayDialog("오류",
                $"폰트를 찾지 못했어요:\n{srcPath}", "확인");
            return;
        }

        // Dynamic SDF 폰트 에셋 생성 (CreateFontAsset은 기본이 Dynamic → 한글 안 깨짐)
        var fa = TMP_FontAsset.CreateFontAsset(font);

        string outPath = "Assets/Fonts/Pretendard SDF.asset";
        AssetDatabase.DeleteAsset(outPath);
        AssetDatabase.CreateAsset(fa, outPath);

        // 아틀라스 텍스처 / 머티리얼을 서브에셋으로 저장
        foreach (var tex in fa.atlasTextures)
        {
            tex.name = "Pretendard Atlas";
            AssetDatabase.AddObjectToAsset(tex, fa);
        }
        if (fa.material != null)
        {
            fa.material.name = "Pretendard Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }

        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Pretendard SDF 생성 완료! 이제 Setup Scene을 다시 실행하세요.");
        EditorUtility.DisplayDialog("완료",
            "Pretendard 폰트 생성 완료!\nTheMadang → Setup Scene 을 다시 실행하세요.", "확인");
    }

    [MenuItem("TheMadang/Fix Korean Font")]
    public static void FixKoreanFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/malgun SDF.asset");
        if (font == null)
        {
            EditorUtility.DisplayDialog("오류", "malgun SDF 폰트를 찾지 못했어요.", "확인");
            return;
        }
        // 필요한 글자를 런타임에 생성하도록 폰트 데이터 초기화
        font.ClearFontAssetData(true);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("한글 폰트를 Dynamic으로 전환했어요. 이제 글자가 안 깨져요.");
        EditorUtility.DisplayDialog("완료", "한글 폰트 Dynamic 전환 완료!\n다시 Play 해보세요.", "확인");
    }

    [MenuItem("TheMadang/Create Custom Cat")]
    public static void CreateCustomCat()
    {
        // 손그림 고양이 '동백'만 다시 생성(그림 교체 후 반영용). PPU 4600 = 2048px/4600 ≈ 0.45유닛.
        if (CreateCustomAnimalCore("동백", "mycat", AnimalType.Cat, "MyCat", 4600f, 1f,
            ZoneMask.Yard | ZoneMask.Porch | ZoneMask.Grass))
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료",
                "손그림 고양이 '동백' 생성 완료!\nSetup Scene을 다시 실행하면 마당에 등장해요.", "확인");
        }
    }

    // 손그림 동물 전체(동백 + 사용자 업로드 15종)를 한 번에 생성.
    // 그림: Assets/Sprites/{key}_body.png (배경 투명 처리됨). 새 동물 추가 시 아래 배열에 한 줄만 더하면 됨.
    [MenuItem("TheMadang/Create All Animals")]
    public static void CreateAllAnimals()
    {
        // size=크기배율, zones=출현 가능 구역(복수). 고양이=마당·마루·수풀, 강아지=마당,
        // 유령=마당·마루·지붕, 처녀귀신(소복이)=거기에 우물 추가(우물은 그녀 전용).
        const ZoneMask CAT   = ZoneMask.Yard | ZoneMask.Porch | ZoneMask.Grass;
        const ZoneMask DOG   = ZoneMask.Yard;
        const ZoneMask GHOST = ZoneMask.Yard | ZoneMask.Porch | ZoneMask.Roof;
        const ZoneMask MAIDEN = ZoneMask.Well; // 처녀귀신(소복이)은 우물에만
        const ZoneMask GIANT  = ZoneMask.Yard;                 // 거대 괴수·로봇은 마당에 우뚝
        const ZoneMask FLYER  = ZoneMask.Yard | ZoneMask.Roof; // 날개 달린 괴수는 지붕 위에도
        var list = new (string name, string key, AnimalType type, string prefab, float size, ZoneMask zones)[]
        {
            ("동백",   "mycat", AnimalType.Cat, "MyCat", 1.0f,  CAT),
            ("구름",   "cat2",  AnimalType.Cat, "Cat2",  0.95f, CAT),
            ("바람",   "cat3",  AnimalType.Cat, "Cat3",  0.95f, CAT),
            ("보리",   "cat4",  AnimalType.Cat, "Cat4",  0.9f,  CAT),
            ("오레오", "cat5",  AnimalType.Cat, "Cat5",  0.9f,  CAT),
            ("모찌",   "cat6",  AnimalType.Cat, "Cat6",  1.0f,  CAT),
            ("먹물",   "cat7",  AnimalType.Cat, "Cat7",  0.95f, CAT),
            ("호피",   "cat8",  AnimalType.Cat, "Cat8",  1.0f,  CAT),
            ("뽀글",   "dog1",  AnimalType.Dog, "Dog1",  0.85f, DOG),
            ("솜이",   "dog2",  AnimalType.Dog, "Dog2",  0.8f,  DOG),
            ("소세지", "dog3",  AnimalType.Dog, "Dog3",  0.95f, DOG),
            ("뚱이",   "dog4",  AnimalType.Dog, "Dog4",  1.15f, DOG),
            ("꼬마",   "dog5",  AnimalType.Dog, "Dog5",  0.7f,  DOG),
            ("코코",   "dog6",  AnimalType.Dog, "Dog6",  1.05f, DOG),
            ("해피",   "dog7",  AnimalType.Dog, "Dog7",  1.3f,  DOG),
            ("만두",   "dog8",  AnimalType.Dog, "Dog8",  0.85f, DOG),

            ("소복이",   "ghost1", AnimalType.Ghost, "Ghost1", 1.05f, MAIDEN),
            ("강시",     "ghost2", AnimalType.Ghost, "Ghost2", 1.05f, GHOST),
            ("드라큘라", "ghost3", AnimalType.Ghost, "Ghost3", 1.0f,  GHOST),
            ("미라",     "ghost4", AnimalType.Ghost, "Ghost4", 1.05f, GHOST),
            ("제이슨",   "ghost5", AnimalType.Ghost, "Ghost5", 1.05f, GHOST),
            ("구미호",   "ghost6", AnimalType.Ghost, "Ghost6", 1.1f,  GHOST),
            ("호박이",   "ghost7", AnimalType.Ghost, "Ghost7", 0.85f, GHOST),

            // 괴수·로봇(특이 피사체). 사진 잘 찍히게 동물과 비슷한 크기(살짝만 큼).
            ("모스",   "what1", AnimalType.Kaiju, "What1", 1.0f, FLYER), // 나방 괴수
            ("고지",   "what2", AnimalType.Kaiju, "What2", 1.2f, GIANT), // 공룡 괴수
            ("금룡",   "what7", AnimalType.Kaiju, "What7", 1.2f, FLYER), // 삼두룡
            ("적기사", "what3", AnimalType.Robot, "What3", 1.1f, GIANT), // 사무라이 로봇
            ("보라매", "what4", AnimalType.Robot, "What4", 1.1f, GIANT), // 보라 메카
            ("강철왕", "what5", AnimalType.Robot, "What5", 1.15f, GIANT), // 마징가풍 로봇
            ("백기사", "what6", AnimalType.Robot, "What6", 1.1f, GIANT), // 건담풍 로봇
        };

        int ok = 0;
        var missing = new System.Collections.Generic.List<string>();
        foreach (var a in list)
        {
            if (CreateCustomAnimalCore(a.name, a.key, a.type, a.prefab, 4600f, a.size, a.zones)) ok++;
            else missing.Add($"{a.name}({a.key}_body.png)");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string msg = $"손그림 동물 {ok}/{list.Length}종 생성 완료!\nSetup Scene을 다시 실행하면 마당·도감에 등장해요.";
        if (missing.Count > 0) msg += "\n\n그림을 못 찾음:\n" + string.Join("\n", missing);
        EditorUtility.DisplayDialog("완료", msg, "확인");
    }

    // 손그림 동물 1종 생성(핵심). 성공 시 true. SaveAssets/Refresh/대화상자는 호출측에서 처리.
    //  · Sprites/{key}_body.png → Resources/{key}_body.bytes 복사(임포트 체커보드 우회)
    //  · RuntimeImageLoader 프리팹(Prefabs/Animals/{prefabName}.prefab) 생성, costumeKey=key
    //  · RuntimeImageLoader가 있어야 스폰 명단 필터(SceneSetup)에 잡혀 게임에 등록됨
    static bool CreateCustomAnimalCore(string animalName, string key, AnimalType type, string prefabName, float ppu, float sizeScale, ZoneMask zones)
    {
        string bodyPath = $"Assets/Sprites/{key}_body.png";
        EnsureSpriteImport(bodyPath);
        var bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(bodyPath);
        if (bodySprite == null)
            bodySprite = System.Linq.Enumerable.FirstOrDefault(
                System.Linq.Enumerable.OfType<Sprite>(AssetDatabase.LoadAllAssetsAtPath(bodyPath)));
        if (bodySprite == null) return false;

        System.IO.Directory.CreateDirectory(Application.dataPath + "/Prefabs/Animals");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
        System.IO.File.Copy(Application.dataPath + $"/Sprites/{key}_body.png",
                            Application.dataPath + $"/Resources/{key}_body.bytes", true);
        // 선물 착용(코스튬) 그림이 있으면 함께 복사 (친밀도 MAX 때 사용). 없으면 건너뜀.
        string dressedSrc = Application.dataPath + $"/Sprites/{key}_dressed_body.png";
        if (System.IO.File.Exists(dressedSrc))
            System.IO.File.Copy(dressedSrc, Application.dataPath + $"/Resources/{key}_dressed_body.bytes", true);

        var go = new GameObject(prefabName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bodySprite;
        sr.sortingOrder = 1;

        var animal = go.AddComponent<Animal>();
        animal.animalType = type;
        animal.animalName = animalName;
        animal.costumeKey = key; // Resources/Costumes/{key}_{id}.png
        animal.sizeScale = sizeScale;
        animal.zones = zones;

        var loader = go.AddComponent<RuntimeImageLoader>();
        loader.resourceName = $"{key}_body";
        loader.pixelsPerUnit = ppu;

        float size = (bodySprite.texture != null ? bodySprite.texture.width : 2048f) / ppu;
        go.AddComponent<BoxCollider2D>().size = new Vector2(size, size);

        string prefabPath = $"Assets/Prefabs/Animals/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return true;
    }

    // 선물 그림(Assets/Sprites/gift1~4_body.png)을 Resources/gift{n}_body.bytes로 복사.
    // 상점/드래그가 BodyImage.Load("gift{id}_body")로 불러와 단색 아이콘 대신 그림 표시.
    // gift{n}_body.png가 없으면 그 선물은 그대로 단색으로 남음(폴백).
    [MenuItem("TheMadang/Import Gift Images")]
    public static void ImportGiftImages()
    {
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
        int n = 0;
        for (int i = 1; i <= 4; i++)
        {
            string src = Application.dataPath + $"/Sprites/gift{i}_body.png";
            if (!System.IO.File.Exists(src)) continue;
            System.IO.File.Copy(src, Application.dataPath + $"/Resources/gift{i}_body.bytes", true);
            n++;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"선물 그림 {n}/4개를 Resources로 복사했어요.\n다시 Play 하면 상점에 그림이 보여요.", "확인");
    }

    [MenuItem("TheMadang/Import Button Art")]
    public static void ImportButtonArt()
    {
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
        string[] names = { "btn_dex", "btn_album", "btn_camera", "btn_shop" };
        int n = 0;
        foreach (var name in names)
        {
            string src = Application.dataPath + $"/Sprites/UI/{name}.png";
            if (!System.IO.File.Exists(src)) continue;
            System.IO.File.Copy(src, Application.dataPath + $"/Resources/{name}.bytes", true);
            n++;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"버튼 그림 {n}/4개를 Resources로 복사했어요.\n그림 없는 버튼은 기존 원목 스타일 그대로예요.\nSetup Scene 재실행 후 Play 하면 반영돼요.", "확인");
    }

    [MenuItem("TheMadang/Reset Save Data")]
    public static void ResetSave()
    {
        if (EditorUtility.DisplayDialog("세이브 초기화",
            "골드·친밀도·도감 기록을 모두 지울까요?", "초기화", "취소"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("세이브 데이터 초기화 완료");
        }
    }

    // 큰 풀컬러 텍스처가 Play에서 체커보드로 깨질 때: 무압축으로 강제 재임포트
    [MenuItem("TheMadang/Reimport Sprites")]
    public static void ReimportSprites()
    {
        string[] paths = {
            "Assets/Sprites/madang_bg_portrait.png",
            "Assets/Sprites/madang_bg.png",
            "Assets/Sprites/mycat_body.png",
        };
        int count = 0;
        foreach (var p in paths)
        {
            var importer = AssetImporter.GetAtPath(p) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureCompression = TextureImporterCompression.Uncompressed; // GPU 포맷 문제 제거
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096; // 큰 배경이 다운스케일로 NPOT 깨지지 않게
            importer.SaveAndReimport();
            count++;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"스프라이트 {count}개 무압축으로 재임포트 완료!\n▶ Play 후 C키로 동백/배경을 확인하세요.", "확인");
    }

    // 패널 안에 스크롤 그리드(뷰포트+마스크+ScrollRect > Content) 생성. Content transform 반환.
    // 항목이 많아 화면을 넘치면 세로 스크롤. 상단 탭 줄(~160px) 아래, 하단 닫기 버튼(80px) 위 영역.
    static Transform BuildScrollGrid(Transform panel, Vector2 cellSize, Vector2 spacing, int columns)
    {
        var scroll = new GameObject("Scroll");
        scroll.transform.SetParent(panel, false);
        var scrollImg = scroll.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0); // 투명 (휠 스크롤 raycast용)
        var scrollRT = scroll.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(20, 80);     // 하단 닫기 버튼 위
        scrollRT.offsetMax = new Vector2(-20, -160);  // 상단 탭 줄 아래
        scroll.AddComponent<RectMask2D>();
        var sr = scroll.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 35f;

        var content = new GameObject("Content");
        content.transform.SetParent(scroll.transform, false);
        var glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize = cellSize;
        glg.spacing = spacing;
        glg.padding = new RectOffset(20, 20, 20, 20);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = columns;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = Vector2.zero;
        cRT.anchoredPosition = Vector2.zero;

        sr.viewport = scrollRT;
        sr.content = cRT;
        return content.transform;
    }

    static void DestroyIfExists(string name)
    {
        var obj = GameObject.Find(name);
        if (obj != null) Object.DestroyImmediate(obj);
    }

    // PNG가 Single Sprite로 임포트되도록 보장
    static void EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 전체화면이되 상단 topInset만큼 비움 → 상단 HUD(골드·레벨·달) 아래 영역에 패널 배치.
    static void StretchBelowTop(RectTransform rect, float topInset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(0, -topInset);
    }

    // 뷰파인더 모서리 ㄱ자 브래킷. dir은 안쪽 방향(x,y: +1 또는 -1)
    static void CreateBracket(Transform parent, Vector2 anchor, Vector2 dir)
    {
        float len = 28f, thick = 5f;
        Color c = Color.white;

        var h = new GameObject("BracketH");
        h.transform.SetParent(parent, false);
        var hImg = h.AddComponent<Image>();
        hImg.color = c; hImg.raycastTarget = false;
        var hRect = h.GetComponent<RectTransform>();
        hRect.anchorMin = hRect.anchorMax = anchor;
        hRect.pivot = new Vector2(dir.x > 0 ? 0 : 1, dir.y > 0 ? 0 : 1);
        hRect.sizeDelta = new Vector2(len, thick);
        hRect.anchoredPosition = Vector2.zero;

        var v = new GameObject("BracketV");
        v.transform.SetParent(parent, false);
        var vImg = v.AddComponent<Image>();
        vImg.color = c; vImg.raycastTarget = false;
        var vRect = v.GetComponent<RectTransform>();
        vRect.anchorMin = vRect.anchorMax = anchor;
        vRect.pivot = new Vector2(dir.x > 0 ? 0 : 1, dir.y > 0 ? 0 : 1);
        vRect.sizeDelta = new Vector2(thick, len);
        vRect.anchoredPosition = Vector2.zero;
    }

    // 프레임 컨테이너의 한 모서리에 ㄱ자 브래킷 생성. corner: (0,1)좌상 (1,1)우상 (0,0)좌하 (1,0)우하
    static void CreateFrameBracket(Transform frame, Vector2 corner)
    {
        float len = 40f, thick = 6f;
        Color c = Color.white;

        var h = new GameObject("BracketH");
        h.transform.SetParent(frame, false);
        var hImg = h.AddComponent<Image>();
        hImg.color = c; hImg.raycastTarget = false;
        var hRect = h.GetComponent<RectTransform>();
        hRect.anchorMin = hRect.anchorMax = corner;
        hRect.pivot = corner;
        hRect.sizeDelta = new Vector2(len, thick);
        hRect.anchoredPosition = Vector2.zero;

        var v = new GameObject("BracketV");
        v.transform.SetParent(frame, false);
        var vImg = v.AddComponent<Image>();
        vImg.color = c; vImg.raycastTarget = false;
        var vRect = v.GetComponent<RectTransform>();
        vRect.anchorMin = vRect.anchorMax = corner;
        vRect.pivot = corner;
        vRect.sizeDelta = new Vector2(thick, len);
        vRect.anchoredPosition = Vector2.zero;
    }

    // 십자선 텍스처 생성
    static Sprite CreateCrosshairSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        var clear = new Color(0, 0, 0, 0);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, clear);

        int mid = size / 2;
        for (int i = 0; i < size; i++)
        {
            // 가운데 빈 십자(끝부분만 그림)
            if (i < 10 || i > size - 11)
            {
                tex.SetPixel(i, mid, Color.white);
                tex.SetPixel(i, mid - 1, Color.white);
                tex.SetPixel(mid, i, Color.white);
                tex.SetPixel(mid - 1, i, Color.white);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // 해 아이콘 (노란 원 + 광선)
    static Sprite CreateSunSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        var clear = new Color(0, 0, 0, 0);
        var sun = new Color(1f, 0.82f, 0.2f);
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float core = 17f, rayOuter = 30f;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, c);
                bool fill = d <= core;
                if (!fill && d <= rayOuter)
                {
                    // 8방향 광선
                    float ang = Mathf.Atan2(y - c.y, x - c.x) * Mathf.Rad2Deg;
                    float m = Mathf.Abs(Mathf.Repeat(ang, 45f) - 22.5f);
                    if (m < 7f) fill = true;
                }
                tex.SetPixel(x, y, fill ? sun : clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // 초승달 아이콘 (원에서 살짝 옆 원을 빼서 crescent)
    static Sprite CreateMoonSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        var clear = new Color(0, 0, 0, 0);
        var moon = new Color(0.95f, 0.93f, 0.7f);
        Vector2 c = new Vector2(size / 2f, size / 2f);
        Vector2 c2 = new Vector2(size / 2f + 12f, size / 2f + 6f); // 빼낼 원
        float r = 24f, r2 = 22f;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                Vector2 p = new Vector2(x, y);
                bool inMoon = Vector2.Distance(p, c) <= r;
                bool inCut = Vector2.Distance(p, c2) <= r2;
                tex.SetPixel(x, y, (inMoon && !inCut) ? moon : clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite CreateColorSprite(Color color)
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { color, color, color, color });
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
    }

    static GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        if (koreanFont != null) tmp.font = koreanFont;
        return obj;
    }

    // ===== 한옥 톤 팔레트 (따뜻한 원목/한지) =====
    static readonly Color HanjiWood  = new Color(0.42f, 0.29f, 0.18f, 0.96f); // 버튼=원목 갈색
    static readonly Color HanjiCream = new Color(0.96f, 0.91f, 0.80f, 1f);    // 밝은 글자(원목 위)
    static readonly Color PanelWarm  = new Color(0.16f, 0.12f, 0.09f, 0.96f); // 패널=짙은 원목
    static readonly Color BarWarm    = new Color(0.13f, 0.09f, 0.06f, 0.60f); // 하단바
    static readonly Color GoldTint   = new Color(1f, 0.88f, 0.55f, 1f);       // 골드 텍스트

    static GameObject CreateButtonObject(string name, Transform parent, string label, int fontSize = 28)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var img = obj.AddComponent<Image>();
        img.color = HanjiWood;
        obj.AddComponent<Button>();
        obj.AddComponent<ButtonJuice>(); // 누름 손맛(스케일 펀치)
        obj.AddComponent<RoundedBox>();   // 둥근 모서리(원목 단추 느낌)

        var textObj = CreateTextObject(name + "Text", obj.transform, label, fontSize);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var ttmp = textObj.GetComponent<TextMeshProUGUI>();
        ttmp.alignment = TextAlignmentOptions.Center;
        ttmp.color = HanjiCream; // 원목 위 따뜻한 흰 글자

        return obj;
    }

    // 버튼에 사용자가 그린 그림(글씨가 이미 그림 안에 새겨져 있는 완성형 디자인)을 적용.
    // 코드로 만든 텍스트 라벨은 지워 중복 표시를 막고, 9-슬라이스 없이 이미지 전체를 그대로 늘려 채운다
    // (그림이 이미 버튼 비율에 맞게 크롭돼 있으므로 border=0=단순 스트레치가 가장 자연스러움).
    static void UseBakedLabelArt(GameObject btn, string skinName)
    {
        var box = btn.GetComponent<RoundedBox>();
        box.skinName = skinName;
        box.skinBorder = 0f;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = "";
    }
}

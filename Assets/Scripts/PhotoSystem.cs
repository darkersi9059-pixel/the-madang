using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum PhotoRarity { Common, Rare, Epic }

public class PhotoSystem : MonoBehaviour
{
    public static PhotoSystem Instance { get; private set; }

    [Header("Camera UI")]
    public GameObject cameraViewfinderUI;   // 카메라 모드 전체 오버레이
    public RectTransform viewfinderFrame;    // 마우스를 따라다니는 프레임
    public Image captureFlash;
    public Canvas uiCanvas;                  // 캡처 시 잠깐 숨길 UI 캔버스
    public float flashDuration = 0.35f;

    [Header("Photo Score")]
    public int basePhotoValue = 10;

    public bool IsCameraMode { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;

        if (kb != null && kb.cKey.wasPressedThisFrame) ToggleCameraMode();
        if (!IsCameraMode) return;

        if (kb != null && kb.escapeKey.wasPressedThisFrame) { ToggleCameraMode(); return; }

        // 마우스와 터치를 모두 지원하는 통합 포인터 (모바일 웹 대응)
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer == null) return;

        Vector2 pos = pointer.position.ReadValue();
        if (viewfinderFrame != null)
            viewfinderFrame.position = pos;

        // 데스크톱: hover로 조준 + 클릭 촬영 / 모바일: 손가락으로 프레임 끌어 조준 후 떼면 촬영
        if (pointer.press.wasReleasedThisFrame) TryTakePhoto(pos);
    }

    public void ToggleCameraMode()
    {
        IsCameraMode = !IsCameraMode;
        cameraViewfinderUI?.SetActive(IsCameraMode);
    }

    void TryTakePhoto(Vector2 frameCenter)
    {
        float frameSize = viewfinderFrame != null ? viewfinderFrame.sizeDelta.x : 280f;

        // 프레임 안에서 가장 잘 담긴 동물 찾기
        Animal best = null;
        float bestQuality = 0f;
        bool sawAlreadyShot = false;
        foreach (var a in FindObjectsByType<Animal>(FindObjectsSortMode.None))
        {
            if (a.HasBeenPhotographed)
            {
                // 이미 찍은 피사체가 프레임 안에 있는지 기록
                if (EvaluateFraming(a, frameCenter, frameSize) > 0.05f) sawAlreadyShot = true;
                continue;
            }
            float q = EvaluateFraming(a, frameCenter, frameSize);
            if (q > bestQuality) { bestQuality = q; best = a; }
        }

        if (best == null || bestQuality <= 0.05f)
        {
            var wp = Camera.main.ScreenToWorldPoint(new Vector3(frameCenter.x, frameCenter.y, 10f));
            UIManager.Instance?.ShowFloatingText(wp,
                sawAlreadyShot ? "이미 찍은 친구예요!" : "프레임 안에 친구가 없어요!");
            return;
        }

        TakePhoto(best, bestQuality);
    }

    // 구도 평가: 동물이 프레임 중앙에 + 꽉 차게 담길수록 1.0에 가까움
    float EvaluateFraming(Animal animal, Vector2 frameCenter, float frameSize)
    {
        var sr = animal.GetComponent<SpriteRenderer>();
        if (sr == null) return 0f;

        var cam = Camera.main;
        Vector3 animalScreen = cam.WorldToScreenPoint(sr.bounds.center);
        float halfFrame = frameSize * 0.5f;

        // 1) 중앙 점수: 동물이 프레임 중심에 가까울수록 높음
        float dist = Vector2.Distance(new Vector2(animalScreen.x, animalScreen.y), frameCenter);
        float centerScore = Mathf.Clamp01(1f - dist / halfFrame);
        if (centerScore <= 0f) return 0f; // 프레임 밖

        // 2) 채움 점수: 동물의 화면 크기가 프레임을 적당히 채울수록 높음(0.85가 이상적)
        float pixelsPerUnit = Screen.height / (2f * cam.orthographicSize);
        float animalPixelH = sr.bounds.size.y * pixelsPerUnit;
        float fill = animalPixelH / frameSize;
        float fillScore = Mathf.Clamp01(1f - Mathf.Abs(fill - 0.85f) / 0.85f);

        return centerScore * 0.6f + fillScore * 0.4f;
    }

    void TakePhoto(Animal animal, float quality)
    {
        int score = CalculateScore(animal, quality, out PhotoRarity rarity);
        StartCoroutine(CaptureRoutine(animal, score, rarity, quality));
    }

    // 프레임 영역을 실제로 캡처해 PNG로 저장
    IEnumerator CaptureRoutine(Animal animal, int score, PhotoRarity rarity, float quality)
    {
        // 프레임의 실제 화면 픽셀 크기/위치 (CanvasScaler 영향 받지 않도록 월드 코너 사용)
        int w, h, x, y;
        if (viewfinderFrame != null)
        {
            var corners = new Vector3[4];
            viewfinderFrame.GetWorldCorners(corners); // 오버레이 캔버스 → 화면 픽셀
            w = Mathf.RoundToInt(corners[2].x - corners[0].x);
            h = Mathf.RoundToInt(corners[2].y - corners[0].y);
            x = Mathf.RoundToInt(corners[0].x);
            y = Mathf.RoundToInt(corners[0].y);
        }
        else
        {
            w = h = 280;
            var ptr = UnityEngine.InputSystem.Pointer.current;
            var c = ptr != null ? ptr.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            x = Mathf.RoundToInt(c.x - 140); y = Mathf.RoundToInt(c.y - 140);
        }
        w = Mathf.Clamp(w, 1, Screen.width);
        h = Mathf.Clamp(h, 1, Screen.height);
        x = Mathf.Clamp(x, 0, Screen.width - w);
        y = Mathf.Clamp(y, 0, Screen.height - h);

        // UI를 잠깐 숨겨 깨끗한 장면만 캡처
        bool canvasWas = uiCanvas != null && uiCanvas.enabled;
        if (uiCanvas != null) uiCanvas.enabled = false;

        yield return new WaitForEndOfFrame();

        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
        tex.Apply();

        if (uiCanvas != null) uiCanvas.enabled = canvasWas;

        // 파일로 저장
        string category = Categories.Name(animal.animalType);
        PhotoStorage.Save(tex, animal.animalName, (int)animal.animalType, score, (int)rarity, category);

        // 연출 & 보상
        PlayShutter();
        StartCoroutine(FlashEffect());
        StartCoroutine(PunchAnimal(animal.transform));
        var previewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        Vector2 previewStart = new Vector2(x + w / 2f, y + h / 2f); // 캡처 영역 중앙
        StartCoroutine(ShowPolaroid(previewSprite, previewStart));

        string grade = quality >= 0.8f ? "완벽한 구도! " : quality >= 0.5f ? "좋은 구도! " : "";
        string label = rarity switch
        {
            PhotoRarity.Epic => $"{grade}★EPIC★ +{score}G!",
            PhotoRarity.Rare => $"{grade}☆RARE☆ +{score}G!",
            _ => $"+{score}G 촬영!"
        };
        Color rewardColor = rarity switch
        {
            PhotoRarity.Epic => new Color(1f, 0.45f, 1f),    // 에픽=핑크
            PhotoRarity.Rare => new Color(0.5f, 0.8f, 1f),   // 레어=블루
            _ => new Color(1f, 0.85f, 0.25f)                 // 일반=골드
        };
        UIManager.Instance?.ShowFloatingText(animal.transform.position, label, rewardColor);
        GameManager.Instance?.AddGold(score);

        // 처음 사진 찍으면 도감 등록 → 실루엣 해제, 이름 정상 표시
        DexManager.Instance?.Register(animal.animalName, animal.animalType);

        // 사진을 찍을 때마다 그 피사체의 친밀도 +1 (MAX 도달 시 코스튬 착용)
        animal.GainAffinity(1);

        animal.MarkPhotographed();
        ToggleCameraMode();
    }

    int CalculateScore(Animal animal, float quality, out PhotoRarity rarity)
    {
        int baseScore = basePhotoValue + animal.affinity / 10;
        if (animal.AffinityLevel >= AffinityLevel.Friend) baseScore += 15;
        if (animal.AffinityLevel >= AffinityLevel.BestFriend) baseScore += 25;
        // 희귀 특이 피사체는 잡기 어려운 만큼 고득점 보너스
        if (animal.animalType == AnimalType.Ghost) baseScore += 30;
        if (animal.animalType == AnimalType.Kaiju || animal.animalType == AnimalType.Robot) baseScore += 50;

        // 구도 품질이 가격을 결정: 0.3배 ~ 2.5배
        float multiplier = 0.3f + quality * 2.2f;
        int score = Mathf.Max(Mathf.RoundToInt(baseScore * multiplier), 3);

        rarity = quality >= 0.8f ? PhotoRarity.Epic
               : quality >= 0.5f ? PhotoRarity.Rare
               : PhotoRarity.Common;
        return score;
    }

    IEnumerator FlashEffect()
    {
        if (captureFlash == null) yield break;
        captureFlash.gameObject.SetActive(true);
        float t = 0;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            captureFlash.color = new Color(1, 1, 1, Mathf.Lerp(0.9f, 0f, t / flashDuration));
            yield return null;
        }
        captureFlash.gameObject.SetActive(false);
    }

    IEnumerator PunchAnimal(Transform tr)
    {
        Vector3 baseScale = tr.localScale;
        Vector3 big = baseScale * 1.15f;
        float t = 0, dur = 0.18f;
        while (t < dur && tr != null)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.Lerp(big, baseScale, t / dur);
            yield return null;
        }
        if (tr != null) tr.localScale = baseScale;
    }

    // ---------- 폴라로이드 미리보기 (찍은 사진이 앨범으로 쏙) ----------
    IEnumerator ShowPolaroid(Sprite photoSprite, Vector2 startScreenPos)
    {
        if (uiCanvas == null) { yield break; }

        // 흰 폴라로이드 프레임
        var frame = new GameObject("PolaroidPreview");
        frame.transform.SetParent(uiCanvas.transform, false);
        var frameImg = frame.AddComponent<Image>();
        frameImg.color = Color.white;
        var frameRect = frame.GetComponent<RectTransform>();
        frameRect.sizeDelta = new Vector2(190, 220);
        frameRect.position = startScreenPos;

        // 사진
        var photo = new GameObject("Photo");
        photo.transform.SetParent(frame.transform, false);
        var photoImg = photo.AddComponent<Image>();
        photoImg.sprite = photoSprite;
        photoImg.preserveAspect = true;
        var photoRect = photo.GetComponent<RectTransform>();
        photoRect.anchorMin = new Vector2(0.07f, 0.15f);
        photoRect.anchorMax = new Vector2(0.93f, 0.93f);
        photoRect.offsetMin = Vector2.zero;
        photoRect.offsetMax = Vector2.zero;

        // 1) 통통 뿅 등장 (작게→오버슈트→안착) + 살짝 기운 각도에서 똑바로 펴짐
        float tilt0 = Random.Range(-8f, 8f);
        float t = 0, popDur = 0.32f;
        while (t < popDur)
        {
            t += Time.deltaTime;
            float k = t / popDur;
            float s = Mathf.Lerp(0.2f, 1f, UITween.EaseOutBack(k));
            frameRect.localScale = new Vector3(s, s, 1f);
            frameRect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(tilt0, 0f, k));
            yield return null;
        }
        frameRect.localScale = Vector3.one;
        frameRect.localEulerAngles = Vector3.zero;

        // 2) 잠깐 보여주며 살짝 흔들흔들(감쇠)
        float wob = 0f;
        while (wob < 0.55f)
        {
            wob += Time.deltaTime;
            frameRect.localEulerAngles = new Vector3(0, 0, Mathf.Sin(wob * 6f) * 2.5f * (1f - wob / 0.55f));
            yield return null;
        }
        frameRect.localEulerAngles = Vector3.zero;

        // 3) 앨범 버튼(오른쪽 아래)으로 위로 살짝 솟는 아치를 그리며 회전하면서 쏙 빨려 들어감
        Vector2 target = new Vector2(Screen.width - 95, 165);
        Vector2 from = frameRect.position;
        Vector2 ctrl = (from + target) * 0.5f + new Vector2(0, 130f); // 아치 정점
        t = 0; float flyDur = 0.45f;
        while (t < flyDur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, t / flyDur);
            // 2차 베지어로 아치 궤적
            Vector2 a = Vector2.Lerp(from, ctrl, k);
            Vector2 b = Vector2.Lerp(ctrl, target, k);
            frameRect.position = Vector2.Lerp(a, b, k);
            float s = Mathf.Lerp(1f, 0.1f, k);
            frameRect.localScale = new Vector3(s, s, 1f);
            frameRect.localEulerAngles = new Vector3(0, 0, k * 35f); // 빨려들며 살짝 회전
            frameImg.color = new Color(1, 1, 1, 1 - k);
            photoImg.color = new Color(1, 1, 1, 1 - k);
            yield return null;
        }

        Destroy(frame);
        Destroy(photoSprite.texture);
        Destroy(photoSprite);
    }

    // ---------- 셔터음 (코드 생성, 에셋 불필요) ----------
    AudioSource audioSrc;
    static AudioClip shutterClip;

    void PlayShutter()
    {
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.PlayOneShot(GetShutterClip(), 0.6f);
    }

    static AudioClip GetShutterClip()
    {
        if (shutterClip != null) return shutterClip;
        int sampleRate = 44100;
        int len = sampleRate / 8; // 약 0.125초
        var data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            // 두 번의 짧은 '찰칵' (셔터 열림/닫힘)
            float env1 = Mathf.Exp(-t * 60f);
            float env2 = Mathf.Exp(-Mathf.Abs(t - 0.4f) * 60f);
            float noise = Random.value * 2f - 1f;
            data[i] = noise * (env1 + env2) * 0.5f;
        }
        shutterClip = AudioClip.Create("shutter", len, 1, sampleRate, false);
        shutterClip.SetData(data, 0);
        return shutterClip;
    }
}

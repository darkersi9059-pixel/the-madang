using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI affinityText;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Canvas worldCanvas;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => UpdateGoldUI(GameManager.Instance?.Gold ?? 0);

    int lastGold = -1;
    public void UpdateGoldUI(int gold)
    {
        if (goldText == null) return;
        goldText.text = $"골드: {gold}G";
        // 골드가 늘었을 때만 통통 펀치(소비/초기화 땐 조용히)
        if (lastGold >= 0 && gold > lastGold) UITween.Instance.Punch(goldText.transform, 0.2f, 0.28f);
        lastGold = gold;
    }

    public void ShowAffinityPopup(string animalName, int affinity)
    {
        if (affinityText == null) return;
        affinityText.text = $"{animalName} 친밀도: {affinity}/100";
        StopCoroutine(nameof(HideAffinityText));
        StartCoroutine(nameof(HideAffinityText));
    }

    IEnumerator HideAffinityText()
    {
        yield return new WaitForSeconds(2f);
        if (affinityText != null) affinityText.text = "";
    }

    public void ShowFloatingText(Vector3 worldPos, string message)
        => ShowFloatingText(worldPos, message, Color.white);

    public void ShowFloatingText(Vector3 worldPos, string message, Color color)
    {
        if (floatingTextPrefab == null || worldCanvas == null) return;
        var obj = Instantiate(floatingTextPrefab, worldCanvas.transform);
        obj.SetActive(true); // 템플릿이 비활성이어도 복제본은 보이게
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.color = color;

        // 월드 좌표 → 화면 → 캔버스 로컬 좌표로 정확히 변환
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        var canvasRect = worldCanvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPos);
        obj.GetComponent<RectTransform>().anchoredPosition = localPos;

        StartCoroutine(FloatAndFade(obj, color));
    }

    IEnumerator FloatAndFade(GameObject obj, Color baseColor)
    {
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        var rect = obj.GetComponent<RectTransform>();
        float duration = 1.5f;
        float t = 0;
        Vector2 startPos = rect.anchoredPosition;
        float drift = Random.Range(-28f, 28f); // 좌우로 살짝 흘러감

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            rect.anchoredPosition = startPos + new Vector2(drift * k, k * 70f);
            // 등장 시 통통 팝(0.4→1.15→1) 후 유지
            float scale = k < 0.12f ? Mathf.Lerp(0.4f, 1.15f, k / 0.12f)
                        : k < 0.22f ? Mathf.Lerp(1.15f, 1f, (k - 0.12f) / 0.1f)
                        : 1f;
            rect.localScale = Vector3.one * scale;
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
            yield return null;
        }
        Destroy(obj);
    }
}

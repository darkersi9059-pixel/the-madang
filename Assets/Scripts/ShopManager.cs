using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop UI")]
    public GameObject shopPanel;
    public Transform itemContainer;   // GridLayoutGroup
    public TMP_Text titleText;        // 갱신 잔여시간 표시
    public TMP_FontAsset font;

    [Header("Refresh")]
    public int slotsPerRefresh = 6;
    public int refreshPeriodSec = 3600; // 1시간

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (shopPanel != null && shopPanel.activeSelf && titleText != null)
            titleText.text = $"상점   (갱신까지 {RemainingText()})";
    }

    // ---------- 진열 갱신 ----------
    long PeriodIndex() =>
        (System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / refreshPeriodSec)
        + PlayerPrefs.GetInt("ShopAdOffset", 0);

    string RemainingText()
    {
        long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int remain = refreshPeriodSec - (int)(now % refreshPeriodSec);
        return $"{remain / 60:00}:{remain % 60:00}";
    }

    // 4종 고정 진열(순서 그대로)
    List<GiftItem> CurrentSlots() => new List<GiftItem>(GiftDatabase.All);

    // ---------- 갱신 주기당 선물 1회 사용 ----------
    // 각 선물은 갱신(1시간 또는 광고 새로고침)되기 전까지 1회만 선물 가능.
    void SyncUsedPeriod()
    {
        string cur = PeriodIndex().ToString();
        if (PlayerPrefs.GetString("ShopUsedPeriod", "") != cur)
        {
            PlayerPrefs.SetString("ShopUsedPeriod", cur);
            PlayerPrefs.SetString("ShopUsedIds", "");
            PlayerPrefs.Save();
        }
    }

    public bool IsGiftAvailable(int id)
    {
        SyncUsedPeriod();
        foreach (var p in PlayerPrefs.GetString("ShopUsedIds", "").Split(','))
            if (int.TryParse(p, out int x) && x == id) return false;
        return true;
    }

    // 선물 성공 시 호출 → 이 선물을 이번 갱신 주기 동안 품절 처리
    public void MarkGiftUsed(int id)
    {
        if (!IsGiftAvailable(id)) return;
        string s = PlayerPrefs.GetString("ShopUsedIds", "");
        PlayerPrefs.SetString("ShopUsedIds", string.IsNullOrEmpty(s) ? id.ToString() : s + "," + id);
        PlayerPrefs.Save();
        if (shopPanel != null && shopPanel.activeSelf) BuildShopUI(); // 열려있으면 즉시 반영
    }

    // ---------- 열기/닫기 ----------
    public void ToggleShop()
    {
        if (shopPanel == null) return;
        if (shopPanel.activeSelf) CloseShop();
        else OpenShop();
    }

    public void OpenShop()
    {
        UITween.Instance.PopOpen(shopPanel); // 먼저 활성화(통통 등장)
        BuildShopUI();
    }

    public void CloseShop()
    {
        if (shopPanel != null) UITween.Instance.PopClose(shopPanel);
    }

    // 광고 시청 후 즉시 갱신 (지금은 광고 없이 바로 리롤)
    public void AdRefresh()
    {
        PlayerPrefs.SetInt("ShopAdOffset", PlayerPrefs.GetInt("ShopAdOffset", 0) + 1);
        PlayerPrefs.Save();
        BuildShopUI();
        UIManager.Instance?.ShowFloatingText(Vector3.zero, "상점을 새로고침했어요!");
    }

    void BuildShopUI()
    {
        if (itemContainer == null) return;
        foreach (Transform c in itemContainer) Destroy(c.gameObject);
        foreach (var item in CurrentSlots()) CreateItemCard(item);
    }

    void CreateItemCard(GiftItem item)
    {
        bool avail = IsGiftAvailable(item.id);

        var card = new GameObject("ItemCard");
        card.transform.SetParent(itemContainer, false);
        var bg = card.AddComponent<Image>();
        bg.color = avail ? new Color(0.96f, 0.96f, 0.94f) : new Color(0.7f, 0.68f, 0.64f); // 품절은 회색
        // 사용 가능할 때만 드래그(선물) 가능
        if (avail) card.AddComponent<GiftItemButton>().item = item;

        // 아이콘: 선물 그림(Resources/gift{id}_body.bytes)이 있으면 그림, 없으면 단색 폴백
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(card.transform, false);
        var icon = iconObj.AddComponent<Image>();
        var giftSprite = BodyImage.Load($"gift{item.id}_body");
        if (giftSprite != null)
        {
            icon.sprite = giftSprite;
            icon.preserveAspect = true;
            icon.color = avail ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
        else
        {
            icon.color = avail ? item.color : new Color(item.color.r, item.color.g, item.color.b, 0.35f);
        }
        icon.raycastTarget = false;
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.45f);
        iconRect.anchorMax = new Vector2(0.8f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // 라벨
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(card.transform, false);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = avail ? $"{item.name}\n♥{item.affinityBonus} · {item.price}G" : $"{item.name}\n(다음 갱신까지)";
        tmp.fontSize = 19;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = avail ? Color.black : new Color(0.35f, 0.33f, 0.3f);
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0.45f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}

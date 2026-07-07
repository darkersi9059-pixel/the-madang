using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 선물을 집어서(상점 카드 누름) 피사체에게 드래그&드롭. 빗나가면 취소(환불).
public class GiftDragController : MonoBehaviour
{
    public static GiftDragController Instance { get; private set; }

    public Canvas uiCanvas;
    public TMP_FontAsset font;

    GameObject ghost;
    Image ghostImg;
    GiftItem current;
    bool dragging;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 상점 카드를 누르면 호출됨
    public void BeginDrag(GiftItem item)
    {
        if (dragging) return;
        current = item;
        dragging = true;

        // 마당이 보이도록 상점 닫기
        ShopManager.Instance?.CloseShop();

        // 따라다니는 고스트 아이콘
        ghost = new GameObject("GiftGhost");
        ghost.transform.SetParent(uiCanvas.transform, false);
        var bg = ghost.AddComponent<Image>();
        bg.color = new Color(1, 1, 1, 0.9f);
        ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(ghost.transform, false);
        ghostImg = iconObj.AddComponent<Image>();
        // 선물 그림이 있으면 그림, 없으면 단색 폴백
        var giftSprite = BodyImage.Load($"gift{item.id}_body");
        if (giftSprite != null) { ghostImg.sprite = giftSprite; ghostImg.preserveAspect = true; ghostImg.color = Color.white; }
        else ghostImg.color = item.color;
        var ir = iconObj.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.15f, 0.35f);
        ir.anchorMax = new Vector2(0.85f, 0.9f);
        ir.offsetMin = Vector2.zero; ir.offsetMax = Vector2.zero;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(ghost.transform, false);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = item.name;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        if (font != null) tmp.font = font;
        var lr = labelObj.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(1, 0.35f);
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (!dragging) return;

        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (pointer == null) return;

        Vector2 pos = pointer.position.ReadValue();
        if (ghost != null) ghost.transform.position = pos;

        if (pointer.press.wasReleasedThisFrame) Drop(pos);
    }

    void Drop(Vector2 screenPos)
    {
        dragging = false;
        if (ghost != null) Destroy(ghost);

        var world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        var hit = Physics2D.OverlapPoint(world);

        if (hit != null && hit.TryGetComponent<Animal>(out var animal))
        {
            // 친밀도가 가득 찬(코스튬) 친구는 더 이상 선물을 받지 않음
            if (animal.IsMaxed)
            {
                UIManager.Instance?.ShowFloatingText(world, "이미 친밀도가 가득해요!");
                current = null;
                return;
            }
            if (!GameManager.Instance.SpendGold(current.price))
            {
                UIManager.Instance?.ShowFloatingText(world, "골드가 부족해요!");
                return;
            }
            animal.ReceiveGift(current.affinityBonus);
            ShopManager.Instance?.MarkGiftUsed(current.id); // 이 선물은 다음 갱신까지 품절
        }
        else
        {
            // 빗나감 → 취소 (돈 안 빠짐)
            UIManager.Instance?.ShowFloatingText(world, "선물 취소!");
        }
        current = null;
    }
}

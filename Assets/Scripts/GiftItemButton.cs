using UnityEngine;
using UnityEngine.EventSystems;

// 상점 카드: 누르면 드래그 시작
public class GiftItemButton : MonoBehaviour, IPointerDownHandler
{
    [System.NonSerialized] public GiftItem item; // 런타임에만 코드로 할당(직렬화 안 함 → UAC1001 경고 제거)

    public void OnPointerDown(PointerEventData eventData)
    {
        if (item != null) GiftDragController.Instance?.BeginDrag(item);
    }
}

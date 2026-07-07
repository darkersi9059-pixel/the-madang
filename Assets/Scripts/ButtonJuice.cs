using UnityEngine;
using UnityEngine.EventSystems;

// 버튼 누름 손맛: 누르면 살짝 들어가고(작아짐), 떼면 통통 복귀.
// SceneSetup.CreateButtonObject에서 모든 버튼에 자동 부착.
public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData e)
    {
        transform.localScale = Vector3.one * 0.92f;
        SoundManager.Play(SoundManager.Sfx.Click, 0.5f);
    }

    public void OnPointerUp(PointerEventData e)
    {
        UITween.Instance.Punch(transform, 0.10f, 0.18f);
    }
}

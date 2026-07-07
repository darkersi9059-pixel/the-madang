using UnityEngine;
using UnityEngine.UI;

// Image에 둥근 모서리를 입힌다(색은 SceneSetup에서 정한 그대로 유지).
// 런타임에 스프라이트를 생성하므로 에셋 불필요. SceneSetup에서 버튼·패널에 부착.
[RequireComponent(typeof(Image))]
public class RoundedBox : MonoBehaviour
{
    public int cornerRadius = 18;

    // 지정하면 Resources/{skinName}.bytes(사용자 그림)를 9-슬라이스로 사용, 없으면(파일 없을 때) 코드생성 둥근사각형으로 폴백.
    public string skinName = "";
    public float skinBorder = 40f;

    void Awake()
    {
        var img = GetComponent<Image>();
        var custom = string.IsNullOrEmpty(skinName) ? null : BodyImage.Load(skinName, skinBorder);
        if (custom != null)
        {
            img.sprite = custom;
            img.color = Color.white; // 사용자 그림은 원본 색 그대로(틴트 없음)
        }
        else
        {
            img.sprite = UIShapes.RoundedRect(cornerRadius);
        }
        img.type = Image.Type.Sliced;
    }
}

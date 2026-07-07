using UnityEngine;

// Unity 스프라이트 임포트 파이프라인을 우회한다.
// Resources의 .bytes(원본 PNG 바이트)를 런타임에 Texture2D.LoadImage로 직접 디코딩해
// 스프라이트로 만들어 같은 오브젝트의 SpriteRenderer에 넣는다.
// → 큰 풀컬러 텍스처가 Play에서 체커보드로 깨지는 임포트 문제를 근본 회피.
[RequireComponent(typeof(SpriteRenderer))]
public class RuntimeImageLoader : MonoBehaviour
{
    [Tooltip("Resources 폴더 안의 .bytes 이름 (확장자 없이). 예: madang_bg_portrait")]
    public string resourceName;
    public float pixelsPerUnit = 100f;
    public Vector2 pivot = new Vector2(0.5f, 0.5f);

    void Awake()
    {
        if (!string.IsNullOrEmpty(resourceName) && !TryLoad(resourceName))
            Debug.LogWarning($"RuntimeImageLoader: Resources/{resourceName}.bytes 로드 실패");
    }

    // 지정한 Resources/.bytes(PNG)를 디코딩해 SpriteRenderer에 주입. 성공 시 true.
    // 실패(파일 없음·디코딩 실패) 시 기존 스프라이트를 건드리지 않음 → 코스튬 그림이 아직
    // 없을 때 안전하게 기본 그림 유지. Animal이 친밀도 MAX 코스튬 교체에 재사용.
    public bool TryLoad(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        var ta = Resources.Load<TextAsset>(name);
        if (ta == null) return false;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(ta.bytes)) return false; // PNG 바이트를 직접 디코딩
        tex.wrapMode = TextureWrapMode.Clamp;

        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit);
        return true;
    }
}

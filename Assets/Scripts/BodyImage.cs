using System.Collections.Generic;
using UnityEngine;

// Resources의 .bytes(원본 PNG)를 런타임 디코딩해 UI/도감용 Sprite로 캐싱 제공한다.
// RuntimeImageLoader와 같은 임포트 우회 방식이지만, SpriteRenderer가 아니라
// 여러 곳에서 재사용 가능한 Sprite를 돌려준다(도감 카드 실루엣 등).
public static class BodyImage
{
    static readonly Dictionary<string, Sprite> cache = new();

    public static Sprite Load(string resourceName)
    {
        return Load(resourceName, 0f);
    }

    // border>0: 9-슬라이스 스프라이트로 생성(버튼 등 늘어나야 하는 UI용). 네 변 동일 두께(px).
    public static Sprite Load(string resourceName, float border)
    {
        if (string.IsNullOrEmpty(resourceName)) return null;
        string key = border > 0f ? resourceName + "#" + border : resourceName;
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var ta = Resources.Load<TextAsset>(resourceName);
        if (ta == null) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(ta.bytes)) return null;
        tex.wrapMode = TextureWrapMode.Clamp;

        var b = new Vector4(border, border, border, border);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        cache[key] = sprite;
        return sprite;
    }
}

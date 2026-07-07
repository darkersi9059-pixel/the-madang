using UnityEngine;
using System.Collections.Generic;

// 코드로 생성하는 UI 도형(에셋 불필요). 둥근 모서리 9-슬라이스 스프라이트.
public static class UIShapes
{
    static readonly Dictionary<int, Sprite> roundedCache = new Dictionary<int, Sprite>();

    // 둥근 사각형 9-슬라이스 스프라이트. Image.type=Sliced로 쓰면 어떤 크기에서도 모서리 라운드 유지.
    public static Sprite RoundedRect(int radius)
    {
        radius = Mathf.Max(2, radius);
        if (roundedCache.TryGetValue(radius, out var cached) && cached != null) return cached;

        int r = radius;
        int size = r * 2 + 2; // 가운데 2px만 늘어남(9-슬라이스)
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // 안쪽 사각형(모서리 중심) 경계에서의 거리로 알파 → 모서리만 둥글게, 1px 부드러운 경계
                float cx = Mathf.Clamp(x, r, size - r);
                float cy = Mathf.Clamp(y, r, size - r);
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        roundedCache[radius] = sprite;
        return sprite;
    }
}

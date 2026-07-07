using UnityEngine;

// 밤에 마당에 반딧불이가 둥실둥실 떠다니며 반짝인다(낮엔 서서히 사라짐).
// 글로우 스프라이트를 코드로 생성하므로 에셋 불필요.
// DayNightManager.Start에서 자동 부착되므로 씬 배치/Setup Scene 불필요.
public class NightAmbience : MonoBehaviour
{
    [Header("반딧불이")]
    public int count = 14;
    public Vector2 areaMin = new Vector2(-3.2f, -4f);
    public Vector2 areaMax = new Vector2(3.2f, 3.5f);
    public Color glowColor = new Color(0.85f, 1f, 0.45f); // 연두빛 반딧불

    [Header("밤하늘 별")]
    public int starCount = 28;
    public Vector2 skyMin = new Vector2(-3.6f, 3.6f);   // 지붕 위 하늘 영역
    public Vector2 skyMax = new Vector2(3.6f, 6.4f);
    public Color starColor = new Color(1f, 0.97f, 0.85f); // 따뜻한 흰빛

    class Firefly
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Vector2 basePos;
        public float seedX, seedY, seedTw, speed, range;
    }

    class Star
    {
        public SpriteRenderer sr;
        public float seedTw, twSpeed, maxA;
    }

    Firefly[] flies;
    Star[] stars;
    static Sprite glowSprite;

    void Start()
    {
        var sprite = GetGlowSprite();
        flies = new Firefly[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Firefly");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 950; // 밤 오버레이(900)보다 위라야 어둠 위로 빛이 보임
            sr.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);

            float s = Random.Range(0.12f, 0.24f);
            go.transform.localScale = new Vector3(s, s, 1f);

            flies[i] = new Firefly
            {
                tr = go.transform,
                sr = sr,
                basePos = new Vector2(Random.Range(areaMin.x, areaMax.x), Random.Range(areaMin.y, areaMax.y)),
                seedX = Random.value * 100f,
                seedY = Random.value * 100f,
                seedTw = Random.value * 6.28f,
                speed = Random.Range(0.15f, 0.4f),
                range = Random.Range(0.3f, 0.9f)
            };
        }

        // 밤하늘 별: 상단에 거의 정지, 제각각 반짝임
        stars = new Star[starCount];
        for (int i = 0; i < starCount; i++)
        {
            var go = new GameObject("Star");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(
                Random.Range(skyMin.x, skyMax.x), Random.Range(skyMin.y, skyMax.y), 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 950;
            sr.color = new Color(starColor.r, starColor.g, starColor.b, 0f);
            float s = Random.Range(0.05f, 0.12f); // 별은 반딧불이보다 작게
            go.transform.localScale = new Vector3(s, s, 1f);

            stars[i] = new Star
            {
                sr = sr,
                seedTw = Random.value * 6.28f,
                twSpeed = Random.Range(0.8f, 2.2f),
                maxA = Random.Range(0.5f, 1f)
            };
        }
    }

    void Update()
    {
        bool night = DayNightManager.Instance != null && DayNightManager.Instance.IsNight();
        float time = Time.time;

        if (flies != null)
            foreach (var f in flies)
            {
                // 둥실둥실 이동(Perlin noise로 부드럽게 떠다님)
                float dx = (Mathf.PerlinNoise(f.seedX, time * f.speed) - 0.5f) * 2f * f.range;
                float dy = (Mathf.PerlinNoise(f.seedY, time * f.speed) - 0.5f) * 2f * f.range;
                f.tr.position = new Vector3(f.basePos.x + dx, f.basePos.y + dy, 0f);

                // 반짝임 + 낮/밤 페이드
                float twinkle = Mathf.Sin(time * 3f + f.seedTw) * 0.5f + 0.5f;
                float targetA = night ? (0.25f + twinkle * 0.55f) : 0f;
                var c = f.sr.color;
                c.a = Mathf.Lerp(c.a, targetA, Time.deltaTime * 2.5f);
                f.sr.color = c;
            }

        if (stars != null)
            foreach (var st in stars)
            {
                float twinkle = Mathf.Sin(time * st.twSpeed + st.seedTw) * 0.5f + 0.5f;
                float targetA = night ? (0.15f + twinkle * st.maxA) : 0f;
                var c = st.sr.color;
                c.a = Mathf.Lerp(c.a, targetA, Time.deltaTime * 2f);
                st.sr.color = c;
            }
    }

    // 가운데가 밝고 가장자리로 부드럽게 사라지는 원형 글로우 스프라이트(1회 생성 후 공유)
    static Sprite GetGlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(size / 2f, size / 2f);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;
                float a = Mathf.Clamp01(1f - d);
                a *= a; // 부드러운 falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return glowSprite;
    }
}

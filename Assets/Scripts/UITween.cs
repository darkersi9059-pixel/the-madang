using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 공용 UI 연출 헬퍼(싱글톤). 창 열기/닫기 팝 애니메이션, 스케일 펀치.
// 처음 쓸 때 자동 생성되므로 씬 배치나 Setup Scene 불필요.
// Time.timeScale 영향 안 받도록 전부 unscaledDeltaTime 사용.
public class UITween : MonoBehaviour
{
    static UITween _inst;
    public static UITween Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("UITween");
                _inst = go.AddComponent<UITween>();
                DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }

    readonly Dictionary<GameObject, Coroutine> running = new Dictionary<GameObject, Coroutine>();

    // 창 열기: 살짝 작게+투명에서 통통 튀며 등장
    public void PopOpen(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(true);
        SoundManager.Play(SoundManager.Sfx.Open, 0.6f);
        StopFor(panel);
        running[panel] = StartCoroutine(OpenRoutine(panel, GetCG(panel)));
    }

    // 창 닫기: 살짝 작아지고 투명해진 뒤 비활성화
    public void PopClose(GameObject panel)
    {
        if (panel == null || !panel.activeSelf) return;
        SoundManager.Play(SoundManager.Sfx.Close, 0.5f);
        StopFor(panel);
        running[panel] = StartCoroutine(CloseRoutine(panel, GetCG(panel)));
    }

    IEnumerator OpenRoutine(GameObject panel, CanvasGroup cg)
    {
        var t = panel.transform;
        float d = 0.18f, e = 0f;
        cg.alpha = 0f;
        while (e < d)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / d);
            t.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, EaseOutBack(k)); // 1.0 살짝 넘었다 안착
            cg.alpha = k;
            yield return null;
        }
        t.localScale = Vector3.one;
        cg.alpha = 1f;
        running.Remove(panel);
    }

    IEnumerator CloseRoutine(GameObject panel, CanvasGroup cg)
    {
        var t = panel.transform;
        Vector3 from = t.localScale;
        float d = 0.13f, e = 0f;
        while (e < d)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / d);
            t.localScale = Vector3.Lerp(from, Vector3.one * 0.9f, k);
            cg.alpha = 1f - k;
            yield return null;
        }
        // 다음에 다시 켤 때 정상 상태로 복원
        t.localScale = Vector3.one;
        cg.alpha = 1f;
        panel.SetActive(false);
        running.Remove(panel);
    }

    // 스케일 펀치(버튼 누름·골드 획득 등). 0→peak→0 형태로 통통 튀고 1로 안착.
    public void Punch(Transform target, float amount = 0.15f, float dur = 0.22f)
    {
        if (target != null) StartCoroutine(PunchRoutine(target, amount, dur));
    }

    IEnumerator PunchRoutine(Transform t, float amount, float dur)
    {
        float e = 0f;
        while (e < dur && t != null)
        {
            e += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(e / dur);
            float p = Mathf.Sin(k * Mathf.PI) * amount;
            t.localScale = Vector3.one * (1f + p);
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    void StopFor(GameObject panel)
    {
        if (running.TryGetValue(panel, out var co) && co != null) StopCoroutine(co);
    }

    static CanvasGroup GetCG(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    // 끝에서 1.0을 살짝 넘었다 돌아오는 이징(통통한 느낌). 다른 스크립트(Animal 등록)도 재사용.
    public static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 앨범/도감 패널 상단의 카테고리 필터 탭 줄을 코드로 생성하는 공용 헬퍼.
// SceneSetup을 다시 돌릴 필요 없이 매니저가 런타임에 직접 만든다.
public static class CategoryTabBar
{
    public class Tab
    {
        public string label;
        public Action onSelect;
        public Tab(string label, Action onSelect) { this.label = label; this.onSelect = onSelect; }
    }

    // panel 상단(제목 아래)에 탭 줄을 만들어 반환. selectedIndex 탭만 강조.
    // 기존 탭 줄은 매니저가 미리 Destroy 하고 호출한다.
    public static GameObject Build(Transform panel, List<Tab> tabs, int selectedIndex, TMP_FontAsset font)
    {
        var row = new GameObject("CategoryTabs");
        row.transform.SetParent(panel, false);
        var rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        // 제목(상단 ~80px) 아래 90~150px 밴드에 배치
        rect.offsetMin = new Vector2(20, -150);
        rect.offsetMax = new Vector2(-20, -90);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            bool sel = i == selectedIndex;

            var btnObj = new GameObject("Tab_" + tab.label);
            btnObj.transform.SetParent(row.transform, false);
            var img = btnObj.AddComponent<Image>();
            img.color = sel ? new Color(0.95f, 0.85f, 0.4f) : new Color(0.42f, 0.29f, 0.18f, 0.96f); // 선택=골드, 미선택=원목(한옥톤 통일)
            var btn = btnObj.AddComponent<Button>();
            btnObj.AddComponent<ButtonJuice>(); // 누름 손맛(다른 버튼과 통일)
            btnObj.AddComponent<RoundedBox>().cornerRadius = 14; // 둥근 모서리(다른 버튼과 통일)
            var captured = tab;
            btn.onClick.AddListener(() => captured.onSelect());

            var txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = tab.label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = sel ? new Color(0.3f, 0.2f, 0.05f) : new Color(0.96f, 0.91f, 0.80f); // 선택=진한갈, 미선택=크림(한옥톤 통일)
            if (font != null) tmp.font = font;
            var tr = txtObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
        }

        return row;
    }
}

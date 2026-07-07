using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("HUD")]
    public TMP_Text levelButtonText;   // 좌하단 레벨 표시

    [Header("Level Panel")]
    public GameObject levelPanel;
    public TMP_Text infoText;
    public Button levelUpButton;
    public TMP_Text levelUpButtonText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && levelButtonText != null)
            levelButtonText.text = $"Lv.{gm.Level}";
    }

    public void TogglePanel()
    {
        if (levelPanel == null) return;
        if (levelPanel.activeSelf) { UITween.Instance.PopClose(levelPanel); return; }
        UITween.Instance.PopOpen(levelPanel); // 먼저 활성화(통통 등장)
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int cost = gm.LevelUpCost();

        if (infoText != null)
            infoText.text =
                $"현재 레벨: Lv.{gm.Level}\n" +
                $"보유 골드: {gm.Gold}G\n" +
                $"레벨업 비용: {cost}G";

        bool can = gm.CanLevelUp();
        if (levelUpButton != null) levelUpButton.interactable = can;
        if (levelUpButtonText != null)
            levelUpButtonText.text = can ? $"레벨 업! (-{cost}G)" : "골드 부족";
    }

    public void OnLevelUpButton()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (gm.TryLevelUp())
        {
            SoundManager.Play(SoundManager.Sfx.LevelUp);
            UIManager.Instance?.ShowFloatingText(ScreenCenterWorld(), $"레벨 업! Lv.{gm.Level} 달성!");
            Refresh();
        }
    }

    Vector3 ScreenCenterWorld() =>
        Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 10f));
}

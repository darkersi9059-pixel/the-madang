using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int Gold { get; private set; } = 1000; // 테스트: 시작 골드 상향
    public int TotalGoldEarned { get; private set; } = 0; // 누적 획득 골드
    public int Level { get; private set; } = 1;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadGame();
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        if (amount > 0) { TotalGoldEarned += amount; SoundManager.Play(SoundManager.Sfx.Gold); } // 누적은 벌었을 때만 + 동전 종소리
        UIManager.Instance?.UpdateGoldUI(Gold);
        SaveGame();
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        UIManager.Instance?.UpdateGoldUI(Gold);
        SaveGame();
        return true;
    }

    // ---------- 레벨 (보유 골드 소비) ----------
    public int LevelUpCost() => Level * 50; // 테스트: Lv1→2: 50, Lv2→3: 100 ... (원래 ×300)

    public bool CanLevelUp() => Gold >= LevelUpCost();

    public bool TryLevelUp()
    {
        int cost = LevelUpCost();
        if (Gold < cost) return false;
        SpendGold(cost);  // 보유 골드 차감
        Level++;
        SaveGame();
        return true;
    }

    void SaveGame()
    {
        PlayerPrefs.SetInt("Gold", Gold);
        PlayerPrefs.SetInt("TotalGoldEarned", TotalGoldEarned);
        PlayerPrefs.SetInt("Level", Level);
        PlayerPrefs.Save();
    }

    void LoadGame()
    {
        Gold = PlayerPrefs.GetInt("Gold", 1000); // 테스트: 시작 골드 상향
        TotalGoldEarned = PlayerPrefs.GetInt("TotalGoldEarned", 0);
        Level = PlayerPrefs.GetInt("Level", 1);
    }
}

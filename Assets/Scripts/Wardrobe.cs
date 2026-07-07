using UnityEngine;
using System.Collections.Generic;

// 각 친구(animalName)가 받은 선물 아이템 ID들을 저장
public static class Wardrobe
{
    static string Key(string animalName) => $"Wardrobe_{animalName}";

    public static void Add(string animalName, int itemId)
    {
        if (string.IsNullOrEmpty(animalName)) return;
        var owned = GetOwned(animalName);
        if (!owned.Contains(itemId))
        {
            owned.Add(itemId);
            PlayerPrefs.SetString(Key(animalName), string.Join(",", owned));
            PlayerPrefs.Save();
        }
    }

    public static List<int> GetOwned(string animalName)
    {
        var list = new List<int>();
        string s = PlayerPrefs.GetString(Key(animalName), "");
        if (string.IsNullOrEmpty(s)) return list;
        foreach (var p in s.Split(','))
            if (int.TryParse(p, out int id)) list.Add(id);
        return list;
    }
}

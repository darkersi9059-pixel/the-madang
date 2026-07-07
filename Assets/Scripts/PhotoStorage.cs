using System.IO;
using System.Collections.Generic;
using UnityEngine;

// 사진 메타데이터 (index.json에 저장)
[System.Serializable]
public class PhotoMeta
{
    public string fileName;
    public string animalName;
    public int animalType;   // (int)AnimalType
    public int score;
    public int rarity;       // (int)PhotoRarity
    public string category;  // 수집 카테고리 (예: 고양이/강아지/유령...)
    public long takenAtTicks;
}

[System.Serializable]
class PhotoIndex { public List<PhotoMeta> photos = new(); }

// 사진을 파일(PNG)로 저장/로드. 모바일(persistentDataPath) 안전.
public static class PhotoStorage
{
    public static string Dir => Path.Combine(Application.persistentDataPath, "Photos");
    static string IndexPath => Path.Combine(Dir, "index.json");

    static void EnsureDir()
    {
        if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
    }

    public static PhotoMeta Save(Texture2D tex, string animalName, int animalType,
                                 int score, int rarity, string category)
    {
        EnsureDir();
        string fileName = $"photo_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        File.WriteAllBytes(Path.Combine(Dir, fileName), tex.EncodeToPNG());

        var meta = new PhotoMeta
        {
            fileName = fileName,
            animalName = animalName,
            animalType = animalType,
            score = score,
            rarity = rarity,
            category = category,
            takenAtTicks = System.DateTime.Now.Ticks
        };

        var idx = LoadIndex();
        idx.photos.Add(meta);
        SaveIndex(idx);
        return meta;
    }

    public static List<PhotoMeta> AllMeta()
    {
        var list = LoadIndex().photos;
        list.Reverse(); // 최신 사진이 앞으로
        return list;
    }

    public static Sprite LoadSprite(string fileName)
    {
        string p = Path.Combine(Dir, fileName);
        if (!File.Exists(p)) return null;
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(p));
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    static PhotoIndex LoadIndex()
    {
        EnsureDir();
        if (!File.Exists(IndexPath)) return new PhotoIndex();
        try { return JsonUtility.FromJson<PhotoIndex>(File.ReadAllText(IndexPath)) ?? new PhotoIndex(); }
        catch { return new PhotoIndex(); }
    }

    static void SaveIndex(PhotoIndex idx)
    {
        EnsureDir();
        File.WriteAllText(IndexPath, JsonUtility.ToJson(idx));
    }
}

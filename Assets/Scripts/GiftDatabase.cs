using System.Collections.Generic;
using UnityEngine;

// 선물 아이템 1개
public class GiftItem
{
    public int id;
    public string name;
    public int price;
    public int affinityBonus;
    public string category;
    public Color color;
    public AnimalType? target; // null이면 공용

    public GiftItem(int id, string name, int price, int bonus, string category, Color color, AnimalType? target = null)
    {
        this.id = id; this.name = name; this.price = price;
        this.affinityBonus = bonus; this.category = category; this.color = color; this.target = target;
    }
}

// 4종 고정 선물. 가격 100/300/500/1000G, 친밀도 1/3/5/10.
// 상점은 이 4종을 항상 진열하되, 갱신 주기당 각 선물은 1회만 선물 가능(ShopManager).
public static class GiftDatabase
{
    static List<GiftItem> all;

    public static List<GiftItem> All
    {
        get { if (all == null) Build(); return all; }
    }

    public static GiftItem ById(int id) => All.Find(g => g.id == id);

    static Color C(float r, float g, float b) => new Color(r, g, b);

    static void Build()
    {
        all = new List<GiftItem>
        {
            new(1, "간식",       100,  1, "선물", C(0.95f, 0.75f, 0.45f)),
            new(2, "장난감",     300,  3, "선물", C(0.55f, 0.8f,  0.95f)),
            new(3, "포근한 방석", 500,  5, "선물", C(0.75f, 0.6f,  0.9f)),
            new(4, "특별한 선물", 1000, 10, "선물", C(1f,    0.8f,  0.35f)),
        };
    }
}

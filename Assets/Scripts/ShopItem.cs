using UnityEngine;

[CreateAssetMenu(fileName = "NewShopItem", menuName = "TheMadang/ShopItem")]
public class ShopItem : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    public int price;
    public int affinityBonus;
    public AnimalType[] targetAnimalTypes;
}

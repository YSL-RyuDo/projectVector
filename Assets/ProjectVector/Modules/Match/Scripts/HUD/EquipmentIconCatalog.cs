using UnityEngine;

[CreateAssetMenu(menuName = "Project Vector/Equipment Icons")]
public sealed class EquipmentIconCatalog : ScriptableObject
{
    public Sprite[] weapons = new Sprite[14];
    public Sprite[] armor = new Sprite[2];
    public Sprite device;
    public Sprite Weapon(int id) => id >= 0 && id < weapons.Length ? weapons[id] : null;
    public Sprite Armor(int id) => id >= 0 && id < armor.Length ? armor[id] : null;
}

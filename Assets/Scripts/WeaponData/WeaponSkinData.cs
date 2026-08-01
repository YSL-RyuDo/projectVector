using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponSkinData")]
public class WeaponSkinData : ScriptableObject
{
    [Header("식별")]
    public int skinId;
    public string skinName;

    [Header("검증용")]
    public WeaponData baseWeapon; // 잘못된 무기에 잘못된 스킨이 물리는 걸 방지하는 용도

    [Header("비주얼")]
    public GameObject visualModelPrefab; // VisualModel 자리에 들어갈 모델
    public Sprite skinIcon;
}
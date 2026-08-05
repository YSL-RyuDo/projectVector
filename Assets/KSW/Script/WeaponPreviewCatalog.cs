using System;
using UnityEngine;

public enum WeaponPreviewCategory
{
    Primary = 0,
    Secondary = 1,
    Melee = 2
}

[Serializable]
public sealed class WeaponPreviewEntry
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private GameObject previewPrefab;

    public WeaponData WeaponData => weaponData;
    public GameObject PreviewPrefab => previewPrefab;
    public int WeaponId => weaponData != null ? weaponData.weaponId : -1;
    public string DisplayName => weaponData != null ? weaponData.name : "Missing Weapon";
    public Sprite Icon => weaponData != null ? weaponData.weaponIcon : null;
    public WeaponPreviewCategory Category => ResolveCategory(weaponData);

    public static WeaponPreviewCategory ResolveCategory(WeaponData data)
    {
        if (data != null && data.weaponType == WeaponData.WeaponType.Pistol)
            return WeaponPreviewCategory.Secondary;

        if (data != null && data.weaponType == WeaponData.WeaponType.Knife)
            return WeaponPreviewCategory.Melee;

        return WeaponPreviewCategory.Primary;
    }
}

[CreateAssetMenu(fileName = "Weapon Preview Catalog", menuName = "UI/Weapon Preview Catalog")]
public sealed class WeaponPreviewCatalog : ScriptableObject
{
    [SerializeField] private WeaponPreviewEntry[] weapons;
    [SerializeField] private WeaponSkinDatabase skinDatabase;

    public int GetCount(WeaponPreviewCategory category)
    {
        int count = 0;

        for (int i = 0; i < (weapons?.Length ?? 0); i++)
        {
            WeaponPreviewEntry entry = weapons[i];
            if (entry != null && entry.WeaponData != null && entry.Category == category)
                count++;
        }

        return count;
    }

    public WeaponPreviewEntry GetEntry(WeaponPreviewCategory category, int categoryIndex)
    {
        if (categoryIndex < 0)
            return null;

        int currentIndex = 0;

        for (int i = 0; i < (weapons?.Length ?? 0); i++)
        {
            WeaponPreviewEntry entry = weapons[i];
            if (entry == null || entry.WeaponData == null || entry.Category != category)
                continue;

            if (currentIndex == categoryIndex)
                return entry;

            currentIndex++;
        }

        return null;
    }

    public WeaponPreviewEntry GetByServerId(int weaponId)
    {
        for (int i = 0; i < (weapons?.Length ?? 0); i++)
        {
            WeaponPreviewEntry entry = weapons[i];
            if (entry != null && entry.WeaponId == weaponId)
                return entry;
        }

        return null;
    }

    public int GetSkinCount(WeaponPreviewCategory category, int categoryIndex)
    {
        WeaponPreviewEntry entry = GetEntry(category, categoryIndex);
        if (entry == null || entry.WeaponData == null)
            return 0;

        // Skin index 0 is always the base preview prefab.
        return 1 + GetAdditionalSkinCount(entry.WeaponData);
    }

    public WeaponSkinData GetSkinData(WeaponPreviewCategory category, int categoryIndex, int skinIndex)
    {
        if (skinIndex <= 0)
            return null;

        WeaponPreviewEntry entry = GetEntry(category, categoryIndex);
        if (entry == null || entry.WeaponData == null || skinDatabase?.skins == null)
            return null;

        int requestedAdditionalIndex = skinIndex - 1;
        int currentAdditionalIndex = 0;

        for (int i = 0; i < skinDatabase.skins.Length; i++)
        {
            WeaponSkinData skin = skinDatabase.skins[i];
            if (skin == null || skin.baseWeapon != entry.WeaponData || skin.visualModelPrefab == null)
                continue;

            if (currentAdditionalIndex == requestedAdditionalIndex)
                return skin;

            currentAdditionalIndex++;
        }

        return null;
    }

    public GameObject GetPreviewPrefab(WeaponPreviewCategory category, int categoryIndex, int skinIndex)
    {
        WeaponPreviewEntry entry = GetEntry(category, categoryIndex);
        if (entry == null)
            return null;

        if (skinIndex <= 0)
            return entry.PreviewPrefab;

        WeaponSkinData skin = GetSkinData(category, categoryIndex, skinIndex);
        return skin != null && skin.visualModelPrefab != null
            ? skin.visualModelPrefab
            : entry.PreviewPrefab;
    }

    public int GetSkinId(WeaponPreviewCategory category, int categoryIndex, int skinIndex)
    {
        if (skinIndex <= 0)
            return 0;

        WeaponSkinData skin = GetSkinData(category, categoryIndex, skinIndex);
        return skin != null ? skin.skinId : 0;
    }

    public int FindSkinIndex(WeaponPreviewCategory category, int categoryIndex, int skinId)
    {
        if (skinId <= 0)
            return 0;

        int skinCount = GetSkinCount(category, categoryIndex);
        for (int skinIndex = 1; skinIndex < skinCount; skinIndex++)
        {
            WeaponSkinData skin = GetSkinData(category, categoryIndex, skinIndex);
            if (skin != null && skin.skinId == skinId)
                return skinIndex;
        }

        return 0;
    }

    private int GetAdditionalSkinCount(WeaponData weaponData)
    {
        if (weaponData == null || skinDatabase?.skins == null)
            return 0;

        int count = 0;
        for (int i = 0; i < skinDatabase.skins.Length; i++)
        {
            WeaponSkinData skin = skinDatabase.skins[i];
            if (skin != null && skin.baseWeapon == weaponData && skin.visualModelPrefab != null)
                count++;
        }

        return count;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (weapons == null)
            return;

        Array.Sort(weapons, (left, right) =>
        {
            int leftId = left?.WeaponId ?? int.MaxValue;
            int rightId = right?.WeaponId ?? int.MaxValue;
            return leftId.CompareTo(rightId);
        });

        for (int i = 1; i < weapons.Length; i++)
        {
            if (weapons[i - 1] != null && weapons[i] != null &&
                weapons[i - 1].WeaponId == weapons[i].WeaponId)
            {
                Debug.LogWarning($"Duplicate server weapon ID {weapons[i].WeaponId} in {name}.", this);
            }
        }
    }
#endif
}

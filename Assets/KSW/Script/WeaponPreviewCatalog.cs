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

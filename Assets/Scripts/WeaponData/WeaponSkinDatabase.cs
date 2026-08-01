using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponSkinDatabase")]
public class WeaponSkinDatabase : ScriptableObject
{
    public WeaponSkinData[] skins;

    private Dictionary<int, WeaponSkinData> lookup;

    public WeaponSkinData GetById(int skinId)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<int, WeaponSkinData>();
            foreach (var s in skins)
            {
                if (s != null)
                    lookup[s.skinId] = s;
            }
        }

        lookup.TryGetValue(skinId, out var data);
        return data;
    }
}
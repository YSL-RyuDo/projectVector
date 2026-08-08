using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponSkinDatabase")]
public class WeaponSkinDatabase : ScriptableObject
{
    public static WeaponSkinDatabase Instance { get; private set; }

    public WeaponSkinData[] skins;

    private Dictionary<long, WeaponSkinData> lookup;

    // 씬 시작 시 한 번 등록 (아래 참고)
    public void Register()
    {
        Instance = this;
    }

    public WeaponSkinData GetById(int weaponId, int skinId)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<long, WeaponSkinData>();
            foreach (WeaponSkinData skin in skins)
            {
                if (skin == null || skin.baseWeapon == null)
                    continue;

                lookup[MakeKey(skin.baseWeapon.weaponId, skin.skinId)] = skin;
            }
        }

        lookup.TryGetValue(MakeKey(weaponId, skinId), out WeaponSkinData data);
        return data;
    }

    private static long MakeKey(int weaponId, int skinId)
    {
        return ((long)weaponId << 32) | (uint)skinId;
    }
}
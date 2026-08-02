using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/WeaponSkinDatabase")]
public class WeaponSkinDatabase : ScriptableObject
{
    public static WeaponSkinDatabase Instance { get; private set; }

    public WeaponSkinData[] skins;

    private Dictionary<int, WeaponSkinData> lookup;

    // 씬 시작 시 한 번 등록 (아래 참고)
    public void Register()
    {
        Instance = this;
    }

    public WeaponSkinData GetById(int skinId)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<int, WeaponSkinData>();
            foreach (var s in skins)
                if (s != null) lookup[s.skinId] = s;
        }

        lookup.TryGetValue(skinId, out var data);
        return data;
    }
}
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkinLoadout : MonoBehaviour
{
    private Dictionary<int, int> equippedSkins = new Dictionary<int, int>(); // weaponId -> skinId
    private bool isLocked = false;

    public void SetSkin(int weaponId, int skinId)
    {
        if (isLocked)
        {
            Debug.LogWarning("[SKIN LOADOUT] locked. cannot change during match.");
            return;
        }

        equippedSkins[weaponId] = skinId;
    }

    public int GetSkinId(int weaponId)
    {
        return equippedSkins.TryGetValue(weaponId, out var id) ? id : 0;
    }

    // 매치 시작 시 서버가 확정 응답을 주면 호출해서 잠근다.
    public void Lock()
    {
        isLocked = true;
    }

    public void ApplyServerConfirmedLoadout(Dictionary<int, int> confirmed)
    {
        equippedSkins = new Dictionary<int, int>(confirmed);
        Lock();
    }
}
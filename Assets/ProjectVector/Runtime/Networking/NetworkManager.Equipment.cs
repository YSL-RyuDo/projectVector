using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Security.Principal;
using System.Text;

// Feature-specific wire handlers. Packet layouts and public methods are unchanged.
public partial class NetworkManager
{
    public void SendADSState(bool isADS)
    {
        ushort size = 4 + 1;
        ushort type = (ushort)PacketType.PKT_CS_ADS_STATE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        buffer[offset] = isADS ? (byte)1 : (byte)0;

        client.Send(buffer);

        Debug.Log($"[SEND ADS STATE] isADS={isADS}");
    }

    private void HandleADSState(byte[] body)
    {
        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool isADS =
            body[offset] != 0;
        offset += 1;

        Debug.Log(
            $"[ADS STATE] " +
            $"playerId={playerId}, isADS={isADS}"
        );

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(playerId);

        snapshot.isADS = isADS;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity != null)
        {
            FPSCameraLook look =
                identity.GetComponentInChildren<FPSCameraLook>(
                    true
                );

            if (look != null)
            {
                look.SetADSFromServer(
                    isADS
                );
            }

            PlayerItem item =
                identity.GetComponent<PlayerItem>();

            if (item == null)
            {
                item =
                    identity.GetComponentInChildren<PlayerItem>(
                        true
                    );
            }

            if (item != null)
            {
                item.SetADSVisualFromServer(
                    isADS
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[ADS STATE] PlayerIdentity not found. " +
                $"playerId={playerId}"
            );
        }

        bool isCurrentSpectatorTarget =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating &&
            SpectatorManager.Instance.CurrentTargetPlayerId == playerId;

        if (isCurrentSpectatorTarget &&
            InGameUIManager.Instance != null)
        {
            // 관전 중인 대상의 ADS 상태를 크로스헤어에 즉시 반영한다.
            InGameUIManager.Instance.SetDisplayedPlayerADS(isADS);
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }
    }
    private void ClearAllSpikeObjectsForNewRound()
    {
        DroppedSpikePickup[] droppedSpikes =
            FindObjectsByType<DroppedSpikePickup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (DroppedSpikePickup dropped in droppedSpikes)
        {
            if (dropped == null)
                continue;

            Rigidbody rb = dropped.GetComponentInParent<Rigidbody>();

            if (rb != null)
                Destroy(rb.gameObject);
            else
                Destroy(dropped.gameObject);
        }

        Spike[] plantedSpikes =
            FindObjectsByType<Spike>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Spike spike in plantedSpikes)
        {
            if (spike == null)
                continue;

            if (spike.IsLocalInteraction) spike.ForceCancelInteraction();
            Destroy(spike.gameObject);
        }

        Debug.Log("[ROUND RESET] all old spike objects cleared");
    }

    private void ApplySpikeDroppedToPlayer(uint dropperPlayerId)
    {
        PlayerIdentity identity = FindPlayerIdentityById(dropperPlayerId);

        if (identity == null)
        {
            Debug.LogWarning($"[SPIKE DROPPED] dropper identity not found. playerId={dropperPlayerId}");
            return;
        }

        PlayerSpike playerSpike = identity.GetComponent<PlayerSpike>();

        if (playerSpike == null)
            playerSpike = identity.GetComponentInChildren<PlayerSpike>(true);

        if (playerSpike == null)
        {
            Debug.LogWarning($"[SPIKE DROPPED] PlayerSpike missing. playerId={dropperPlayerId}");
            return;
        }

        playerSpike.SetHasSpikeFromServer(false);

        Debug.Log($"[SPIKE DROPPED APPLY] playerId={dropperPlayerId}, hasSpike=false");
    }

    public bool TryGetMyScoreboardMember(out ScoreboardMemberData myData)
    {
        myData = null;

        if (latestScoreboardData == null)
            return false;

        foreach (ScoreboardMemberData member in latestScoreboardData.members)
        {
            if (member.playerId == MyPlayerId)
            {
                myData = member;
                return true;
            }
        }

        return false;
    }

    public void SendWeaponDropRequest(
    int weaponId,
    Vector3 position,
    Vector3 direction)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_WEAPON_DROP_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(direction.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND WEAPON DROP REQUEST] weaponId={weaponId}, pos={position}, dir={direction}");
    }

    public void SendWeaponPickupRequest(uint dropId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_WEAPON_PICKUP_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(dropId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND WEAPON PICKUP REQUEST] dropId={dropId}");
    }

    private void HandleWeaponDropped(byte[] body)
    {
        const int expectedBodySize = 48;
        if (body == null || body.Length < expectedBodySize)
        {
            Debug.LogError($"[DROP SPAWN FAIL] Invalid packet size. received={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint dropId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint dropperPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int skinId = BitConverter.ToInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirX = BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirY = BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirZ = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError("[DROP SPAWN FAIL] WeaponDropSpawner.Instance is null");
            return;
        }

        Vector3 pos = new Vector3(x, y, z);
        Vector3 dir = new Vector3(dirX, dirY, dirZ);

        WeaponData data = WeaponDropSpawner.Instance.GetWeaponData(weaponId);
        if (data == null)
        {
            Debug.LogError($"[DROP SPAWN FAIL] weaponData not found. weaponId={weaponId}");
            return;
        }

        GameObject obj = WeaponDropSpawner.Instance.SpawnDroppedWeapon(
            dropId,
            weaponId,
            magAmmo,
            reserveAmmo,
            skinId,
            pos,
            dir
        );

        if (obj == null)
            return;

        RegisterDroppedItem(dropId, obj);

        PlayerIdentity dropperIdentity = FindPlayerIdentityById(dropperPlayerId);

        if (dropperIdentity != null)
        {
            PlayerItem dropperItem = dropperIdentity.GetComponent<PlayerItem>();

            if (dropperItem == null)
                dropperItem = dropperIdentity.GetComponentInChildren<PlayerItem>(true);

            if (dropperItem != null)
            {
                dropperItem.RemoveWeaponFromServer(weaponId);

                Debug.Log(
                    $"[DROP REMOVE HELD WEAPON] dropper={dropperPlayerId}, weaponId={weaponId}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[DROP REMOVE HELD WEAPON FAIL] PlayerItem missing. dropper={dropperPlayerId}"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[DROP REMOVE HELD WEAPON FAIL] PlayerIdentity missing. dropper={dropperPlayerId}"
            );
        }

        Debug.Log(
            $"[DROP SPAWN] dropId={dropId}, " +
            $"dropper={dropperPlayerId}, " +
            $"weaponId={weaponId}, " +
            $"weaponName={data.name}, " +
            $"skinId={skinId}, " +
            $"ammo={magAmmo}/{reserveAmmo}"
        );
    }

    private void HandleWeaponPickupNotify(byte[] body)
    {
        const int expectedBodySize = 24;
        if (body == null || body.Length < expectedBodySize)
        {
            Debug.LogError($"[WEAPON PICKUP NOTIFY] Invalid packet size. received={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint dropId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint pickerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int skinId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        if (dropId != 0)
        {
            RemoveDroppedItem(
                dropId
            );
        }

        if (WeaponDropSpawner.Instance == null)
        {
            Debug.LogError(
                "[WEAPON PICKUP NOTIFY] " +
                "WeaponDropSpawner.Instance is null"
            );

            return;
        }

        WeaponData data =
            WeaponDropSpawner.Instance.GetWeaponData(
                weaponId
            );

        if (data == null)
        {
            Debug.LogError(
                $"[WEAPON PICKUP NOTIFY] " +
                $"WeaponData not found. weaponId={weaponId}"
            );

            return;
        }

        PlayerIdentity pickerIdentity =
            FindPlayerIdentityById(
                pickerPlayerId
            );

        if (pickerIdentity == null)
        {
            Debug.LogWarning(
                $"[WEAPON PICKUP NOTIFY] " +
                $"picker not found. playerId={pickerPlayerId}"
            );

            return;
        }

        PlayerItem pickerItem =
            pickerIdentity.GetComponent<PlayerItem>();

        if (pickerItem == null)
        {
            pickerItem =
                pickerIdentity.GetComponentInChildren<PlayerItem>(
                    true
                );
        }

        if (pickerItem == null)
        {
            Debug.LogWarning(
                $"[WEAPON PICKUP NOTIFY] " +
                $"PlayerItem missing. playerId={pickerPlayerId}"
            );

            return;
        }

        pickerItem.ApplyPickedWeaponFromServer(
            data,
            magAmmo,
            reserveAmmo,
            skinId
        );

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(
                pickerPlayerId
            );

        snapshot.weaponId = weaponId;
        snapshot.magAmmo = magAmmo;
        snapshot.reserveAmmo = reserveAmmo;
        snapshot.isADS = false;

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                pickerPlayerId
            );
        }

        Debug.Log(
            $"[WEAPON PICKUP APPLY] " +
            $"dropId={dropId}, " +
            $"picker={pickerPlayerId}, " +
            $"weaponId={weaponId}, " +
            $"skinId={skinId}, " +
            $"ammo={magAmmo}/{reserveAmmo}"
        );
    }

    public void SendStoreBuyRequest(byte itemType, int itemId)
    {
        ushort size = 4 + 1 + 4;
        ushort type = (ushort)PacketType.PKT_CS_STORE_BUY_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        buffer[offset] = itemType;
        offset += 1;

        Buffer.BlockCopy(BitConverter.GetBytes(itemId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[STORE BUY REQUEST] itemType={itemType}, itemId={itemId}");
    }

    private void HandleStoreBuyResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        byte itemType = body[offset];
        offset += 1;

        int itemId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int price = BitConverter.ToInt32(body, offset);
        offset += 4;

        int remainCredits = BitConverter.ToInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int barrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxBarrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        byte purchasedSkillMask = body[offset];
        offset += 1;

        byte usedSkillMask = body[offset];
        offset += 1;

        SkillInventoryState.ApplyServerState(
            purchasedSkillMask,
            usedSkillMask
        );

        Debug.Log(
            $"[STORE BUY RESULT] result={result}, itemType={itemType}, itemId={itemId}, " +
            $"price={price}, credits={remainCredits}, weaponId={weaponId}, " +
            $"ammo={magAmmo}/{reserveAmmo}, barrier={barrier}/{maxBarrier}"
        );

        if (StorePanelUI.Instance != null)
        {
            StorePanelUI.Instance.SetCreditDirect(remainCredits);
        }

        if (result != 0)
        {
            Debug.LogWarning($"[STORE BUY FAILED] result={result}");
            return;
        }

        if (StorePanelUI.Instance != null)
        {
            if (itemType == 0)
            {
                // 무기 구매 성공
                StorePanelUI.Instance.SetOwnedWeaponFromServer(weaponId);
            }
            else if (itemType == 1)
            {
                // 방어구 구매 성공
                StorePanelUI.Instance.SetOwnedArmorFromServer(maxBarrier);
            }
        }

        Debug.Log("[STORE BUY SUCCESS]");
    }

    private void HandleSkillState(byte[] body)
    {
        if (body == null || body.Length < 2)
            return;

        byte purchasedSkillMask = body[0];
        byte usedSkillMask = body[1];

        SkillInventoryState.ApplyServerState(
            purchasedSkillMask,
            usedSkillMask
        );

        Debug.Log(
            $"[SKILL STATE] purchased=0x{purchasedSkillMask:X2}, used=0x{usedSkillMask:X2}"
        );
    }

    private byte GetMyTeamForKillLog()
    {
        // Scoreboard에서 받은 내 팀 정보가 가장 정확하다.
        if (latestScoreboardData != null)
            return latestScoreboardData.myTeam;

        PlayerIdentity myIdentity =
            FindPlayerIdentityById(MyPlayerId);

        if (myIdentity != null)
            return (byte)myIdentity.Team;

        Debug.LogWarning(
            "[KILL LOG] local team not found"
        );

        return 255;
    }

    private void RegisterDroppedItem(uint dropId, GameObject obj)
    {
        if (droppedItemMap.TryGetValue(dropId, out GameObject oldObj))
        {
            if (oldObj != null)
                Destroy(oldObj);
        }

        droppedItemMap[dropId] = obj;
    }

    private void RemoveDroppedItem(uint dropId)
    {
        if (!droppedItemMap.TryGetValue(dropId, out GameObject obj))
            return;

        bool destroyedBySpawner =
            WeaponDropSpawner.Instance != null &&
            WeaponDropSpawner.Instance.DestroyDroppedWeapon(dropId);

        if (!destroyedBySpawner && obj != null)
            Destroy(obj);

        droppedItemMap.Remove(dropId);
    }

    private PlayerItem FindMyPlayerItem()
    {
        PlayerIdentity identity = FindPlayerIdentityById(MyPlayerId);

        if (identity == null)
            return null;

        PlayerItem item = identity.GetComponent<PlayerItem>();

        if (item == null)
            item = identity.GetComponentInChildren<PlayerItem>(true);

        return item;
    }

    private PlayerCombatSnapshot GetOrCreateCombatSnapshot(
    uint playerId)
    {
        if (!playerCombatSnapshots.TryGetValue(
            playerId,
            out PlayerCombatSnapshot snapshot))
        {
            snapshot =
                new PlayerCombatSnapshot();

            playerCombatSnapshots[playerId] =
                snapshot;
        }

        return snapshot;
    }

    public bool TryGetCombatSnapshot(
        uint playerId,
        out PlayerCombatSnapshot snapshot)
    {
        return playerCombatSnapshots.TryGetValue(
            playerId,
            out snapshot
        );
    }

    private void HandleFireNotify(byte[] body)
    {
        if (body == null || body.Length < 8)
            return;

        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        if (playerId == MyPlayerId)
            return;

        PlayerIdentity identity = FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning($"[FIRE NOTIFY] player not found. playerId={playerId}");
            return;
        }

        PlayerItem playerItem = identity.GetComponent<PlayerItem>();

        if (playerItem == null)
            playerItem = identity.GetComponentInChildren<PlayerItem>(true);

        if (playerItem == null)
        {
            Debug.LogWarning($"[FIRE NOTIFY] PlayerItem not found. playerId={playerId}");
            return;
        }

        playerItem.PlayFireVisualFromServer(weaponId);
    }

    public void SendReloadVisual(
    int weaponId,
    bool isReloading)
    {
        ushort size =
            4 + 4 + 4;

        ushort type = (ushort)PacketType.PKT_CS_RELOAD_VISUAL;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(weaponId),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(
                isReloading ? 1 : 0
            ),
            0,
            buffer,
            offset,
            4
        );

        client.Send(buffer);
    }

    private void HandleReloadVisual(byte[] body)
    {
        if (body == null ||
            body.Length < 12)
        {
            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(
                body,
                offset
            );
        offset += 4;

        int weaponId =
            BitConverter.ToInt32(
                body,
                offset
            );
        offset += 4;

        bool isReloading =
            BitConverter.ToInt32(
                body,
                offset
            ) != 0;
        offset += 4;

        // 내 화면은 이미 로컬에서 처리했다.
        if (playerId == MyPlayerId)
            return;

        PlayerIdentity identity =
            FindPlayerIdentityById(
                playerId
            );

        if (identity == null)
            return;

        PlayerItem item =
            identity.GetComponent<PlayerItem>();

        if (item == null)
        {
            item =
                identity.GetComponentInChildren<PlayerItem>(
                    true
                );
        }

        if (item == null)
            return;

        item.SetReloadVisualFromServer(
            weaponId,
            isReloading
        );
    }

    private byte GetLocalTeamForRoundResult()
    {
        PlayerIdentity localPlayer =
            FindPlayerIdentityById(MyPlayerId);

        if (localPlayer != null)
            return localPlayer.Team;

        return 255;
    }

}

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
    public void SendSpikePickupRequest()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_SPIKE_PICKUP_REQUEST;

        byte[] buffer = new byte[size];

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log("[SEND] SPIKE PICKUP REQUEST");
    }

    public void SendSpikePlantComplete(Vector3 position)
    {
        ushort size = 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SPIKE_PLANT_COMPLETE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND] SPIKE PLANT COMPLETE pos={position}");
    }

    public void SendSpikePlantStart() => SendEmptyPacket(PacketType.PKT_CS_SPIKE_PLANT_START);
    public void SendSpikePlantCancel() => SendEmptyPacket(PacketType.PKT_CS_SPIKE_PLANT_CANCEL);

    private void HandleSpikePickupNotify(byte[] body)
    {
        int offset = 0;

        uint carrierPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte showCarrierInfo = body[offset];
        offset += 1;

        string carrierNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log($"[SPIKE PICKUP NOTIFY] carrier={carrierPlayerId}, show={showCarrierInfo}, nickname={carrierNickname}");

        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        foreach (PlayerIdentity identity in identities)
        {
            PlayerSpike ps = identity.GetComponent<PlayerSpike>();
            if (ps == null)
                continue;

            bool hasSpike = identity.PlayerId == carrierPlayerId;
            ps.SetHasSpikeFromServer(hasSpike);
        }

        // 핵심: 모든 클라에서 바닥 스파이크 제거
        DroppedSpikePickup[] droppedSpikes =
            FindObjectsByType<DroppedSpikePickup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (DroppedSpikePickup dropped in droppedSpikes)
        {
            Rigidbody rb = dropped.GetComponentInParent<Rigidbody>();

            if (rb != null)
                Destroy(rb.gameObject);
            else
                Destroy(dropped.transform.root.gameObject);
        }

        if (TopInfoUI.Instance != null)
        {
            if (showCarrierInfo != 0)
                TopInfoUI.Instance.SetGameDescription($"{carrierNickname} 디바이스 획득");
            else
                TopInfoUI.Instance.SetGameDescription("디바이스 획득됨");
        }
    }
    private void HandleSpikePlanted(byte[] body)
    {
        if (body == null || body.Length != 12) return;
        if (RoundManager.Instance != null && (RoundManager.Instance.currentState == RoundManager.RoundState.RoundEnd ||
            RoundManager.Instance.currentState == RoundManager.RoundState.GameOver)) return;
        int offset = 0;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 pos = new Vector3(x, y, z);
        SpikeInteractionPresentation.StopAll(false);

        Debug.Log($"[SPIKE PLANTED] pos={pos}");

        if (SpikeSpawner.Instance != null)
            SpikeSpawner.Instance.SpawnPlanted(pos);

        // The placement broadcast clears possession on every replicated actor,
        // including the remote planter's held inventory model.
        foreach(var actor in FindObjectsByType<PlayerSpike>(FindObjectsSortMode.None))
            if(actor.hasSpike) actor.SetHasSpikeFromServer(false);

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetGameDescription("디바이스 접속 중");
            TopInfoUI.Instance.SetUnknownTimer();
        }

        if (CombatFeedbackUI.Instance != null)
        {
            CombatFeedbackUI.Instance.HideSpikeProgress();

            CombatFeedbackUI.Instance.ShowObjectiveNotice(
                "디바이스가 연결되었습니다. AI 제어권 탈취 중입니다.",
                true
            );
        }
    }

    private PlayerSpike FindMyPlayerSpike()
    {
        PlayerIdentity[] players = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        foreach (PlayerIdentity identity in players)
        {
            if (identity.PlayerId == MyPlayerId)
                return identity.GetComponent<PlayerSpike>();
        }

        return null;
    }

    private void HandleSpikeDropped(byte[] body)
    {
        int offset = 0;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float dirX = BitConverter.ToSingle(body, offset); offset += 4;
        float dirY = BitConverter.ToSingle(body, offset); offset += 4;
        float dirZ = BitConverter.ToSingle(body, offset); offset += 4;

        uint dropperPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Vector3 pos = new Vector3(x, y, z);
        Vector3 dir = new Vector3(dirX, dirY, dirZ);

        if (SpikeSpawner.Instance != null)
            SpikeSpawner.Instance.SpawnDroppedAt(pos, dir, dropperPlayerId);

        // 핵심: 스파이크를 버린 플레이어는 더 이상 스파이크 소유자가 아님
        ApplySpikeDroppedToPlayer(dropperPlayerId);
    }

    private void HandleSpikeDefuseState(byte[] body)
    {
        if (body == null || body.Length != 6) return;
        int offset = 0;

        byte state = body[offset];
        offset += 1;

        byte halfDefused = body[offset];
        offset += 1;

        uint defuserPlayerId = BitConverter.ToUInt32(body, offset);

        SpikeInteractionPresentation.ServerDefuse(defuserPlayerId, state, halfDefused != 0);

        Spike[] spikes = FindObjectsByType<Spike>(FindObjectsSortMode.None);

        for (int i = 0; i < spikes.Length; i++)
        {
            if (!spikes[i].isPlanted) continue;
            spikes[i].GetComponent<SpikePresentation>()?.DefuseState(state, halfDefused != 0);
            if (state == 1)
            {
                if (defuserPlayerId == MyPlayerId)
                    spikes[i].StartDefuseFromServer(halfDefused != 0);
            }
            else if (state == 0)
            {
                spikes[i].CancelDefuseFromServer(halfDefused != 0);
            }
            else if (state == 2)
            {
                spikes[i].SetHalfDefusedFromServer(true);
            }
        }
    }

    private void HandleSpikeDefused(byte[] body)
    {
        if (body == null || body.Length != 4) return;
        SpikeInteractionPresentation.StopAll(false);
        int offset = 0;

        uint defuserPlayerId = BitConverter.ToUInt32(body, offset);

        Spike[] spikes = FindObjectsByType<Spike>(FindObjectsSortMode.None);

        for (int i = 0; i < spikes.Length; i++)
        {
            if (!spikes[i].isPlanted) continue;
            spikes[i].ForceCancelInteraction();
            spikes[i].GetComponent<SpikePresentation>()?.Resolve(false);
            foreach (var collider in spikes[i].GetComponentsInChildren<Collider>()) collider.enabled = false;
            spikes[i].isPlanted = false;
            Destroy(spikes[i].gameObject, 2f);
        }

        if (TopInfoUI.Instance != null)
            TopInfoUI.Instance.SetGameDescription("디바이스 접속 차단됨");

        if (CombatFeedbackUI.Instance != null)
        {
            CombatFeedbackUI.Instance.HideSpikeProgress();

            // 기존 설치 상태 아이콘 제거
            CombatFeedbackUI.Instance.ClearObjectiveState();

            // 해체 완료 메시지만 잠깐 표시
            CombatFeedbackUI.Instance.ShowObjectiveNotice(
                "수비팀이 디바이스의 접속을 차단했습니다.",
                false
            );
        }

        Debug.Log($"[SPIKE DEFUSED] defuser={defuserPlayerId}");
    }

    public void SendSpikeDefuseStart()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_START);
    }

    public void SendSpikeDefuseCancel()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_CANCEL);
    }

    public void SendSpikeDefuseHalf()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_HALF);
    }

    public void SendSpikeDefuseComplete()
    {
        SendEmptyPacket(PacketType.PKT_CS_SPIKE_DEFUSE_COMPLETE);
    }

    private void SendEmptyPacket(PacketType packetType)
    {
        ushort size = 4;
        ushort type = (ushort)packetType;

        byte[] buffer = new byte[size];

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log($"[SEND] {packetType}");
    }

    private void HandleDie(byte[] body)
    {
        if(body == null || body.Length != 58) return;
        int offset = 0;

        uint killerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint deadPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte killerTeam = body[offset];
        offset += 1;

        byte deadTeam = body[offset];
        offset += 1;

        int killerCharacterId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        int deadCharacterId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        string killerNickname =
            Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        string deadNickname =
            Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[DIE] killer={killerPlayerId}, " +
            $"dead={deadPlayerId}, " +
            $"killerTeam={killerTeam}, " +
            $"deadTeam={deadTeam}, " +
            $"killerNickname={killerNickname}, " +
            $"deadNickname={deadNickname}, " +
            $"killerCharacter={killerCharacterId}, " +
            $"deadCharacter={deadCharacterId}"
        );

        // 킬러가 정상적으로 존재하는 경우만 킬로그 표시
        if (killerPlayerId != 0 &&
            KillLogManager.Instance != null)
        {
            byte myTeam = GetMyTeamForKillLog();

            // 킬러가 내 팀이면 OurKillLogPrefab
            // 킬러가 상대 팀이면 EnemyKillLogPrefab
            bool isOurKill =
                killerTeam == myTeam;

            KillLogManager.Instance.ShowDeath(
                killerPlayerId, deadPlayerId, isOurKill,
                killerCharacterId,
                deadCharacterId,
                killerNickname,
                deadNickname
            );
        }

        PlayerIdentity deadIdentity =
            FindPlayerIdentityById(deadPlayerId);

        if (deadIdentity != null)
        {
            PlayerDeathController death =
                deadIdentity.GetComponent<PlayerDeathController>();

            if (death == null)
            {
                death =
                    deadIdentity.GetComponentInChildren<PlayerDeathController>(
                        true
                    );
            }

            if (death != null)
            {
                death.OnDead();
            }
            else
            {
                Debug.LogError(
                    $"[DIE] PlayerDeathController missing. " +
                    $"object={deadIdentity.name}"
                );
            }
        }

        if (deadPlayerId != MyPlayerId)
            return;

        PlayerIdentity myIdentity =
            FindPlayerIdentityById(MyPlayerId);

        if (myIdentity != null)
        {
            PlayerSpike playerSpike =
                myIdentity.GetComponent<PlayerSpike>();

            if (playerSpike == null)
            {
                playerSpike =
                    myIdentity.GetComponentInChildren<PlayerSpike>(true);
            }

            if (playerSpike != null)
            {
                playerSpike.ForceCancelPlant();
            }
            else
            {
                Debug.LogError(
                    "[DIE] PlayerSpike missing on local player"
                );
            }

            PlayerItem item =
                myIdentity.GetComponent<PlayerItem>();

            if (item != null)
                item.SetWeaponVisibility(false, this);

            FPSCameraLook cameraLook =
                myIdentity.GetComponentInChildren<FPSCameraLook>(true);

            if (cameraLook != null)
            {
                cameraLook.ForceExitADS();
            }
            else
            {
                Debug.LogError(
                    "[DIE] FPSCameraLook missing on local player"
                );
            }
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.StartSpectating();
        }
        else
        {
            Debug.LogError(
                "[DIE] SpectatorManager.Instance is null"
            );
        }

        BeginLocalCombatReport();
    }

    public void SendSpikeDropRequest(Vector3 position, Vector3 direction)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SPIKE_DROP_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

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

        Debug.Log($"[SEND] SPIKE DROP REQUEST pos={position}, dir={direction}");
    }

    private void ApplyTeamToPlayer(uint playerId, byte team)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            identity.Team = team;

            Debug.Log(
                $"[TEAM APPLY] playerId={playerId}, team={team}, object={identity.gameObject.name}"
            );

            return;
        }

        Debug.LogWarning(
            $"[TEAM APPLY FAIL] player not found. playerId={playerId}, team={team}"
        );
    }

    private void RefreshPlayerTagsByTeam()
    {
        PlayerIdentity myIdentity = null;

        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId == MyPlayerId)
            {
                myIdentity = identity;
                break;
            }
        }

        if (myIdentity == null)
            return;

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.Team == myIdentity.Team)
                identity.gameObject.tag = "Player";
            else
                identity.gameObject.tag = "Enemy";

            Debug.Log($"[TAG REFRESH] playerId={identity.PlayerId}, team={identity.Team}, tag={identity.gameObject.tag}");
        }
    }

    private PlayerIdentity FindPlayerIdentityById(uint playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PlayerIdentity identity in identities)
        {
            if (identity == null)
                continue;

            if (identity.PlayerId == playerId)
                return identity;
        }

        return null;
    }

    private void HandleWeaponSwitchNotify(byte[] body)
    {
        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        Debug.Log(
            $"[WEAPON SWITCH NOTIFY] " +
            $"playerId={playerId}, weaponId={weaponId}"
        );

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(playerId);

        snapshot.weaponId = weaponId;
        snapshot.isADS = false;

        // 로컬은 무기 Switch에서 이미 처리함
        if (playerId == MyPlayerId)
        {
            Debug.Log(
                $"[WEAPON SWITCH NOTIFY] " +
                $"ignored self. weaponId={weaponId}"
            );

            return;
        }

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity != null)
        {
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
                item.ApplyWeaponSwitchFromServer(
                    weaponId
                );

                // 무기 변경 후 기존 ADS 상태 해제
                item.SetADSVisualFromServer(false);
            }
            else
            {
                Debug.LogWarning(
                    $"[WEAPON SWITCH NOTIFY] " +
                    $"PlayerItem missing. playerId={playerId}"
                );
            }

            FPSCameraLook look =
                identity.GetComponentInChildren<FPSCameraLook>(
                    true
                );

            if (look != null)
            {
                look.SetADSFromServer(false);
            }
        }
        else
        {
            Debug.LogWarning(
                $"[WEAPON SWITCH NOTIFY] " +
                $"player not found. playerId={playerId}"
            );
        }

        bool isCurrentSpectatorTarget =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating &&
            SpectatorManager.Instance.CurrentTargetPlayerId == playerId;

        if (isCurrentSpectatorTarget &&
            InGameUIManager.Instance != null)
        {
            // 무기를 변경하면 서버 스냅샷의 ADS도 false로 초기화된다.
            InGameUIManager.Instance.SetDisplayedPlayerADS(false);
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }
    }
}

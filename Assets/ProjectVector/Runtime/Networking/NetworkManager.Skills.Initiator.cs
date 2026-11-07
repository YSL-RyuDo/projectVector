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
    public void SendDroneDeploy(
    float x,
    float y,
    float z,
    float yaw,
    float pitch)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_DEPLOY;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(yaw), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(pitch), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND DRONE DEPLOY] pos=({x}, {y}, {z}), yaw={yaw}, pitch={pitch}");
    }

    public void SendDroneState(
        uint droneId,
        float x,
        float y,
        float z,
        float yaw,
        float pitch)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_STATE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(yaw), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(pitch), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);
    }

    public void SendDroneExit(uint droneId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_EXIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND DRONE EXIT] droneId={droneId}");
    }

    private void HandleDroneSpawn(byte[] body)
    {
        if (body == null || body.Length < 28)
        {
            Debug.LogWarning($"[DRONE SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float yaw = BitConverter.ToSingle(body, offset);
        offset += 4;

        float pitch = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (DroneNetworkManager.Instance == null)
        {
            Debug.LogError("[DRONE SPAWN] DroneNetworkManager.Instance is null");
            return;
        }

        DroneNetworkManager.Instance.OnDroneSpawn(
            droneId,
            ownerPlayerId,
            x,
            y,
            z,
            yaw,
            pitch
        );



        Debug.Log($"[DRONE SPAWN] droneId={droneId}, owner={ownerPlayerId}");
    }

    private void HandleDroneState(byte[] body)
    {
        if (body == null || body.Length < 28)
            return;

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float yaw = BitConverter.ToSingle(body, offset);
        offset += 4;

        float pitch = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (DroneNetworkManager.Instance == null)
            return;

        DroneNetworkManager.Instance.OnDroneState(
            droneId,
            ownerPlayerId,
            x,
            y,
            z,
            yaw,
            pitch
        );
    }

    private void HandleDroneDestroy(byte[] body)
    {
        if (body == null || body.Length < 9)
        {
            Debug.LogWarning($"[DRONE DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte reason = body[offset];
        offset += 1;

        if (DroneNetworkManager.Instance == null)
        {
            Debug.LogError("[DRONE DESTROY] DroneNetworkManager.Instance is null");
            return;
        }

        DroneNetworkManager.Instance.OnDroneDestroy(
            droneId,
            ownerPlayerId,
            reason
        );

        Debug.Log($"[DRONE DESTROY] droneId={droneId}, owner={ownerPlayerId}, reason={reason}");
    }

    public void SendDroneHit(uint droneId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND DRONE HIT] droneId={droneId}");
    }

    public void SendDroneTaserFire(
    uint droneId,
    uint targetPlayerId)
    {
        ushort size = 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_DRONE_TASER_FIRE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(droneId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND DRONE TASER FIRE] " +
            $"droneId={droneId}, targetPlayerId={targetPlayerId}"
        );
    }

    private void HandleDroneTaserFire(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 1 + 1 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[DRONE TASER FIRE] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte hit = body[offset];
        offset += 1;

        byte stack = body[offset];
        offset += 1;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        int damage = BitConverter.ToInt32(body, offset);
        offset += 4;

        Debug.Log(
            $"[DRONE TASER FIRE] droneId={droneId}, " +
            $"owner={ownerPlayerId}, target={targetPlayerId}, " +
            $"hit={hit}, stack={stack}, duration={duration}, damage={damage}"
        );

        bool isMyDroneTaser =
            ownerPlayerId == MyPlayerId;

        bool successHit =
            hit != 0 &&
            targetPlayerId != 0;

        if (DroneNetworkManager.Instance != null &&
            DroneNetworkManager.Instance.TryGetActiveDroneByOwner(ownerPlayerId, out var firingDrone) &&
            firingDrone != null && firingDrone.DroneId == droneId)
            firingDrone.TaserGun?.PlayServerResult(successHit, successHit ? FindPlayerIdentityById(targetPlayerId)?.transform : null);

        // 핵심:
        // 서버 판정 결과를 내 드론 HUD에 반영
        if (isMyDroneTaser && DroneHUDUI.Instance != null)
        {
            if (successHit)
            {
                DroneHUDUI.Instance.PlayTaserHitConfirm();
            }
            else
            {
                DroneHUDUI.Instance.PlayTaserMiss();
            }
        }

        // 빗나감이면 HUD 피드백만 주고 종료
        if (!successHit)
            return;

        PlayerIdentity targetIdentity =
            FindPlayerIdentityById(targetPlayerId);

        if (targetIdentity == null)
        {
            Debug.LogWarning(
                $"[DRONE TASER FIRE] target player not found. " +
                $"targetPlayerId={targetPlayerId}"
            );
            return;
        }

        ElectricShock shock =
            targetIdentity.GetComponent<ElectricShock>();

        if (shock == null)
            shock = targetIdentity.GetComponentInChildren<ElectricShock>(true);

        if (shock == null)
        {
            Debug.LogWarning(
                $"[DRONE TASER FIRE] ElectricShock missing. " +
                $"targetPlayerId={targetPlayerId}"
            );
            return;
        }

        shock.Shock(duration);

        Debug.Log(
            $"[DRONE TASER APPLY] target={targetPlayerId}, " +
            $"duration={duration}, stack={stack}, damage={damage}"
        );

        // 나중에 ElectricShock에 스택 함수 만들면 이걸로 교체
        // shock.ApplyStack(stack, duration);
    }

    private void HandleDroneHpUpdate(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[DRONE HP UPDATE] invalid body size. " +
                $"size={(body == null ? 0 : body.Length)}"
            );
            return;
        }

        int offset = 0;

        uint droneId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int hp = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxHp = BitConverter.ToInt32(body, offset);
        offset += 4;

        if (DroneNetworkManager.Instance != null)
        {
            DroneNetworkManager.Instance.OnDroneHpUpdate(
                droneId,
                ownerPlayerId,
                hp,
                maxHp
            );
        }
    }

    public void SendScannerThrow(
    float x,
    float y,
    float z,
    float vx,
    float vy,
    float vz)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SCANNER_THROW;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(vx), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(vy), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(vz), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND SCANNER THROW] " +
            $"pos=({x},{y},{z}), velocity=({vx},{vy},{vz})"
        );
    }

    public void SendScannerAnchor(
        uint scannerId,
        float x,
        float y,
        float z)
    {
        ushort size = 4 + 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SCANNER_ANCHOR;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(scannerId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND SCANNER ANCHOR] " +
            $"scannerId={scannerId}, pos=({x},{y},{z})"
        );
    }

    private void HandleScannerSpawn(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning($"[SCANNER SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint scannerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float vx = BitConverter.ToSingle(body, offset);
        offset += 4;

        float vy = BitConverter.ToSingle(body, offset);
        offset += 4;

        float vz = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerSpawn(
                scannerId,
                ownerPlayerId,
                x,
                y,
                z,
                vx,
                vy,
                vz
            );
        }
    }

    private void HandleScannerAnchored(byte[] body)
    {
        const int requiredSize = 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint scannerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerAnchored(
                scannerId,
                x,
                y,
                z
            );
        }
    }

    private void HandleScannerZap(byte[] body)
    {
        const int requiredSize = 4 + 4 + 1 + 4;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint scannerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte stack = body[offset];
        offset += 1;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        // Older servers send the original 13-byte body. Newer servers include
        // the exact endpoint used for wall occlusion, including crouch height.
        Vector3? confirmedTarget = null;
        if (body.Length >= requiredSize + 12)
            confirmedTarget = new Vector3(BitConverter.ToSingle(body, offset),
                BitConverter.ToSingle(body, offset + 4), BitConverter.ToSingle(body, offset + 8));

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerZap(
                scannerId,
                targetPlayerId,
                stack,
                duration, confirmedTarget
            );
        }
    }

    private void HandleScannerDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint scannerId = BitConverter.ToUInt32(body, 0);

        if (ScannerNetworkManager.Instance != null)
        {
            ScannerNetworkManager.Instance.OnScannerDestroy(scannerId,body.Length>=5 && body[4]==1);
        }
    }

    public void SendScannerHit(uint scannerId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SCANNER_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(scannerId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SCANNER HIT] scannerId={scannerId}");
    }

    public void SendEmpCast()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_EMP_CAST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND EMP CAST]");
    }

    public void SendEmpGesture(bool preparing)
    {
        if(!IsConnected || !IsAuthenticated)return;
        var buffer=new byte[5];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)5),0,buffer,0,2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_EMP_GESTURE),0,buffer,2,2);
        buffer[4]=(byte)(preparing?1:0);client.Send(buffer);
    }
    public void SendScannerGesture(byte phase)
    {
        SendSkillGesture(PacketType.PKT_CS_SCANNER_GESTURE, phase);
    }
    public void SendSentinelGesture(byte phase)=>SendSkillGesture(PacketType.PKT_CS_SENTINEL_GESTURE,phase);
    public void SendDischargeGesture(bool preparing) => SendSkillGesture(PacketType.PKT_CS_DISCHARGE_GESTURE, (byte)(preparing ? 1 : 0));
    private void SendSkillGesture(PacketType type, byte phase)
    {
        if(!IsConnected || !IsAuthenticated || phase>2)return;
        var buffer=new byte[5];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)5),0,buffer,0,2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)type),0,buffer,2,2);
        buffer[4]=phase;client.Send(buffer);
    }

    private void HandleEmpCast(byte[] body)
    {
        if(body==null || body.Length<5 || body[4]>8 || body.Length<5+body[4]*4)return;
        int offset = 0;

        uint casterPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte targetCount = body[offset];
        offset += 1;

        uint[] targetPlayerIds = new uint[targetCount];

        for (int i = 0; i < targetCount; i++)
        {
            targetPlayerIds[i] =
                BitConverter.ToUInt32(body, offset);
            offset += 4;
        }

        PlayerIdentity caster =
            FindPlayerIdentityById(casterPlayerId);

        if (caster == null)
            return;

        InitiatorSkill_E skill =
            caster.GetComponent<InitiatorSkill_E>();

        if (skill == null)
            return;

        skill.PlayEmpEffect(targetPlayerIds);

        Debug.Log(
            $"[EMP CAST APPLY] caster={casterPlayerId}, targetCount={targetCount}"
        );
    }

    private void HandleElectricStatus(byte[] body)
    {
        if(body==null || body.Length<10)return;
        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte shockStack = body[offset];
        offset += 1;

        byte stunned = body[offset];
        offset += 1;

        float stunSeconds =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if(float.IsNaN(stunSeconds) || float.IsInfinity(stunSeconds) || stunSeconds<0)return;

        PlayerIdentity target =
            FindPlayerIdentityById(playerId);

        if (target == null)
        {
            Debug.LogWarning($"[ELECTRIC STATUS] target not found playerId={playerId}");
            return;
        }

        ElectricShock shock =
            target.GetComponent<ElectricShock>();

        if (shock == null)
            shock = target.GetComponentInChildren<ElectricShock>(true);

        if (shock != null)
        {
            shock.ApplyServerStatus(
                shockStack,
                stunned != 0,
                stunSeconds
            );
        }
        else
        {
            Debug.LogWarning($"[ELECTRIC STATUS] ElectricShock missing playerId={playerId}");
        }

        PlayerMovement movement =
            target.GetComponent<PlayerMovement>();

        if (movement == null)
            movement = target.GetComponentInChildren<PlayerMovement>(true);

        if (movement != null)
        {
            if (stunned != 0)
            {
                movement.ApplyStunFromServer(stunSeconds);
            }
            else if (shockStack == 0)
            {
                movement.ClearStunFromServer();
            }
        }
        else
        {
            Debug.LogWarning($"[ELECTRIC STATUS] PlayerMovement missing playerId={playerId}");
        }

        Debug.Log(
            $"[ELECTRIC STATUS APPLY] playerId={playerId}, " +
            $"stack={shockStack}, stunned={stunned}, stun={stunSeconds}"
        );
    }

    public void SendUltimateCast(Vector3 direction)
    {
        if (!IsConnected || !IsAuthenticated) return;
        ushort size = 4 + 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_ULTIMATE_CAST;

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

        Vector3 dir =
            direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;

        Buffer.BlockCopy(
            BitConverter.GetBytes(dir.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(dir.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(dir.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ULTIMATE CAST] dir={dir}"
        );
    }

    private void HandleUltimateCast(byte[] body)
    {
        if (body == null || body.Length < 46 || body[16] > 6) return;
        const int maxTargetCount = 6;

        int offset = 0;

        uint casterPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float dirX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dirZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        byte targetCount =
            body[offset];
        offset += 1;

        uint[] targetPlayerIds =
            new uint[targetCount];

        for (int i = 0; i < maxTargetCount; i++)
        {
            uint targetPlayerId =
                BitConverter.ToUInt32(body, offset);
            offset += 4;

            if (i < targetCount)
            {
                targetPlayerIds[i] =
                    targetPlayerId;
            }
        }

        int damage =
            BitConverter.ToInt32(body, offset);
        offset += 4;

        byte stack =
            body[offset];
        offset += 1;

        PlayerIdentity caster =
            FindPlayerIdentityById(casterPlayerId);

        if (caster == null)
        {
            Debug.LogWarning(
                $"[ULTIMATE CAST] caster not found. caster={casterPlayerId}"
            );

            return;
        }

        InitiatorSkill_X skill =
            caster.GetComponent<InitiatorSkill_X>();

        if (skill == null)
            skill = caster.GetComponentInChildren<InitiatorSkill_X>(true);

        if (skill != null)
        {
            skill.PlayUltimateEffect(
                targetPlayerIds,
                new Vector3(dirX, dirY, dirZ)
            );
        }

        Debug.Log(
            $"[ULTIMATE CAST APPLY] caster={casterPlayerId}, " +
            $"targetCount={targetCount}, damage={damage}, stack={stack}"
        );
    }

}

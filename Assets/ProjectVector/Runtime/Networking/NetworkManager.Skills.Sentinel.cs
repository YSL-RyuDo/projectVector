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
    public void SendSentinelCThrow(Vector3 position, Vector3 velocity)
    {
        ushort size = 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_THROW;

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

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C THROW] pos={position}, velocity={velocity}");
    }

    public void SendSentinelCAnchor(uint trapId, Vector3 position, Vector3 normal)
    {
        ushort size = 4 + 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_ANCHOR;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C ANCHOR] trapId={trapId}, pos={position}, normal={normal}");
    }

    public void SendSentinelCTrigger(uint trapId, Vector3 position, float radius, float revealDuration)
    {
        ushort size = 4 + 4 + 12 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_TRIGGER;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(radius), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(revealDuration), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C TRIGGER] trapId={trapId}, pos={position}, radius={radius}");
    }

    public void SendSentinelCHit(uint trapId, float damage)
    {
        ushort size = 4 + 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_C_HIT;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(damage), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL C HIT] trapId={trapId}, damage={damage}");
    }

    private void HandleSentinelCSpawn(byte[] body)
    {
        if (body == null || body.Length < 37)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        Vector3 velocity = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float hp = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnSpawn(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                velocity,
                hp
            );
        }
    }

    private void HandleSentinelCAnchor(byte[] body)
    {
        if (body == null || body.Length < 28)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        Vector3 normal = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        if (SentinelCNetworkManager.Instance != null)
            SentinelCNetworkManager.Instance.OnAnchor(trapId, position, normal);
    }

    private void HandleSentinelCTrigger(byte[] body)
    {
        if (body == null || body.Length < 29)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float radius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float revealDuration = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnTrigger(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                radius,
                revealDuration
            );
        }
    }

    private void HandleSentinelCDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint trapId = BitConverter.ToUInt32(body, 0);

        if (SentinelCNetworkManager.Instance != null)
            SentinelCNetworkManager.Instance.OnDestroyTrap(trapId);
    }

    public void SendSentinelQPlace(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_Q_PLACE;

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

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL Q PLACE] position={position}");
    }

    public void SendSentinelQActivate(uint trapId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_Q_ACTIVATE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL Q ACTIVATE] trapId={trapId}");
    }

    public void SendSentinelQHit(uint trapId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_SENTINEL_Q_HIT;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND SENTINEL Q HIT] trapId={trapId}");
    }

    private void HandleSentinelQSpawn(byte[] body)
    {
        const int requiredSize = 33;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float hp = BitConverter.ToSingle(body, offset);
        offset += 4;

        float detectionRadius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float zoneRadius = BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnQSpawn(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                hp,
                detectionRadius,
                zoneRadius
            );
        }
    }

    private void HandleSentinelQActivate(byte[] body)
    {
        const int requiredSize = 12;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowMultiplier = BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnQActivate(
                trapId,
                duration,
                slowMultiplier
            );
        }
    }

    private void HandleSentinelQDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint trapId = BitConverter.ToUInt32(body, 0);

        if (SentinelCNetworkManager.Instance != null)
            SentinelCNetworkManager.Instance.OnQDestroy(trapId);
    }

    public void SendSentinelEPlace(
        Vector3 position,
        Vector3 normal)
    {
        ushort size = 4 + 24;
        ushort type =
            (ushort)PacketType.PKT_CS_SENTINEL_E_PLACE;

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

        float[] values =
        {
            position.x,
            position.y,
            position.z,
            normal.x,
            normal.y,
            normal.z
        };

        foreach (float value in values)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(value),
                0,
                buffer,
                offset,
                4
            );
            offset += 4;
        }

        client.Send(buffer);

        Debug.Log(
            $"[SEND SENTINEL E PLACE] " +
            $"position={position}, normal={normal}"
        );
    }

    public void SendSentinelEActivate(uint trapId)
    {
        SendSentinelETrapIdPacket(
            PacketType.PKT_CS_SENTINEL_E_ACTIVATE,
            trapId,
            "ACTIVATE"
        );
    }

    public void SendSentinelEHit(uint trapId)
    {
        SendSentinelETrapIdPacket(
            PacketType.PKT_CS_SENTINEL_E_HIT,
            trapId,
            "HIT"
        );
    }

    public void SendSentinelERecall(uint trapId)
    {
        SendSentinelETrapIdPacket(
            PacketType.PKT_CS_SENTINEL_E_RECALL,
            trapId,
            "RECALL"
        );
    }

    private void SendSentinelETrapIdPacket(
        PacketType packetType,
        uint trapId,
        string action)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)packetType;

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
            BitConverter.GetBytes(trapId),
            0,
            buffer,
            offset,
            4
        );

        client.Send(buffer);

        Debug.Log(
            $"[SEND SENTINEL E {action}] trapId={trapId}"
        );
    }

    private void HandleSentinelESpawn(byte[] body)
    {
        const int requiredSize = 41;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        Vector3 normal = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float hp = BitConverter.ToSingle(body, offset);
        offset += 4;

        float radius = BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnESpawn(
                trapId,
                ownerPlayerId,
                ownerTeam,
                position,
                normal,
                hp,
                radius
            );
        }
    }

    private void HandleSentinelEActivate(byte[] body)
    {
        const int requiredSize = 20;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float radius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowDuration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowMultiplier =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float stunDuration =
            BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnEActivate(
                trapId,
                radius,
                slowDuration,
                slowMultiplier,
                stunDuration
            );
        }
    }

    private void HandleSentinelEDestroy(byte[] body)
    {
        const int requiredSize = 9;

        if (body == null || body.Length < requiredSize)
            return;

        uint trapId = BitConverter.ToUInt32(body, 0);
        int reason = body[4];
        float cooldownSeconds =
            BitConverter.ToSingle(body, 5);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnEDestroy(
                trapId,
                reason,
                cooldownSeconds
            );
        }
    }

    public void SendSentinelXCast(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type =
            (ushort)PacketType.PKT_CS_SENTINEL_X_CAST;

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

        float[] values =
        {
            position.x,
            position.y,
            position.z
        };

        foreach (float value in values)
        {
            Buffer.BlockCopy(
                BitConverter.GetBytes(value),
                0,
                buffer,
                offset,
                4
            );
            offset += 4;
        }

        client.Send(buffer);

        Debug.Log(
            $"[SEND SENTINEL X CAST] position={position}"
        );
    }

    private void HandleSentinelXSpawn(byte[] body)
    {
        const int requiredSize = 37;

        if (body == null || body.Length < requiredSize)
            return;

        int offset = 0;

        uint ultimateId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        int ownerTeam = body[offset];
        offset += 1;

        Vector3 position = new Vector3(
            BitConverter.ToSingle(body, offset),
            BitConverter.ToSingle(body, offset + 4),
            BitConverter.ToSingle(body, offset + 8)
        );
        offset += 12;

        float radius = BitConverter.ToSingle(body, offset);
        offset += 4;

        float duration = BitConverter.ToSingle(body, offset);
        offset += 4;

        float slowMultiplier =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float protectionDuration =
            BitConverter.ToSingle(body, offset);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnXSpawn(
                ultimateId,
                ownerPlayerId,
                ownerTeam,
                position,
                radius,
                duration,
                slowMultiplier,
                protectionDuration
            );
        }
    }

    private void HandleSentinelXDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
            return;

        uint ultimateId = BitConverter.ToUInt32(body, 0);

        if (SentinelCNetworkManager.Instance != null)
        {
            SentinelCNetworkManager.Instance.OnXDestroy(
                ultimateId
            );
        }
    }

    // 호감도 레벨 송수신

}

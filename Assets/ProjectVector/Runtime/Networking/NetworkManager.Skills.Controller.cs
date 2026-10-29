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
    public void SendControllerSwampThrow(
    Vector3 position,
    Vector3 velocity)
    {
        ushort size =
            4 + 12 + 12;

        ushort type =
            (ushort)PacketType.PKT_CS_CONTROLLER_SWAMP_THROW;

        byte[] buffer =
            new byte[size];

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

        Debug.Log(
            $"[SEND CONTROLLER SWAMP THROW] pos={position}, velocity={velocity}"
        );
    }

    public void SendControllerSwampActivate(uint trapId, Vector3 position, Vector3 normal)
    {
        ushort size = 4 + 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_SWAMP_ACTIVATE;
        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(trapId), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);
        Debug.Log($"[SEND CONTROLLER SWAMP ACTIVATE] trapId={trapId}, pos={position}, normal={normal}");
    }

    private void HandleControllerSwampSpawn(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 12 + 12;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[CONTROLLER SWAMP SPAWN] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint trapId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float y =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float z =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vx =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vy =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float vz =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if (ControllerSwampNetworkManager.Instance != null)
        {
            ControllerSwampNetworkManager.Instance.OnSwampSpawn(
                trapId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Vector3(vx, vy, vz)
            );
        }
        else
        {
            Debug.LogError(
                "[CONTROLLER SWAMP SPAWN] ControllerSwampNetworkManager.Instance is null"
            );
        }
    }

    private void HandleControllerSwampActivate(byte[] body)
    {
        const int requiredSize = 40;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning($"[SWAMP ACTIVATE] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint trapId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float normalX = BitConverter.ToSingle(body, offset); offset += 4;
        float normalY = BitConverter.ToSingle(body, offset); offset += 4;
        float normalZ = BitConverter.ToSingle(body, offset); offset += 4;

        float radius = BitConverter.ToSingle(body, offset); offset += 4;
        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(x, y, z);
        Vector3 normal = new Vector3(normalX, normalY, normalZ);

        Debug.Log($"[RECV SWAMP ACTIVATE] trapId={trapId}, owner={ownerPlayerId}, pos={position}, normal={normal}, radius={radius}, duration={duration}");

        if (ControllerSwampNetworkManager.Instance != null)
        {
            ControllerSwampNetworkManager.Instance.OnSwampActivate(trapId, ownerPlayerId, position, normal, radius, duration);
        }
    }

    private void HandleControllerSwampDestroy(byte[] body)
    {
        if (body == null ||
            body.Length < 4)
        {
            return;
        }

        uint trapId =
            BitConverter.ToUInt32(body, 0);

        if (ControllerSwampNetworkManager.Instance != null)
        {
            ControllerSwampNetworkManager.Instance.OnSwampDestroy(
                trapId
            );
        }
    }

    public void SendControllerSmokeDrop(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_SMOKE_DROP;
        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);
        Debug.Log($"[SEND CONTROLLER SMOKE DROP] pos={position}");
    }

    private void HandleControllerSmokeSpawn(byte[] body)
    {
        if (body == null || body.Length < 28)
        {
            Debug.LogWarning($"[CONTROLLER SMOKE SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint smokeId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float radius = BitConverter.ToSingle(body, offset); offset += 4;
        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        if (ControllerSmokeNetworkManager.Instance != null)
        {
            ControllerSmokeNetworkManager.Instance.OnSmokeSpawn(smokeId, ownerPlayerId, new Vector3(x, y, z), radius, duration);
        }
    }

    private void HandleControllerSmokeDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
        {
            Debug.LogWarning($"[CONTROLLER SMOKE DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        uint smokeId = BitConverter.ToUInt32(body, 0);

        if (ControllerSmokeNetworkManager.Instance != null)
        {
            ControllerSmokeNetworkManager.Instance.OnSmokeDestroy(smokeId);
        }
    }

    public void SendControllerDecoyThrow(Vector3 position, Vector3 velocity)
    {
        ushort size = 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_DECOY_THROW;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(velocity.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(velocity.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(velocity.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND CONTROLLER DECOY THROW] pos={position}, velocity={velocity}");
    }

    public void SendControllerDecoyActivate(uint decoyId, Vector3 position, Vector3 normal)
    {
        ushort size = 4 + 4 + 12 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_DECOY_ACTIVATE;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(decoyId), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(normal.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(normal.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND CONTROLLER DECOY ACTIVATE] decoyId={decoyId}, pos={position}, normal={normal}");
    }

    public void SendControllerDecoyTrigger(uint decoyId)
    {
        if (!IsConnected) return;
        byte[] buffer = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)8), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_CONTROLLER_DECOY_TRIGGER), 0, buffer, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(decoyId), 0, buffer, 4, 4);
        client.Send(buffer);
    }

    private void HandleControllerDecoySpawn(byte[] body)
    {
        if (body == null || body.Length < 32)
        {
            Debug.LogWarning($"[RECV CONTROLLER DECOY SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint decoyId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float px = BitConverter.ToSingle(body, offset); offset += 4;
        float py = BitConverter.ToSingle(body, offset); offset += 4;
        float pz = BitConverter.ToSingle(body, offset); offset += 4;

        float vx = BitConverter.ToSingle(body, offset); offset += 4;
        float vy = BitConverter.ToSingle(body, offset); offset += 4;
        float vz = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(px, py, pz);
        Vector3 velocity = new Vector3(vx, vy, vz);

        if (ControllerDecoyNetworkManager.Instance != null)
        {
            ControllerDecoyNetworkManager.Instance.OnDecoySpawn(
                decoyId,
                ownerPlayerId,
                position,
                velocity
            );
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER DECOY SPAWN] ControllerDecoyNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER DECOY SPAWN] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, velocity={velocity}");
    }

    private void HandleControllerDecoyActivate(byte[] body)
    {
        if (body == null || body.Length < 36)
        {
            Debug.LogWarning($"[RECV CONTROLLER DECOY ACTIVATE] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint decoyId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float px = BitConverter.ToSingle(body, offset); offset += 4;
        float py = BitConverter.ToSingle(body, offset); offset += 4;
        float pz = BitConverter.ToSingle(body, offset); offset += 4;

        float nx = BitConverter.ToSingle(body, offset); offset += 4;
        float ny = BitConverter.ToSingle(body, offset); offset += 4;
        float nz = BitConverter.ToSingle(body, offset); offset += 4;

        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(px, py, pz);
        Vector3 normal = new Vector3(nx, ny, nz);

        if (ControllerDecoyNetworkManager.Instance != null)
        {
            ControllerDecoyNetworkManager.Instance.OnDecoyActivate(
                decoyId,
                ownerPlayerId,
                position,
                normal,
                duration
            );
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER DECOY ACTIVATE] ControllerDecoyNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER DECOY ACTIVATE] decoyId={decoyId}, owner={ownerPlayerId}, pos={position}, normal={normal}, duration={duration}");
    }

    private void HandleControllerDecoyDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
        {
            Debug.LogWarning($"[RECV CONTROLLER DECOY DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint decoyId = BitConverter.ToUInt32(body, offset); offset += 4;

        if (ControllerDecoyNetworkManager.Instance != null)
        {
            ControllerDecoyNetworkManager.Instance.OnDecoyDestroy(decoyId);
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER DECOY DESTROY] ControllerDecoyNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER DECOY DESTROY] decoyId={decoyId}");
    }

    public void SendControllerUltimateCast(Vector3 position)
    {
        ushort size = 4 + 12;
        ushort type = (ushort)PacketType.PKT_CS_CONTROLLER_ULTIMATE_CAST;

        byte[] buffer = new byte[size];
        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2); offset += 2;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2); offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, buffer, offset, 4); offset += 4;
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, buffer, offset, 4); offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND CONTROLLER ULTIMATE CAST] pos={position}");
    }

    private void HandleControllerUltimateSpawn(byte[] body)
    {
        if (body == null || body.Length < 28)
        {
            Debug.LogWarning($"[RECV CONTROLLER ULTIMATE SPAWN] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint ultimateId = BitConverter.ToUInt32(body, offset); offset += 4;
        uint ownerPlayerId = BitConverter.ToUInt32(body, offset); offset += 4;

        float x = BitConverter.ToSingle(body, offset); offset += 4;
        float y = BitConverter.ToSingle(body, offset); offset += 4;
        float z = BitConverter.ToSingle(body, offset); offset += 4;

        float radius = BitConverter.ToSingle(body, offset); offset += 4;
        float duration = BitConverter.ToSingle(body, offset); offset += 4;

        Vector3 position = new Vector3(x, y, z);

        if (ControllerUltimateNetworkManager.Instance != null)
        {
            ControllerUltimateNetworkManager.Instance.OnUltimateSpawn(
                ultimateId,
                ownerPlayerId,
                position,
                radius,
                duration
            );
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER ULTIMATE SPAWN] ControllerUltimateNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER ULTIMATE SPAWN] ultimateId={ultimateId}, owner={ownerPlayerId}, pos={position}, radius={radius}, duration={duration}");
    }

    private void HandleControllerUltimateDestroy(byte[] body)
    {
        if (body == null || body.Length < 4)
        {
            Debug.LogWarning($"[RECV CONTROLLER ULTIMATE DESTROY] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint ultimateId = BitConverter.ToUInt32(body, offset); offset += 4;

        if (ControllerUltimateNetworkManager.Instance != null)
        {
            ControllerUltimateNetworkManager.Instance.OnUltimateDestroy(ultimateId);
        }
        else
        {
            Debug.LogWarning("[RECV CONTROLLER ULTIMATE DESTROY] ControllerUltimateNetworkManager.Instance is null");
        }

        Debug.Log($"[RECV CONTROLLER ULTIMATE DESTROY] ultimateId={ultimateId}");
    }

}

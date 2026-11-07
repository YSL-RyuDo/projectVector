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
    public void SendBeastStealthCast()
    {
        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_BEAST_STEALTH_CAST;

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

        client.Send(buffer);

        Debug.Log("[SEND BEAST STEALTH CAST]");
    }

    public void SendBeastStealthCancel()
    {
        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_BEAST_STEALTH_CANCEL;

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

        client.Send(buffer);

        Debug.Log("[SEND BEAST STEALTH CANCEL]");
    }

    private void HandleBeastStealthState(byte[] body)
    {
        const int requiredSize = 4 + 1 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[BEAST STEALTH STATE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool active =
            body[offset] != 0;
        offset += 1;

        float duration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float speedMultiplier =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float revealRange =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity target =
            FindPlayerIdentityById(playerId);

        if (target == null)
        {
            Debug.LogWarning(
                $"[BEAST STEALTH STATE] player not found. playerId={playerId}"
            );

            return;
        }

        EntrySkill_E skill =
            target.GetComponent<EntrySkill_E>();

        if (skill == null)
            skill = target.GetComponentInChildren<EntrySkill_E>(true);

        if (skill == null)
        {
            Debug.LogWarning(
                $"[BEAST STEALTH STATE] EntrySkill_E missing. playerId={playerId}"
            );

            return;
        }

        skill.ApplyStealthStateFromServer(
            active,
            duration,
            speedMultiplier,
            revealRange
        );

        Debug.Log(
            $"[BEAST STEALTH STATE APPLY] playerId={playerId}, " +
            $"active={active}, duration={duration}, " +
            $"speedMultiplier={speedMultiplier}, revealRange={revealRange}"
        );
    }

    public void SendEntrySmokeThrow(
    Vector3 position,
    Vector3 velocity)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_SMOKE_THROW;

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
            $"[SEND ENTRY SMOKE THROW] pos={position}, velocity={velocity}"
        );
    }

    public void SendEntrySmokeAttach(
        uint smokeId,
        Vector3 position,
        Vector3 normal)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_SMOKE_ATTACH;

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

        Buffer.BlockCopy(BitConverter.GetBytes(smokeId), 0, buffer, offset, 4);
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

        Debug.Log(
            $"[SEND ENTRY SMOKE ATTACH] smokeId={smokeId}, pos={position}, normal={normal}"
        );
    }

    private void HandleEntrySmokeSpawn(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY SMOKE SPAWN] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint smokeId =
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

        if (EntrySmokeNetworkManager.Instance != null)
        {
            var entry = FindPlayerIdentityById(ownerPlayerId)?.GetComponent<EntrySkillPresentation>();
            if (entry && !entry.IsLocal) entry.Begin(EntrySkillPresentation.Cast.Smoke, .35f);
            EntrySmokeNetworkManager.Instance.OnSmokeSpawn(
                smokeId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Vector3(vx, vy, vz)
            );
        }
    }

    private void HandleEntrySmokeActivate(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY SMOKE ACTIVATE] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint smokeId =
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

        float nx =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float ny =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float nz =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float radius =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float duration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        if (EntrySmokeNetworkManager.Instance != null)
        {
            EntrySmokeNetworkManager.Instance.OnSmokeActivate(
                smokeId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Vector3(nx, ny, nz),
                radius,
                duration
            );
        }
    }

    private void HandleEntrySmokeDestroy(byte[] body)
    {
        if (body == null ||
            body.Length < 4)
            return;

        uint smokeId =
            BitConverter.ToUInt32(body, 0);

        if (EntrySmokeNetworkManager.Instance != null)
        {
            EntrySmokeNetworkManager.Instance.OnSmokeDestroy(
                smokeId
            );
        }
    }

    public void SendEntrySmokeState(
    uint smokeId,
    Vector3 position,
    Quaternion rotation)
    {
        // header 4
        // smokeId 4
        // position 12
        // rotation 16
        // total = 36
        ushort size = 4 + 4 + 12 + 16;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_SMOKE_STATE;

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

        Buffer.BlockCopy(
            BitConverter.GetBytes(smokeId),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(position.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(position.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(position.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(rotation.w),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY SMOKE STATE] smokeId={smokeId}, pos={position}"
        );
    }
    private void HandleEntrySmokeState(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY SMOKE STATE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint smokeId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint ownerPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotX = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotY = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotZ = BitConverter.ToSingle(body, offset);
        offset += 4;

        float rotW = BitConverter.ToSingle(body, offset);
        offset += 4;

        if (EntrySmokeNetworkManager.Instance != null)
        {
            EntrySmokeNetworkManager.Instance.OnSmokeState(
                smokeId,
                ownerPlayerId,
                new Vector3(x, y, z),
                new Quaternion(rotX, rotY, rotZ, rotW)
            );
        }
    }

    public void SendEntryDashCast(Vector3 direction)
    {
        ushort size =
            4 + 4 + 4 + 4;

        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_DASH_CAST;

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

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY DASH CAST] dir={direction}"
        );
    }

    private void HandleEntryDashCast(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 4 + 4 + 4 + 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY DASH CAST] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint playerId =
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

        float dashForce =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float dashDuration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning(
                $"[ENTRY DASH CAST] player not found playerId={playerId}"
            );

            return;
        }

        EntryDashMotor dashMotor =
            identity.GetComponent<EntryDashMotor>();

        if (dashMotor == null)
            dashMotor =
                identity.GetComponentInChildren<EntryDashMotor>(true);

        if (dashMotor == null)
            dashMotor =
                identity.gameObject.AddComponent<EntryDashMotor>();

        dashMotor.PlayDash(
            new Vector3(dirX, dirY, dirZ),
            dashForce,
            dashDuration
        );

        Debug.Log(
            $"[ENTRY DASH CAST APPLY] playerId={playerId}, " +
            $"dir=({dirX}, {dirY}, {dirZ}), force={dashForce}, duration={dashDuration}"
        );
    }

    private void HandleEntryDashEnd(byte[] body)
    {
        const int requiredSize = 4;

        if (body == null || body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY DASH END] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        uint playerId =
            BitConverter.ToUInt32(body, 0);

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity == null)
            return;

        EntryDashMotor dashMotor =
            identity.GetComponent<EntryDashMotor>();

        if (dashMotor == null)
            dashMotor =
                identity.GetComponentInChildren<EntryDashMotor>(true);

        if (dashMotor != null)
        {
            dashMotor.ForceEndDash();
        }

        Debug.Log(
            $"[ENTRY DASH END APPLY] playerId={playerId}"
        );
    }

    public void SendEntryBladeActivate()
    {
        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_ENTRY_BLADE_ACTIVATE;

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

        client.Send(buffer);

        Debug.Log("[SEND ENTRY BLADE ACTIVATE]");
    }

    public void SendEntryBladeFire(
        Vector3 direction,
        uint targetPlayerId,
        Vector3 hitPoint,
        bool burst = false)
    {
        ushort size =
            4 + 12 + 4 + 12; // header + dir + targetId + hitPoint = 32

        ushort type =
            (ushort)(burst ? PacketType.PKT_CS_ENTRY_BLADE_BURST : PacketType.PKT_CS_ENTRY_BLADE_FIRE);

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

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(direction.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(targetPlayerId),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(hitPoint.x),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(hitPoint.y),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        Buffer.BlockCopy(
            BitConverter.GetBytes(hitPoint.z),
            0,
            buffer,
            offset,
            4
        );
        offset += 4;

        client.Send(buffer);

        Debug.Log(
            $"[SEND ENTRY BLADE FIRE] target={targetPlayerId}, hitPoint={hitPoint}"
        );
    }

    private void HandleEntryBladeState(byte[] body)
    {
        const int requiredSize =
            4 + 1 + 1 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE STATE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool active =
            body[offset] != 0;
        offset += 1;

        byte bladeCount =
            body[offset];
        offset += 1;

        float duration =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE STATE] player not found. playerId={playerId}"
            );

            return;
        }

        EntrySkill_X skill =
            identity.GetComponent<EntrySkill_X>();

        if (skill == null)
        {
            skill =
                identity.GetComponentInChildren<EntrySkill_X>(true);
        }

        if (skill == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE STATE] EntrySkill_X missing. playerId={playerId}"
            );

            return;
        }

        skill.ApplyBladeStateFromServer(
            active,
            bladeCount,
            duration
        );

        Debug.Log(
            $"[ENTRY BLADE STATE APPLY] playerId={playerId}, active={active}, blades={bladeCount}, duration={duration}"
        );
    }

    private void HandleEntryBladeFire(byte[] body)
    {
        const int requiredSize =
            4 + 4 + 1 + 1 + 4 + 4 + 4 + 4 + 4 + 4;

        if (body == null ||
            body.Length < requiredSize)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE FIRE] invalid body length={body?.Length ?? 0}"
            );

            return;
        }

        int offset = 0;

        uint shooterPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        uint targetPlayerId =
            BitConverter.ToUInt32(body, offset);
        offset += 4;

        bool hit =
            body[offset] != 0;
        offset += 1;

        byte remainingBlades =
            body[offset];
        offset += 1;

        float startX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float startY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float startZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float endX =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float endY =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        float endZ =
            BitConverter.ToSingle(body, offset);
        offset += 4;

        PlayerIdentity shooter =
            FindPlayerIdentityById(shooterPlayerId);

        if (shooter == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE FIRE] shooter not found. shooter={shooterPlayerId}"
            );

            return;
        }

        EntrySkill_X skill =
            shooter.GetComponent<EntrySkill_X>();

        if (skill == null)
        {
            skill =
                shooter.GetComponentInChildren<EntrySkill_X>(true);
        }

        if (skill == null)
        {
            Debug.LogWarning(
                $"[ENTRY BLADE FIRE] EntrySkill_X missing. shooter={shooterPlayerId}"
            );

            return;
        }

        skill.PlayBladeFireEffect(
            new Vector3(startX, startY, startZ),
            new Vector3(endX, endY, endZ),
            hit,
            remainingBlades
        );

        Debug.Log(
            $"[ENTRY BLADE FIRE APPLY] shooter={shooterPlayerId}, target={targetPlayerId}, hit={hit}, remaining={remainingBlades}"
        );
    }

}

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
    public void SendTempGameRoomEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEMP_GAME_ROOM_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendTempGameStart(int mode)
    {
        byte[] buffer = new byte[5];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_TEMP_GAME_START).CopyTo(buffer, 2);
        buffer[4] = (byte)Mathf.Clamp(mode, 0, 1);

        client.Send(buffer);
    }

    public void SendMatchmakingCancel()
    {
        SendHeaderOnly(PacketType.PKT_CS_MATCHMAKING_CANCEL);
    }

    void HandleTempGameRoomEnter(byte[] body)
    {
        const int BaseSlotSize = 33;
        const int PresetSize = 57;
        if (body == null || body.Length < 2 + 3 * BaseSlotSize) return;
        bool hasPresets = body.Length >= 2 + 3 * (BaseSlotSize + PresetSize);
        byte result = body[0];
        byte playerCount = body[1];
        if (playerCount > 3) return;

        if (result != 0)
        {
            Debug.LogWarning("[TEMP GAME ROOM] enter fail");
            return;
        }

        List<TempGamePlayerData> players = new List<TempGamePlayerData>();

        int offset = 2;
        // Unused fixed-capacity slots are zero-filled (including slotIndex = 0).
        // They must never overwrite the occupied leader card with an empty player.
        for (int i = 0; i < playerCount; i++)
        {
            TempGamePlayerData data = new TempGamePlayerData();
            data.slotIndex = body[offset];
            offset += 1;

            data.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            data.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.logoBorderId = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (hasPresets)
                data.preset = ReadChipCraftPreset(body, ref offset);

            VectorVoiceChat.Instance?.ObserveProfile(data.nickname, data.playerIconId);
            players.Add(data);
        }

        SetReadyRoomChatContext();
        gamePanelController?.OnRecvTempGameRoomEnter(playerCount, players);
    }

    void HandleTempGameStartResult(byte[] body)
    {
        if (body == null || body.Length < 10)
            return;

        byte result = body[0];
        byte state = body[1];
        byte mode = body[2];
        ushort elapsedSeconds = BitConverter.ToUInt16(body, 3);
        byte tierRange = body[5];
        uint roomId = BitConverter.ToUInt32(body, 6);
        gamePanelController?.OnRecvTempGameStartResult(
            result,
            state,
            mode,
            elapsedSeconds,
            tierRange,
            roomId);
    }

    public void SendRoomList()
    {
        Debug.Log("[CLIENT] SendRoomList");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_LIST).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleRoomList(byte[] body)
    {
        Debug.Log($"[ROOMLIST] bodyLength={body.Length}");

        int offset = 0;

        ushort roomCount = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[ROOMLIST] roomCount={roomCount}");

        List<RoomListData> rooms = new();

        for (int i = 0; i < roomCount; i++)
        {
            Debug.Log($"[ROOM PARSE START] index={i} offset={offset}");

            RoomListData room = new RoomListData();

            room.roomId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            // 문자열 raw 바이트 확인
            Debug.Log($"[RAW roomName bytes] {BitConverter.ToString(body, offset, 32)}");
            room.roomName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;

            Debug.Log($"[RAW mapName bytes] {BitConverter.ToString(body, offset, 32)}");
            room.mapName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;

            Debug.Log($"[RAW hostName bytes] {BitConverter.ToString(body, offset, 20)}");
            room.hostName = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            room.currentPlayerCount = BitConverter.ToUInt16(body, offset);
            offset += 2;

            room.maxPlayerCount = BitConverter.ToUInt16(body, offset);
            offset += 2;

            room.state = body[offset];
            offset += 1;

            Debug.Log($"[ROOM PARSED] id={room.roomId}, name={room.roomName}, map={room.mapName}, host={room.hostName}");

            rooms.Add(room);
        }

        gamePanelController?.OnRecvRoomList(rooms);
    }

    public void SendRoomCreate()
    {
        Debug.Log("[CLIENT] SendRoomCreate");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_CREATE).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendRoomEnter(uint roomId)
    {
        Debug.Log($"[CLIENT] SendRoomEnter roomId={roomId}");

        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_ENTER).CopyTo(buffer, 2);
        BitConverter.GetBytes(roomId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    void HandleRoomCreateResult(byte[] body)
    {
        Debug.Log($"[ROOM CREATE RESULT] bodyLength={body.Length}");

        int offset = 0;

        byte result = body[offset];
        offset += 1;

        if (result != 0)
        {
            Debug.LogWarning($"[ROOM CREATE FAIL] result={result}");
            return;
        }

        RoomStateData roomState = new RoomStateData();

        roomState.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        roomState.roomName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.mapName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.isLeader = body[offset] != 0;
        offset += 1;

        roomState.canStart = body[offset] != 0;
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            RoomMemberData member = new RoomMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.level = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.team = (RoomSlotTeam)body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.isLeader = body[offset] != 0;
            offset += 1;

            roomState.members.Add(member);
        }

        SetPrivateRoomChatContext(roomState.roomId, roomState.roomName);
        gamePanelController?.OnRecvRoomCreateResult(roomState);
    }

    public void SendRoomLeave()
    {
        Debug.Log("[CLIENT] SendRoomLeave");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_LEAVE).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleRoomLeaveResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        byte roomDeleted = body[offset];
        offset += 1;

        Debug.Log($"[ROOM LEAVE RESULT] result={result}, roomId={roomId}, roomDeleted={roomDeleted}");

        if (result == 0)
            ClearPrivateRoomChatContext(roomId);

        gamePanelController?.OnRecvRoomLeaveResult(result, roomId, roomDeleted != 0);
    }

    void HandleRoomEnterResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        Debug.Log($"[ROOM ENTER RESULT] result={result}, roomId={roomId}");

        gamePanelController?.OnRecvRoomEnterResult(result, roomId);
    }

    void HandleRoomState(byte[] body)
    {
        Debug.Log($"[ROOM STATE] bodyLength={body.Length}");

        int offset = 0;

        RoomStateData roomState = new RoomStateData();

        roomState.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        roomState.roomName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.mapName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        roomState.canStart = body[offset] != 0;
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        Debug.Log(
            $"[ROOM STATE PARSED] " +
            $"roomId={roomState.roomId}, " +
            $"roomName={roomState.roomName}, " +
            $"mapName={roomState.mapName}, " +
            $"canStart={roomState.canStart}, " +
            $"memberCount={memberCount}"
        );

        for (int i = 0; i < memberCount; i++)
        {
            RoomMemberData member = new RoomMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.level = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.team = (RoomSlotTeam)body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.isLeader = body[offset] != 0;
            offset += 1;

            roomState.members.Add(member);

            Debug.Log(
                $"[ROOM STATE MEMBER] " +
                $"playerId={member.playerId}, " +
                $"nickname={member.nickname}, " +
                $"level={member.level}, " +
                $"team={member.team}, " +
                $"slot={member.slotIndex}, " +
                $"leader={member.isLeader}"
            );
        }

        SetPrivateRoomChatContext(roomState.roomId, roomState.roomName);
        gamePanelController?.OnRecvRoomState(roomState);
    }

    public void SendRoomSlotChange(RoomSlotTeam targetTeam, byte targetSlotIndex)
    {
        Debug.Log($"[CLIENT] SendRoomSlotChange team={targetTeam}, slot={targetSlotIndex}");

        byte[] buffer = new byte[4 + 1 + 1];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_SLOT_CHANGE).CopyTo(buffer, 2);

        buffer[4] = (byte)targetTeam;
        buffer[5] = targetSlotIndex;

        client.Send(buffer);
    }

    public void SendRoomSlotSwapResponse(uint requesterPlayerId, bool accept)
    {
        Debug.Log($"[CLIENT] SendRoomSlotSwapResponse requester={requesterPlayerId}, accept={accept}");

        byte[] buffer = new byte[4 + 4 + 1];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_SLOT_SWAP_RESPONSE).CopyTo(buffer, 2);

        BitConverter.GetBytes(requesterPlayerId).CopyTo(buffer, 4);
        buffer[8] = accept ? (byte)1 : (byte)0;

        client.Send(buffer);
    }

    void HandleRoomSlotChangeResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        RoomSlotTeam targetTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        byte targetSlotIndex = body[offset];
        offset += 1;

        string targetNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[ROOM SLOT CHANGE RESULT] " +
            $"result={result}, targetTeam={targetTeam}, targetSlot={targetSlotIndex}, targetNickname={targetNickname}"
        );

        gamePanelController?.OnRecvRoomSlotChangeResult(
            result,
            targetTeam,
            targetSlotIndex,
            targetNickname
        );
    }

    void HandleRoomSlotSwapRequest(byte[] body)
    {
        int offset = 0;

        RoomSlotSwapRequestData data = new RoomSlotSwapRequestData();

        data.requesterPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.requesterNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        data.requesterTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        data.requesterSlotIndex = body[offset];
        offset += 1;

        data.targetTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        data.targetSlotIndex = body[offset];
        offset += 1;

        Debug.Log(
            $"[ROOM SLOT SWAP REQUEST] " +
            $"requester={data.requesterNickname}, requesterId={data.requesterPlayerId}, " +
            $"from={data.requesterTeam}{data.requesterSlotIndex}, " +
            $"target={data.targetTeam}{data.targetSlotIndex}"
        );

        gamePanelController?.OnRecvRoomSlotSwapRequest(data);
    }

    public void SendRoomTransferLeader(uint targetPlayerId)
    {
        Debug.Log($"[CLIENT] SendRoomTransferLeader target={targetPlayerId}");

        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_TRANSFER_LEADER).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetPlayerId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendRoomKick(uint targetPlayerId)
    {
        Debug.Log($"[CLIENT] SendRoomKick target={targetPlayerId}");

        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_KICK).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetPlayerId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    void HandleRoomTransferLeaderResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        string targetNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[ROOM TRANSFER LEADER RESULT] " +
            $"result={result}, targetPlayerId={targetPlayerId}, targetNickname={targetNickname}"
        );

        gamePanelController?.OnRecvRoomTransferLeaderResult(
            result,
            targetPlayerId,
            targetNickname
        );
    }

    void HandleRoomKickResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        uint targetPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        string targetNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log(
            $"[ROOM KICK RESULT] " +
            $"result={result}, targetPlayerId={targetPlayerId}, targetNickname={targetNickname}"
        );

        gamePanelController?.OnRecvRoomKickResult(
            result,
            targetPlayerId,
            targetNickname
        );
    }

    void HandleRoomKicked(byte[] body)
    {
        int offset = 0;

        uint roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        string kickerNickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;

        Debug.Log($"[ROOM KICKED] roomId={roomId}, kicker={kickerNickname}");

        ClearPrivateRoomChatContext(roomId);

        gamePanelController?.OnRecvRoomKicked(roomId, kickerNickname);
    }

    public void SendRoomMapSet(GameMapId mapId)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_MAP_SET).CopyTo(buffer, 2);
        buffer[4] = (byte)mapId;
        client.Send(buffer);
        Debug.Log($"[CLIENT] SendRoomMapSet map={mapId}");
    }

    private void HandleRoomMapSetResult(byte[] body)
    {
        if (body == null || body.Length < 2)
        {
            Debug.LogWarning("[ROOM MAP SET] Invalid response.");
            return;
        }

        byte result = body[0];
        GameMapId mapId = (GameMapId)body[1];
        gamePanelController?.OnRecvRoomMapSetResult(result, mapId);
    }
    public void SendRoomGameStart()
    {
        Debug.Log("[CLIENT] SendRoomGameStart");

        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_GAME_START).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    void HandleRoomGameStartResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        Debug.Log($"[ROOM GAME START RESULT] result={result}");

        gamePanelController?.OnRecvRoomGameStartResult(result);
    }


}

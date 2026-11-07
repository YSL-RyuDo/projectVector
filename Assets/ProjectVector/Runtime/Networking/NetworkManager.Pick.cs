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
    public void SendPickEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendPickHover(int characterId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_HOVER).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendPickLock(int characterId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PICK_LOCK).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    private void HandlePickState(byte[] body)
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.EnterPickPhase();
        }

        int offset = 0;

        PickStateData state = new PickStateData();

        state.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        state.mapName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;

        state.myTeam = (RoomSlotTeam)body[offset];
        offset += 1;

        state.firstRole = (PickRole)body[offset];
        offset += 1;

        state.secondRole = (PickRole)body[offset];
        offset += 1;

        state.remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        state.blueScore = body[offset];
        offset += 1;

        state.redScore = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            PickTeamMemberData member = new PickTeamMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.slotIndex = body[offset];
            offset += 1;

            member.hoverCharacterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.lockedCharacterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.isLocked = body[offset] != 0;
            offset += 1;

            member.isMe = body[offset] != 0;
            offset += 1;

            if (i < memberCount)
            {
                state.members.Add(member);

                if (member.isMe)
                {
                    myPlayerId = member.playerId;
                }
            }
        }

        pickManager?.OnRecvPickState(state);
    }

    private void HandlePickLockResult(byte[] body)
    {
        int offset = 0;

        byte result = body[offset];
        offset += 1;

        int characterId = BitConverter.ToInt32(body, offset);
        offset += 4;

        pickManager?.OnRecvPickLockResult(result, characterId);
    }

    private void HandlePickTimer(byte[] body)
    {
        int offset = 0;

        PickPhase phase = (PickPhase)body[offset];
        offset += 1;

        ushort remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[PICK TIMER] phase={phase}, remain={remainingSeconds}");

        pickManager?.OnRecvPickTimer(phase, remainingSeconds);
    }

    private void HandleMatchLoadingInfo(byte[] body)
    {
        resultMapName = null; // New match must never reuse the previous result's map.
        if (body != null && body.Length >= 4) BeginResultAppearance(BitConverter.ToUInt32(body, 0));
        if (GameController.Instance != null)
        {
            GameController.Instance.EnterLoadingPhase();
        }

        int offset = 0;

        MatchLoadingInfoData data = new MatchLoadingInfoData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.attackTeam = body[offset];
        offset += 1;

        data.defenseTeam = body[offset];
        offset += 1;

        byte attackCount = body[offset];
        offset += 1;

        byte defenseCount = body[offset];
        offset += 1;

        for (int i = 0; i < 3; i++)
        {
            MatchLoadingMemberData member = new MatchLoadingMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (i < attackCount)
                data.attackMembers.Add(member);
        }

        for (int i = 0; i < 3; i++)
        {
            MatchLoadingMemberData member = new MatchLoadingMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (i < defenseCount)
                data.defenseMembers.Add(member);
        }

        pickManager?.OnRecvMatchLoadingInfo(data);
    }

    private void HandleMatchStart(byte[] body)
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.EnterLoadingPhase();
        }

        uint roomId =
            BitConverter.ToUInt32(body, 0);

        Debug.Log($"[MATCH START] roomId={roomId}");

        pickManager?.OnRecvMatchStart(roomId);
    }

}

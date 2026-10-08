using System;
using System.Collections.Generic;

public partial class NetworkManager
{
    public uint PartyLeader { get; private set; }
    public List<uint> PartyPlayers { get; } = new();
    public bool IsPartyLeader => PartyPlayers.Count < 2 || PartyLeader == MyPlayerId;
    public int PartyLobbyCount { get; private set; }
    public bool PartyAvailabilityKnown { get; private set; }
    public bool PartyReadyToQueue => PartyPlayers.Count < 2 || (PartyAvailabilityKnown && PartyLobbyCount == PartyPlayers.Count);
    public bool ReturningToParty { get; set; }
    public byte LastMatchmakingMode { get; set; }
    public byte PartyMode { get; private set; }
    public void SendPartyMode(byte mode)
    {
        if (!IsConnected || !IsAuthenticated || mode > 1) return;
        var buffer = new byte[5];
        BitConverter.GetBytes((ushort)5).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PARTY_MODE).CopyTo(buffer, 2);
        buffer[4] = mode;
        client.Send(buffer);
    }
    private void ResetSocialState()
    {
        myPlayerId = 0;
        PartyMode = 0;
        PartyLeader = 0; PartyPlayers.Clear(); PartyLobbyCount = 0;
        PartyAvailabilityKnown = false; ReturningToParty = false; LastMatchmakingMode = 0;
        BeginResultAppearance(0);
        ReadyCardAppearances.Clear();
    }

    public void SendSocial(PacketType type, uint value = 0, bool accept = false)
    {
        if (!IsConnected || !IsAuthenticated) return;
        int size;
        switch (type)
        {
            case PacketType.PKT_CS_PARTY_STATE:
            case PacketType.PKT_CS_PARTY_LEAVE: size = 4; break;
            case PacketType.PKT_CS_PARTY_INVITE: size = 8; break;
            case PacketType.PKT_CS_PARTY_INVITE_REPLY:
            case PacketType.PKT_CS_MATCH_READY: size = 9; break;
            default: return;
        }
        var buffer = new byte[size];
        BitConverter.GetBytes((ushort)size).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)type).CopyTo(buffer, 2);
        if (size >= 8) BitConverter.GetBytes(value).CopyTo(buffer, 4);
        if (size == 9) buffer[8] = accept ? (byte)1 : (byte)0;
        client.Send(buffer);
    }

    private void HandleSocialPacket(PacketType type, byte[] body)
    {
        if (body == null) return;
        var ui = LobbySocialController.Instance;
        if (type == PacketType.PKT_SC_PARTY_INVITE && body.Length == 30)
            ui?.ReceiveInvite(BitConverter.ToUInt32(body, 0), Packet.ReadFixedString(body, 8, 20), BitConverter.ToUInt16(body, 28));
        else if (type == PacketType.PKT_SC_SOCIAL_RESULT && body.Length == 5)
        {
            if (body[0] >= 3) FindFirstObjectByType<GamePanelController>(UnityEngine.FindObjectsInactive.Include)?.CancelPendingPartyNavigation();
            ui?.ReceiveResult(body[0]);
        }
        else if (type == PacketType.PKT_SC_PARTY_MODE && body.Length == 5 && body[4] <= 1 && BitConverter.ToUInt32(body, 0) == PartyLeader)
        {
            PartyMode = body[4];
            LastMatchmakingMode = PartyMode;
            FindFirstObjectByType<GamePanelController>(UnityEngine.FindObjectsInactive.Include)?.OnPartyModeReceived(PartyMode);
        }
        else if (type == PacketType.PKT_SC_PARTY_STATE && body.Length == 17 && body[4] <= 3)
        {
            PartyAvailabilityKnown = false;
            PartyLeader = BitConverter.ToUInt32(body, 0);
            PartyPlayers.Clear();
            for (int i = 0; i < body[4]; ++i) PartyPlayers.Add(BitConverter.ToUInt32(body, 5 + i * 4));
            // A solo party is an authoritative self-ID response from older servers too.
            if (PartyPlayers.Count == 1 && PartyPlayers[0] == PartyLeader)
                myPlayerId = PartyLeader;
            ui?.RefreshParty();
        }
        else if (type == PacketType.PKT_SC_PARTY_AVAILABILITY && body.Length == 2 && body[0] == PartyPlayers.Count)
        {
            PartyLobbyCount = 0;
            for (int i = 0; i < body[0]; ++i) if ((body[1] & (1 << i)) != 0) PartyLobbyCount++;
            PartyAvailabilityKnown = true;
            ui?.RefreshParty();
        }
        else if (type == PacketType.PKT_SC_MATCH_READY && body.Length == 10)
            ui?.ReceiveReady(BitConverter.ToUInt32(body, 0), body[4], BitConverter.ToUInt16(body, 5), body[7], body[8], body[9] != 0);
    }
}

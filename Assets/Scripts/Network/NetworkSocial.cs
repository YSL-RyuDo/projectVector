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
    private void ResetSocialState()
    {
        PartyLeader = 0; PartyPlayers.Clear(); PartyLobbyCount = 0;
        PartyAvailabilityKnown = false; ReturningToParty = false; LastMatchmakingMode = 0;
        BeginResultAppearance(0);
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
            ui?.ReceiveResult(body[0]);
        else if (type == PacketType.PKT_SC_PARTY_STATE && body.Length == 17 && body[4] <= 3)
        {
            PartyAvailabilityKnown = false;
            PartyLeader = BitConverter.ToUInt32(body, 0);
            PartyPlayers.Clear();
            for (int i = 0; i < body[4]; ++i) PartyPlayers.Add(BitConverter.ToUInt32(body, 5 + i * 4));
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

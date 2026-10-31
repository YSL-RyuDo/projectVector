using System;
using System.Collections.Generic;

public partial class NetworkManager
{
    public Dictionary<uint, MatchAppearanceData> ReadyCardAppearances { get; } = new();
    private void HandleReadyCards(byte[] body)
    {
        var ui = LobbySocialController.Instance;
        if (ui == null || body == null || body.Length != 588 || body[4] != 6 || body[5] > 63) return;
        uint token = BitConverter.ToUInt32(body, 0);
        if (token == 0 || ui.ReadyToken != token || !ui.readyPanel.activeSelf) return;
        // Reuse the strict cosmetics decoder, without touching match-result state.
        var appearanceBody = new byte[467];
        Array.Copy(body, 0, appearanceBody, 0, 5);
        var names = new string[6]; var ids = new uint[6];
        for (int i = 0; i < 6; i++)
        {
            names[i] = Packet.ReadFixedString(body, 6 + i * 97, 20);
            ids[i] = BitConverter.ToUInt32(body, 26 + i * 97);
            Array.Copy(body, 26 + i * 97, appearanceBody, 5 + i * 77, 77);
        }
        if (!TryReadResultAppearance(appearanceBody, token, out var parsed)) return;
        ReadyCardAppearances.Clear();
        foreach (var pair in parsed) ReadyCardAppearances.Add(pair.Key, pair.Value);
        for (int i = 0; i < ids.Length; i++)
            if ((body[5] & (1 << i)) != 0 && parsed.TryGetValue(ids[i], out var identity))
                VectorVoiceChat.Instance?.ObserveProfile(names[i], identity.playerIconId);
        ui.readyPresentation?.Present(token, names, ids, parsed, body[5], MyPlayerId);
    }
}

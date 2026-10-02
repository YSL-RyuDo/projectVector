using System;
using System.Collections.Generic;

public partial class NetworkManager
{
    private uint appearanceRoomId;
    private Dictionary<uint, MatchAppearanceData> pendingResultAppearance = new();
    private void BeginResultAppearance(uint roomId)
    {
        appearanceRoomId = roomId;
        pendingResultAppearance.Clear();
    }
    private void HandleMatchAppearance(byte[] body)
    {
        if (TryReadResultAppearance(body,appearanceRoomId,out var parsed)) pendingResultAppearance=parsed;
    }
    public static bool TryReadResultAppearance(byte[] body,uint expectedRoomId,out Dictionary<uint,MatchAppearanceData> parsed)
    {
        parsed = new Dictionary<uint,MatchAppearanceData>();
        // Old servers need not send this packet; missing cosmetics remain hidden.
        if (body == null || body.Length != 467 || body[4] > 6 || expectedRoomId == 0 ||
            BitConverter.ToUInt32(body, 0) != expectedRoomId) return false;
        int offset = 5;
        for (int i = 0; i < body[4]; i++)
        {
            uint id = BitConverter.ToUInt32(body, offset); offset += 4;
            var value = new MatchAppearanceData();
            value.playerIconId = BitConverter.ToInt32(body, offset); offset += 4;
            value.logoBorderId = BitConverter.ToInt32(body, offset); offset += 4;
            value.rankImageId = BitConverter.ToInt32(body, offset); offset += 4;
            value.level = BitConverter.ToInt32(body, offset); offset += 4;
            if(body[offset+2]>3 || body[offset+2]>body[offset+1]+1) return false;
            value.preset = ReadChipCraftPreset(body, ref offset);
            if (id == 0 || parsed.ContainsKey(id) || value.playerIconId < 0 || value.playerIconId > 2 ||
                value.preset.cardId != value.playerIconId || value.preset.caseId > 2) return false;
            foreach (var medal in value.preset.medals)
                if (medal.medalId > 5 || float.IsNaN(medal.x) || float.IsInfinity(medal.x) ||
                    float.IsNaN(medal.y) || float.IsInfinity(medal.y) || float.IsNaN(medal.rotation) ||
                    float.IsInfinity(medal.rotation) || float.IsNaN(medal.scale) || float.IsInfinity(medal.scale)) return false;
            parsed.Add(id, value);
        }
        return true;
    }
    private Dictionary<uint, MatchAppearanceData> ConsumeResultAppearance()
    {
        var result = pendingResultAppearance;
        pendingResultAppearance = new(); appearanceRoomId = 0;
        return result;
    }
}

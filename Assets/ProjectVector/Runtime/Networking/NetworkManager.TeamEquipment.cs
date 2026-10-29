using System;
using System.Collections.Generic;

public partial class NetworkManager
{
    void HandleTeamEquipment(byte[] body)
    {
        if (!ApplyTeamEquipment(body, latestScoreboardData)) return;
        InGameScoreboardUI.Instance?.OnRecvScoreboardUpdate(latestScoreboardData);
    }

    // Validate the whole snapshot before changing any visible row.
    public static bool ApplyTeamEquipment(byte[] body, ScoreboardData board)
    {
        if (body == null || body.Length < 6 || board == null ||
            BitConverter.ToUInt32(body, 0) != board.roomId || body[4] > 1 || body[4] != board.myTeam ||
            body[5] > 3 || body.Length != 6 + body[5] * 9) return false;
        var seen = new HashSet<uint>();
        for (int i = 0; i < body[5]; i++)
        {
            int at = 6 + i * 9; uint id = BitConverter.ToUInt32(body, at);
            int weapon = BitConverter.ToInt32(body, at + 4);
            var member = board.members.Find(m => m.playerId == id && m.team == board.myTeam);
            if (member == null || !seen.Add(id) || weapon < -1 || weapon > 13 || body[at + 8] > 1) return false;
        }
        foreach (var member in board.members) { member.hasEquipment = false; member.ownedWeaponId = -1; }
        for (int i = 0; i < body[5]; i++)
        {
            int at = 6 + i * 9; uint id = BitConverter.ToUInt32(body, at);
            var member = board.members.Find(m => m.playerId == id);
            member.hasEquipment = true; member.ownedWeaponId = BitConverter.ToInt32(body, at + 4);
            member.equipmentAlive = body[at + 8] != 0;
        }
        return true;
    }
}

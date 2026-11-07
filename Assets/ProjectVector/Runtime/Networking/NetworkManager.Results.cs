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
    private void HandleMatchResultDetail(byte[] body)
    {
        const int HeaderSize = 5;
        const int MemberEntrySize = 118;

        if (body == null || body.Length < HeaderSize)
        {
            Debug.LogWarning("[MATCH RESULT DETAIL] invalid packet header");
            return;
        }

        int expectedSize = HeaderSize + body[4] * MemberEntrySize;
        if (body.Length < expectedSize)
        {
            Debug.LogWarning(
                $"[MATCH RESULT DETAIL] invalid packet size: " +
                $"expected={expectedSize}, actual={body.Length}");
            return;
        }

        int offset = 0;

        var appearances = ConsumeResultAppearance();
        MatchResultCache.Clear();
        MatchResultCache.mapName = resultMapName;

        MatchResultCache.winnerTeam = body[offset];
        offset += 1;

        MatchResultCache.scoreA = body[offset];
        offset += 1;

        MatchResultCache.scoreB = body[offset];
        offset += 1;

        MatchResultCache.matchType = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            MatchResultMemberData member = new MatchResultMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.team = body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.kills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.deaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.assists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.firstHalfKills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.firstHalfDeaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.firstHalfAssists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.secondHalfKills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.secondHalfDeaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.secondHalfAssists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.overtimeKills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.overtimeDeaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.overtimeAssists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.isMvp = body[offset] != 0;
            offset += 1;

            member.freeCurrencyReward = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.victoryCurrencyReward = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankPointDelta = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankPointsBefore = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankPointsAfter = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankTierIdBefore = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankTierIdAfter = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.rankExpectedWinRatePermille =
                BitConverter.ToUInt16(body, offset);
            offset += 2;
            member.rankModifierFlags = body[offset];
            offset += 1;
            member.totalFreeCurrency = BitConverter.ToInt32(body, offset);
            offset += 4;
            member.totalVictoryCurrency = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (appearances.TryGetValue(member.playerId, out var appearance)) member.appearance = appearance;
            MatchResultCache.members.Add(member);

            if (member.playerId == MyPlayerId)
            {
                MatchResultCache.freeCurrencyReward = member.freeCurrencyReward;
                MatchResultCache.victoryCurrencyReward = member.victoryCurrencyReward;
                MatchResultCache.rankPointDelta = member.rankPointDelta;
                MatchResultCache.rankPointsBefore = member.rankPointsBefore;
                MatchResultCache.rankPointsAfter = member.rankPointsAfter;
                MatchResultCache.rankTierIdBefore = member.rankTierIdBefore;
                MatchResultCache.rankTierIdAfter = member.rankTierIdAfter;
                MatchResultCache.rankExpectedWinRatePermille =
                    member.rankExpectedWinRatePermille;
                MatchResultCache.rankModifierFlags =
                    member.rankModifierFlags;
                MatchResultCache.totalFreeCurrency = member.totalFreeCurrency;
                MatchResultCache.totalVictoryCurrency = member.totalVictoryCurrency;
                MoneyManager.Instance?.SetServerBalances(
                    member.totalFreeCurrency,
                    member.totalVictoryCurrency
                );
            }
        }

        Debug.Log(
            $"[MATCH RESULT DETAIL] winner={MatchResultCache.winnerTeam}, " +
            $"score={MatchResultCache.scoreA}:{MatchResultCache.scoreB}, " +
            $"members={MatchResultCache.members.Count}"
        );

        // The authored Result scene now owns character -> rank -> rewards -> table.
        // Settlement/cache/wallet updates above remain authoritative and unchanged.
        SceneNavigation.Load(ProjectVector.Core.ProjectScene.Result);
    }

    public void SendResultLeave()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_RESULT_LEAVE;

        byte[] buffer = new byte[size];

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log("[SEND RESULT LEAVE]");
    }

    private void HandleResultLeaveDone(byte[] body)
    {
        uint roomId = BitConverter.ToUInt32(body, 0);

        Debug.Log($"[RESULT LEAVE DONE] roomId={roomId}");

        pendingReturnRoomId = roomId;
        ReturningToParty = roomId == 0;

        SceneNavigation.Load(ProjectVector.Core.ProjectScene.Lobby);
    }

}

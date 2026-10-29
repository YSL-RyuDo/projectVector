using System;

public partial class NetworkManager
{
    void HandleRoundRules(byte[] body)
    {
        if(!ApplyRoundRules(body,latestScoreboardData))return;
        InGameScoreboardUI.Instance?.OnRecvScoreboardUpdate(latestScoreboardData);
        TopInfoUI.Instance?.ApplyRoundRules(latestScoreboardData.firstHalfRounds,latestScoreboardData.secondHalfRounds);
    }

    public static bool ApplyRoundRules(byte[] body,ScoreboardData board)
    {
        if(body==null || body.Length!=8 || board==null || BitConverter.ToUInt32(body,0)!=board.roomId)return false;
        int first=BitConverter.ToUInt16(body,4), second=BitConverter.ToUInt16(body,6);
        // Existing round event packets use one byte for the round number.
        if(first<1 || second<1 || first+second>=255)return false;
        board.firstHalfRounds=first;board.secondHalfRounds=second;
        return true;
    }
}

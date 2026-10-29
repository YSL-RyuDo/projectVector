using System;
using UnityEngine;

public partial class NetworkManager
{
    readonly CombatReportAssembly combatReport = new();
    uint combatReportRequestId;
    bool reportForLocalDeath;
    public ScoreboardData CurrentScoreboard => latestScoreboardData;

    public void ResetLocalCombatReport()
    {
        reportForLocalDeath=false; unchecked { combatReportRequestId++; }
        combatReport.Reset(); ReportUI.Instance?.CloseAndReset();
    }

    public void BeginLocalCombatReport()
    {
        uint room=latestScoreboardData?.roomId ?? 0;
        if(room==0 || !IsConnected) return;
        reportForLocalDeath=true;
        ReportUI.Instance?.Open();
        SendCombatReportRequest(room);
    }

    public void RetryLocalCombatReport()
    {
        if(reportForLocalDeath && SpectatorManager.Instance && SpectatorManager.Instance.IsSpectating)
            BeginLocalCombatReport();
    }

    public void AttachCombatReportView()
    {
        if(!reportForLocalDeath || !ReportUI.Instance) return;
        ReportUI.Instance.Open();
        if(combatReport.Complete) ReportUI.Instance.ApplyData(combatReport.Entries,combatReport.Killer,combatReport.Limited);
        else if(!combatReport.Waiting) ReportUI.Instance.ShowFailed();
    }

    public void SendCombatReportRequest(uint roomId,uint lifeId=0,ushort pageIndex=0)
    {
        if(!reportForLocalDeath || roomId==0 || latestScoreboardData?.roomId!=roomId || !IsConnected) return;
        if(pageIndex==0) combatReport.Begin(roomId);
        else if(!combatReport.Waiting || combatReport.Room!=roomId || combatReport.Life!=lifeId || combatReport.NextPage!=pageIndex) return;
        unchecked { combatReportRequestId++; }
        combatReport.Expect(combatReportRequestId);
        byte[] buffer=new byte[18];
        BitConverter.GetBytes((ushort)18).CopyTo(buffer,0);
        BitConverter.GetBytes((ushort)370).CopyTo(buffer,2);
        BitConverter.GetBytes(combatReportRequestId).CopyTo(buffer,4);
        BitConverter.GetBytes(roomId).CopyTo(buffer,8);
        BitConverter.GetBytes(lifeId).CopyTo(buffer,12);
        BitConverter.GetBytes(pageIndex).CopyTo(buffer,16);
        client.Send(buffer);
    }

    private void HandleCombatReport(byte[] body)
    {
        if(!reportForLocalDeath || latestScoreboardData?.roomId!=combatReport.Room) return;
        var result=combatReport.Accept(body);
        if(result==CombatReportAssembly.Result.Ignored) return;
        if(result==CombatReportAssembly.Result.Failed) { ReportUI.Instance?.ShowFailed(); return; }
        ReportUI.Instance?.NotifyPageReceived();
        if(result==CombatReportAssembly.Result.NextPage)
            SendCombatReportRequest(combatReport.Room,combatReport.Life,combatReport.NextPage);
        else ReportUI.Instance?.ApplyData(combatReport.Entries,combatReport.Killer,combatReport.Limited);
    }
}

using System;
using System.Collections.Generic;

// A death snapshot belongs to one room and life, never to the spectated player.
public sealed class CombatReportAssembly
{
    public enum Result { Ignored, Failed, NextPage, Complete }
    public readonly List<CombatReportRawEntry> Entries = new();
    public uint Room { get; private set; }
    public uint Life { get; private set; }
    public uint Killer { get; private set; }
    public ushort NextPage { get; private set; }
    public bool Waiting { get; private set; }
    public bool Complete { get; private set; }
    public bool Limited { get; private set; }
    uint request;
    ushort pages, total;
    byte flags;

    public void Reset()
    {
        Entries.Clear(); Room=Life=Killer=request=0; NextPage=pages=total=0;
        Waiting=Complete=Limited=false; flags=0;
    }
    public void Begin(uint room) { Reset(); Room=room; Waiting=room!=0; }
    public void Expect(uint id) { request=id; }
    public void Fail() { Waiting=false; Complete=false; Entries.Clear(); }

    public Result Accept(byte[] body)
    {
        if(!Waiting || body==null || body.Length!=508 || BitConverter.ToUInt32(body,0)!=request || body[4]!=1)
            return Result.Ignored;
        if(body[5]>3) return Result.Ignored;
        if(body[5]!=0) { Fail(); return Result.Failed; }
        uint room=BitConverter.ToUInt32(body,8), life=BitConverter.ToUInt32(body,12), killer=BitConverter.ToUInt32(body,16);
        ushort page=BitConverter.ToUInt16(body,20), pageCount=BitConverter.ToUInt16(body,22), count=BitConverter.ToUInt16(body,24);
        int rowCount=body[7];
        if(room!=Room || life==0 || page!=NextPage || pageCount<1 || pageCount>16 || count>128 ||
           pageCount!=Math.Max(1,(count+7)/8) || page>=pageCount || rowCount!=Math.Min(8,count-page*8) ||
           (body[6]&1)==0 || (body[6]&~3)!=0 || BitConverter.ToUInt16(body,26)!=0)
            return Result.Ignored;
        if(page>0 && (life!=Life || killer!=Killer || count!=total || pageCount!=pages || body[6]!=flags))
            return Result.Ignored;
        var incoming=new List<CombatReportRawEntry>();
        for(int i=0;i<rowCount;i++)
        {
            int at=28+i*60;
            var e=new CombatReportRawEntry {
                opponentId=BitConverter.ToUInt32(body,at),characterId=BitConverter.ToInt32(body,at+4),
                nickname=Packet.ReadFixedString(body,at+8,20),sourceKind=body[at+28],direction=body[at+29],
                sourceId=BitConverter.ToInt32(body,at+32),hpDamage=BitConverter.ToUInt32(body,at+36),
                barrierDamage=BitConverter.ToUInt32(body,at+40),bodyHits=BitConverter.ToUInt32(body,at+44),
                headHits=BitConverter.ToUInt32(body,at+48),legHits=BitConverter.ToUInt32(body,at+52),
                unlocalizedHits=BitConverter.ToUInt32(body,at+56)
            };
            if(e.opponentId==0 || e.sourceKind>1 || e.direction>1 || BitConverter.ToUInt16(body,at+30)!=0 ||
               (e.hpDamage==0 && e.barrierDamage==0) ||
               (e.sourceKind==1 && (e.bodyHits!=0 || e.headHits!=0 || e.legHits!=0))) return Result.Ignored;
            incoming.Add(e);
        }
        Life=life; Killer=killer; pages=pageCount; total=count; flags=body[6]; Limited=(flags&2)!=0;
        Entries.AddRange(incoming); NextPage++;
        if(NextPage<pages) return Result.NextPage;
        Waiting=false; Complete=true;
        return Result.Complete;
    }
}

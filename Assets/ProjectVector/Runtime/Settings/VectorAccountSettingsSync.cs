using System;
using System.Text;
using UnityEngine;

// No UI or transport ownership. One instance belongs to the authenticated network session.
public sealed class VectorAccountSettingsSync
{
    [Serializable] sealed class Cache {public string revision="0",synced="",local="";}
    readonly Func<byte,ulong,ulong,string,bool> send;
    Cache cache;
    string account="",cacheKey="",observed="",sent="";
    ulong sequence=(ulong)DateTime.UtcNow.Ticks,pending,revision;
    byte operation;
    float due,timeout,poll;
    bool connected,ready,dirty,force;
    public bool CanEdit=>account.Length==0||ready;
    public string Status {get;private set;}="";
    public string Account=>account;
    const string LegacyOwner="Vector.Settings.LegacyOwner",LegacySnapshot="Vector.Settings.LegacySnapshot";
    public VectorAccountSettingsSync(Func<byte,ulong,ulong,string,bool> sender)
    {
        send=sender;
        if(!PlayerPrefs.HasKey(LegacyOwner)&&!PlayerPrefs.HasKey(LegacySnapshot)){
            string existing=VectorSettingsProfile.Capture();
            PlayerPrefs.SetString(LegacySnapshot,VectorSettingsProfile.TryParse(existing,out _)?existing:VectorSettingsProfile.Defaults());PlayerPrefs.Save();
        }
    }
    public void Begin(string user,bool resume=false)
    {
        if(resume&&user==account){Store();connected=false;pending=0;return;}
        Store();account=user??"";pending=0;connected=false;ready=false;dirty=false;force=false;due=poll=0;
        cacheKey="Vector.Settings.Account."+Convert.ToBase64String(Encoding.UTF8.GetBytes(account));
        try{cache=JsonUtility.FromJson<Cache>(PlayerPrefs.GetString(cacheKey,""));}catch(ArgumentException){cache=null;}
        cache??=new Cache();if(!ulong.TryParse(cache.revision,out revision))revision=0;
        observed=VectorSettingsProfile.TryParse(cache.local,out _)?cache.local:VectorSettingsProfile.Defaults();
        VectorSettingsProfile.Apply(observed);
        Status=account.Length==0?"":"계정 설정을 불러오는 중입니다…";
    }
    public void Detach(){Store();account="";pending=0;connected=ready=dirty=false;cache=null;Status="";VectorSettingsProfile.Apply(VectorSettingsProfile.Defaults());}
    public void Flush(){if(ready){Observe(0);force=dirty;Store();}}
    public void Store()
    {
        if(account.Length==0||cache==null)return;
        if(ready)cache.local=VectorSettingsProfile.Capture();
        cache.revision=revision.ToString();PlayerPrefs.SetString(cacheKey,JsonUtility.ToJson(cache));PlayerPrefs.Save();
    }
    void Observe(float now)
    {
        string current=VectorSettingsProfile.Capture();
        if(current!=observed){observed=current;dirty=current!=cache.synced;due=now+.8f;Status=dirty?"변경한 설정을 계정에 저장할 예정입니다.":"계정 설정이 저장되어 있습니다.";}
    }
    public void Tick(bool online,float now)
    {
        if(account.Length==0)return;
        if(ready&&now>=poll){poll=now+.25f;Observe(now);}
        if(!online){if(connected){Store();pending=0;}connected=false;if(dirty)Status="연결이 복구되면 설정을 계정에 저장합니다.";return;}
        if(!connected){Store();connected=true;ready=false;pending=0;due=now;Status="계정 설정을 불러오는 중입니다…";}
        if(pending!=0){if(now<timeout)return;pending=0;due=now+2;Status="설정 서버 응답이 지연되어 다시 시도합니다.";}
        if(now<due&&!force)return;
        if(!ready)Request(0,"",now);
        else if(dirty){Store();Request(1,observed,now);}
    }
    void Request(byte op,string payload,float now)
    {
        ulong id=++sequence;
        if(!send(op,id,op==0?0:revision,payload)){due=now+2;return;}
        operation=op;pending=id;sent=payload;timeout=now+8;force=false;
        Status=op==0?"계정 설정을 불러오는 중입니다…":"계정에 설정을 저장하는 중입니다…";
    }
    public void Receive(byte op,ulong id,byte status,ulong serverRevision,string payload,float now)
    {
        if(id!=pending||op!=operation||account.Length==0||!connected)return;
        pending=0;
        if(status==4){due=now+5;Status="설정을 저장하거나 불러오지 못했습니다. 다시 시도합니다.";return;}
        if(status==2){due=now+30;Status="설정 형식을 확인하지 못했습니다. 클라이언트와 서버 버전을 확인해 주세요.";return;}
        if(op==1&&status==3&&serverRevision==0&&payload.Length==0){Store();ready=false;due=now;Status="계정 설정을 다시 확인하고 있습니다…";return;}
        if(op==0&&status==1&&serverRevision==0&&payload.Length==0){
            string local=VectorSettingsProfile.TryParse(cache.local,out _)?cache.local:null;
            if(local==null&&!PlayerPrefs.HasKey(LegacyOwner))local=PlayerPrefs.GetString(LegacySnapshot,"");
            if(!VectorSettingsProfile.TryParse(local,out _))local=VectorSettingsProfile.Defaults();
            revision=0;cache.synced="";Apply(local);dirty=true;ClaimLegacy();due=now;Store();return;
        }
        if((status!=0&&status!=3)||serverRevision==0||!VectorSettingsProfile.TryParse(payload,out _)){
            due=now+10;Status="서버 설정을 확인하지 못했습니다. 다시 시도합니다.";return;
        }
        if(op==1&&status==0){
            // A late acknowledgement only acknowledges the snapshot actually sent.
            revision=serverRevision;cache.synced=sent;ready=true;Observe(now);dirty=observed!=cache.synced;Store();
            Status=dirty?"추가 변경 내용을 저장하는 중입니다…":"계정에 설정을 저장했습니다.";due=now;return;
        }
        bool keepPending=op==0&&status==0&&serverRevision==revision&&cache.local!=cache.synced&&VectorSettingsProfile.TryParse(cache.local,out _);
        string chosen=keepPending?cache.local:payload;
        bool conflict=status==3||(op==0&&cache.local!=cache.synced&&VectorSettingsProfile.TryParse(cache.local,out _)&&!keepPending&&cache.local!=payload);
        if(conflict){Store();PlayerPrefs.SetString(cacheKey+".ConflictBackup",cache.local);}
        revision=serverRevision;cache.synced=payload;Apply(chosen);dirty=chosen!=payload;ClaimLegacy();Store();due=now;
        Status=conflict?"다른 기기에서 저장한 최신 설정을 불러왔습니다.":dirty?"이 기기에서 변경한 설정을 저장하는 중입니다…":"계정 설정을 불러왔습니다.";
    }
    void Apply(string payload){VectorSettingsProfile.Apply(payload);observed=VectorSettingsProfile.Capture();ready=true;cache.local=observed;}
    void ClaimLegacy(){if(!PlayerPrefs.HasKey(LegacyOwner)){PlayerPrefs.SetString(LegacyOwner,account);PlayerPrefs.DeleteKey(LegacySnapshot);}}
}

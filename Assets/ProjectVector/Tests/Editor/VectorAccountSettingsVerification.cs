#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class VectorAccountSettingsVerification
{
    struct Request {public byte op;public ulong id,revision;public string payload;}
    static void Assert(bool value,string message){if(!value)throw new Exception(message);}
    static readonly List<Action> restore=new List<Action>();
    static void Float(string key){bool had=PlayerPrefs.HasKey(key);float value=PlayerPrefs.GetFloat(key);restore.Add(()=>{if(had)PlayerPrefs.SetFloat(key,value);else PlayerPrefs.DeleteKey(key);});}
    static void Int(string key){bool had=PlayerPrefs.HasKey(key);int value=PlayerPrefs.GetInt(key);restore.Add(()=>{if(had)PlayerPrefs.SetInt(key,value);else PlayerPrefs.DeleteKey(key);});}
    static void String(string key){bool had=PlayerPrefs.HasKey(key);string value=PlayerPrefs.GetString(key);restore.Add(()=>{if(had)PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);});}
    public static void Run()
    {
        restore.Clear();
        foreach(var d in VectorSettingsData.All)Float(d.key.StartsWith("MouseSensitivity_")?d.key:"Vector.Setting."+d.key);
        foreach(string action in VectorInput.Actions)String("Vector.Key."+action);
        foreach(string key in new[]{"Vector.Display.Width","Vector.Display.Height","Vector.Display.Mode","VectorVoice.Input","VectorVoice.Output","VectorVoice.Automatic","VectorVoice.Muted"})Int(key);
        foreach(string key in new[]{"VectorVoice.InputDevice","VectorVoice.OutputDevice","Vector.Settings.LegacyOwner","Vector.Settings.LegacySnapshot"})String(key);
        string accountA="settings-qa-"+Guid.NewGuid().ToString("N"),accountB=accountA+"-b";
        string CacheKey(string account)=>"Vector.Settings.Account."+Convert.ToBase64String(Encoding.UTF8.GetBytes(account));
        foreach(string account in new[]{accountA,accountB}){String(CacheKey(account));String(CacheKey(account)+".ConflictBackup");}
        try{
            PlayerPrefs.DeleteKey("Vector.Settings.LegacyOwner");PlayerPrefs.DeleteKey("Vector.Settings.LegacySnapshot");
            Assert(VectorSettingsProfile.Apply(VectorSettingsProfile.Defaults()),"Defaults validate");
            VectorSettingsData.Set("MouseSensitivity_General",1.37f);VectorSettingsData.Set("CrossGap",12);VectorInput.Bind("Reload","T",out _);
            string legacy=VectorSettingsProfile.Capture();
            var output=Path.Combine(Path.GetTempPath(),"VectorAccountSettingsVerification");Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"profile.txt"),legacy,new UTF8Encoding(false));
            Assert(!VectorSettingsProfile.Apply(legacy.Replace("CrossGap=12","CrossGap=NaN")),"Nonfinite rejected atomically");
            Assert(VectorSettingsProfile.Capture()==legacy,"Invalid payload changes nothing");
            Assert(!VectorSettingsProfile.TryParse(legacy+"CrossGap=4\n",out _),"Duplicate field rejected");
            Assert(!VectorSettingsProfile.TryParse(legacy.Replace("Key.Reload=T","Key.Reload=W"),out _),"Duplicate binding rejected");
            Assert(!VectorSettingsProfile.TryParse(legacy.Replace("Key.Reload=T","Key.Reload=9999"),out _),"Unknown key rejected");
            var requests=new List<Request>();
            var sync=new VectorAccountSettingsSync((op,id,revision,payload)=>{requests.Add(new Request{op=op,id=id,revision=revision,payload=payload});return true;});
            void Reply(byte status,ulong revision,string payload,float now){var q=requests.Last();sync.Receive(q.op,q.id,status,revision,payload,now);}
            sync.Begin(accountA);Assert(!sync.CanEdit,"Cannot overwrite cloud settings before load");sync.Tick(true,0);
            Assert(requests.Last().op==0,"Login begins with load, not upload");Reply(1,0,"",0);
            Assert(VectorSettingsProfile.Capture()==legacy,"First account imports legacy settings");sync.Tick(true,1);string first=requests.Last().payload;
            Assert(requests.Last().op==1&&requests.Last().revision==0,"Initial save uses revision zero");Reply(0,1,first,1);
            Assert(sync.CanEdit,"Loaded settings editable");
            VectorSettingsData.Set("CrossGap",13);sync.Tick(true,2);sync.Tick(true,3);var inflight=requests.Last();
            VectorSettingsData.Set("CrossGap",14);sync.Tick(true,3.5f);sync.Receive(1,inflight.id,0,2,inflight.payload,4);sync.Tick(true,5);
            Assert(requests.Last().payload.Contains("CrossGap=14\n")&&requests.Last().revision==2,"Late ack preserves edits made during save");Reply(0,3,requests.Last().payload,5);
            sync.Begin(accountB);sync.Tick(true,6);Reply(1,0,"",6);
            Assert(VectorSettingsData.Get("CrossGap")==4&&VectorInput.Binding("Reload")=="R","Second account must not inherit first account settings");sync.Tick(true,7);Reply(0,1,requests.Last().payload,7);
            // A second PC has no account cache. Server state must replace its local preferences.
            PlayerPrefs.DeleteKey(CacheKey(accountA));sync.Begin(accountA);sync.Tick(true,8);
            string remote=first.Replace("CrossGap=12","CrossGap=22");Reply(0,4,remote,8);
            Assert(VectorSettingsData.Get("CrossGap")==22&&Mathf.Approximately(MouseSettings.GetSensitivity(SensitivityType.General),1.37f)&&VectorInput.Binding("Reload")=="T","Cloud profile applies on a fresh PC");
            sync.Tick(false,9);VectorSettingsData.Set("CrossGap",23);sync.Tick(false,10);sync.Tick(true,11);Reply(0,4,remote,11);sync.Tick(true,12);
            Assert(requests.Last().op==1&&requests.Last().payload.Contains("CrossGap=23\n"),"Reconnect retries offline changes when revision is unchanged");
            VectorSettingsData.Set("CrossGap",25);
            string newer=remote.Replace("CrossGap=22","CrossGap=26");Reply(3,5,newer,12);
            Assert(VectorSettingsData.Get("CrossGap")==26&&PlayerPrefs.HasKey(CacheKey(accountA)+".ConflictBackup"),"Stale PC cannot overwrite newer server revision");
            Assert(PlayerPrefs.GetString(CacheKey(accountA)+".ConflictBackup").Contains("CrossGap=25\n"),"Conflict backup preserves even edits made during the failed save");
            VectorSettingsData.Set("CrossGap",27);sync.Tick(true,13);sync.Tick(true,14);Reply(4,0,"",14);Assert(sync.Status.Contains("다시 시도"),"Disk failure never reports saved");sync.Tick(true,20);
            Assert(requests.Last().op==1&&requests.Last().revision==5,"Failed save retries original server revision");
            var oldReply=requests.Last();sync.Begin(accountB);sync.Tick(true,21);sync.Receive(1,oldReply.id,0,6,oldReply.payload,21);
            Assert(VectorSettingsData.Get("CrossGap")==4&&!sync.CanEdit,"Late previous-account response ignored");
            Debug.Log("VECTOR_ACCOUNT_SETTINGS_VERIFIED: migration, second PC load, account isolation, validation, autosave, in-flight edits, offline retry, revision conflict, storage failure, stale reply");
        }finally{foreach(var action in restore)action();PlayerPrefs.Save();restore.Clear();}
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// Versioned, named fields. Resolution is stored as dimensions, never a machine-specific list index.
public static class VectorSettingsProfile
{
    // Leave room for both protocol headers in the server's 4096-byte receive buffer.
    public const int MaxBytes=4000;
    static readonly CultureInfo Culture=CultureInfo.InvariantCulture;
    public static bool Applying {get;private set;}
    public static string Capture()
    {
        var b=new StringBuilder("PVSET1\n");
        foreach(var d in VectorSettingsData.All.Where(d=>!d.Video))
            b.Append(d.key).Append('=').Append(VectorSettingsData.Get(d.key).ToString("R",Culture)).Append('\n');
        foreach(string action in VectorInput.Actions)b.Append("Key.").Append(action).Append('=').Append(VectorInput.Binding(action)).Append('\n');
        b.Append("DisplayWidth=").Append(PlayerPrefs.GetInt("Vector.Display.Width",Mathf.Max(640,Screen.width))).Append('\n');
        b.Append("DisplayHeight=").Append(PlayerPrefs.GetInt("Vector.Display.Height",Mathf.Max(480,Screen.height))).Append('\n');
        b.Append("DisplayMode=").Append(PlayerPrefs.GetInt("Vector.Display.Mode",1)).Append('\n');
        foreach(string device in new[]{"InputDevice","OutputDevice"})b.Append(device).Append('=').Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(PlayerPrefs.GetString("VectorVoice."+device,"")))).Append('\n');
        return b.ToString();
    }
    public static bool TryParse(string text,out Dictionary<string,string> fields)
    {
        fields=new Dictionary<string,string>(StringComparer.Ordinal);
        if(text==null||Encoding.UTF8.GetByteCount(text)>MaxBytes||!text.StartsWith("PVSET1\n",StringComparison.Ordinal)||!text.EndsWith("\n",StringComparison.Ordinal))return false;
        foreach(string line in text.Substring(7).Split('\n')){
            if(line.Length==0)continue;
            int split=line.IndexOf('=');if(split<=0||!fields.TryAdd(line.Substring(0,split),line.Substring(split+1)))return false;
        }
        int count=5+VectorInput.Actions.Length;
        foreach(var d in VectorSettingsData.All.Where(d=>!d.Video)){
            count++;
            if(!fields.TryGetValue(d.key,out string s)||!float.TryParse(s,NumberStyles.Float,Culture,out float v)||float.IsNaN(v)||float.IsInfinity(v)||v<d.min||v>d.max)return false;
            if(d.step>=1&&v!=Mathf.Round(v))return false;
        }
        var used=new HashSet<string>(StringComparer.Ordinal);
        foreach(string action in VectorInput.Actions){
            if(!fields.TryGetValue("Key."+action,out string key)||!ValidBinding(key)||!used.Add(key))return false;
        }
        foreach(var item in new[]{("DisplayWidth",640,16384),("DisplayHeight",480,16384),("DisplayMode",0,2)})
            if(!fields.TryGetValue(item.Item1,out string s)||!int.TryParse(s,NumberStyles.None,Culture,out int v)||v<item.Item2||v>item.Item3)return false;
        foreach(string device in new[]{"InputDevice","OutputDevice"}){
            if(!fields.TryGetValue(device,out string s)||s.Length>344)return false;
            try{byte[] bytes=Convert.FromBase64String(s);if(bytes.Length>256)return false;new UTF8Encoding(false,true).GetString(bytes);}catch(ArgumentException){return false;}catch(FormatException){return false;}
        }
        return fields.Count==count;
    }
    public static bool ValidBinding(string key)
    {
        if(new[]{"Escape","Enter","NumpadEnter","Tab","F10","F3","B","Z"}.Contains(key))return false;
        if(new[]{"Mouse0","Mouse1","Mouse2","Mouse3","Mouse4"}.Contains(key))return true;
        return Enum.TryParse(key,out Key parsed)&&parsed!=Key.None&&Enum.IsDefined(typeof(Key),parsed)&&parsed.ToString()==key;
    }
    public static bool Apply(string text)
    {
        if(!TryParse(text,out var fields))return false;
        Applying=true;
        try{
            foreach(var d in VectorSettingsData.All.Where(d=>!d.Video))VectorSettingsData.Set(d.key,float.Parse(fields[d.key],Culture));
            // Apply the complete binding map atomically, including swapped keys.
            foreach(string action in VectorInput.Actions)PlayerPrefs.SetString("Vector.Key."+action,fields["Key."+action]);
            PlayerPrefs.SetInt("Vector.Display.Width",int.Parse(fields["DisplayWidth"],Culture));
            PlayerPrefs.SetInt("Vector.Display.Height",int.Parse(fields["DisplayHeight"],Culture));
            PlayerPrefs.SetInt("Vector.Display.Mode",int.Parse(fields["DisplayMode"],Culture));
            foreach(string device in new[]{"InputDevice","OutputDevice"})PlayerPrefs.SetString("VectorVoice."+device,Encoding.UTF8.GetString(Convert.FromBase64String(fields[device])));
            VectorVoiceChat.Instance?.ReloadAccountPreferences();
            VectorSettingsPanel.Instance?.ReloadAccountSettings();
            PlayerPrefs.Save();return true;
        }finally{Applying=false;}
    }
    public static string Defaults()
    {
        var b=new StringBuilder("PVSET1\n");
        foreach(var d in VectorSettingsData.All.Where(d=>!d.Video))b.Append(d.key).Append('=').Append(d.initial.ToString("R",Culture)).Append('\n');
        for(int i=0;i<VectorInput.Actions.Length;i++)b.Append("Key.").Append(VectorInput.Actions[i]).Append('=').Append(VectorInput.Defaults[i]).Append('\n');
        b.Append("DisplayWidth=").Append(Mathf.Max(640,Screen.width)).Append("\nDisplayHeight=").Append(Mathf.Max(480,Screen.height)).Append("\nDisplayMode=1\nInputDevice=\nOutputDevice=\n");
        return b.ToString();
    }
}

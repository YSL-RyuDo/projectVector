using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class KillLogManager : MonoBehaviour
{
    public static KillLogManager Instance;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private KillLogEntryUI ourKillLogPrefab,enemyKillLogPrefab;
    [SerializeField] private Sprite[] characterSprites;
    [SerializeField] private float displayDuration=6,fadeDuration=.35f;
    [SerializeField] private int maxVisibleLogs=5;
    [SerializeField] private KillLogEntryUI[] authoredEntries=new KillLogEntryUI[0];
    sealed class Record {public uint killer,dead;public bool ours,head;public int kc,dc,weapon=-2;public string kn,dn,assists="";public float expires;}
    readonly List<Record> records=new();
    void Awake(){if(Instance && Instance!=this){enabled=false;return;}Instance=this;ClearAll();}
    public void ShowKillLog(bool ours,int kc,int dc,string kn,string dn)=>ShowDeath(0,0,ours,kc,dc,kn,dn);
    public void ShowDeath(uint killer,uint dead,bool ours,int kc,int dc,string kn,string dn){records.Insert(0,new Record{killer=killer,dead=dead,ours=ours,kc=kc,dc=dc,kn=kn,dn=dn,expires=Time.unscaledTime+displayDuration});int count=Mathf.Min(maxVisibleLogs,authoredEntries.Length);if(records.Count>count)records.RemoveRange(count,records.Count-count);Redraw();}
    public void ApplyDetails(uint killer,uint dead,int weapon,bool head,string assists){var r=records.Find(x=>x.killer==killer && x.dead==dead);if(r==null)return;r.weapon=weapon;r.head=head;r.assists=assists;Redraw();}
    Sprite Portrait(int id)=>id>=0 && id<characterSprites.Length?characterSprites[id]:null;
    void Redraw(){for(int i=0;i<authoredEntries.Length;i++){var row=authoredEntries[i];if(!row)continue;row.gameObject.SetActive(i<records.Count);if(i>=records.Count)continue;var r=records[i];row.Initialize(Portrait(r.kc),Portrait(r.dc),r.kn,r.dn);row.SetTeam(r.ours);row.SetDetails(r.weapon,r.head,r.assists);var group=row.GetComponent<CanvasGroup>();if(group)group.alpha=1;}}
    void Update(){if(records.RemoveAll(r=>Time.unscaledTime>r.expires+fadeDuration)>0)Redraw();for(int i=0;i<records.Count;i++){var g=authoredEntries[i].GetComponent<CanvasGroup>();if(g)g.alpha=1-Mathf.Clamp01((Time.unscaledTime-records[i].expires)/Mathf.Max(.01f,fadeDuration));}}
    public void ClearAll(){records.Clear();foreach(var r in authoredEntries)if(r)r.gameObject.SetActive(false);}
    void OnDestroy(){if(Instance==this)Instance=null;}
}

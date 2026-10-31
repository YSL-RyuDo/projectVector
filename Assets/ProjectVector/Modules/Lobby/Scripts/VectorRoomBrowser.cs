using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Uses saved scene rows. Filtering and pagination never create UI objects.
public sealed class VectorRoomBrowser : MonoBehaviour
{
    public TMP_InputField search;
    public Button join, waitingFilter;
    public TMP_Text status, pageLabel;
    public RoomSlotUI[] rows;
    private List<RoomListData> rooms=new();
    private List<RoomListData> filtered=new();
    private uint? selected;
    private int page;
    private bool waitingOnly;
    void Awake(){search.onValueChanged.AddListener(_=>{page=0;selected=null;Refresh();});for(int i=0;i<rows.Length;i++){int slot=i;rows[i].GetComponent<Button>().onClick.AddListener(()=>Select(slot));}}
    public void Receive(List<RoomListData> value){rooms=value??new();selected=null;Refresh();}
    public void ToggleWaiting(){waitingOnly=!waitingOnly;page=0;selected=null;Refresh();}
    public void Previous(){page=Mathf.Max(0,page-1);selected=null;Refresh();}
    public void Next(){page++;selected=null;Refresh();}
    public void Join(){if(!selected.HasValue)return;var room=filtered.FirstOrDefault(r=>r.roomId==selected.Value);if(room==null||room.state!=0||room.currentPlayerCount>=room.maxPlayerCount)return;NetworkManager.Instance?.SendRoomEnter(room.roomId);}
    void Select(int slot){int index=page*rows.Length+slot;if(index>=filtered.Count)return;selected=filtered[index].roomId;Refresh();}
    void OnEnable(){selected=null;Refresh();}
    void Refresh()
    {
        string term=search.text.Trim();filtered=rooms.Where(r=>(!waitingOnly||r.state==0)&&(term.Length==0||(r.roomName??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||(r.hostName??"").IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||r.roomId.ToString()==term)).ToList();
        int pages=Mathf.Max(1,(filtered.Count+rows.Length-1)/rows.Length);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<rows.Length;i++){int index=page*rows.Length+i;rows[i].gameObject.SetActive(index<filtered.Count);if(index>=filtered.Count)continue;rows[i].SetData(filtered[index]);rows[i].GetComponent<Image>().color=selected==filtered[index].roomId?new Color32(91,117,147,255):new Color32(66,75,88,235);}
        var chosen=selected.HasValue?filtered.FirstOrDefault(r=>r.roomId==selected.Value):null;
        join.interactable=chosen!=null&&chosen.state==0&&chosen.currentPlayerCount<chosen.maxPlayerCount;
        status.text=filtered.Count==0?"표시할 방이 없습니다. 새로고침하거나 방을 만들어 보세요.":$"{filtered.Count}개의 방 · 참가할 방을 선택하세요";
        pageLabel.text=$"{page+1} / {pages}";waitingFilter.GetComponentInChildren<TMP_Text>().text=waitingOnly?"대기 중만 보기":"모든 방 보기";
    }
}

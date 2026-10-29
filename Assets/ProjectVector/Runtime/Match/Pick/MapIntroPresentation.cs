using TMPro;
using UnityEngine;

// A real flyover of the selected authored map, before the server's pick timer begins.
public sealed class MapIntroPresentation : MonoBehaviour
{
    public GameObject overlay;
    public Camera overviewCamera;
    public Transform[] startPoses, endPoses;
    public TMP_Text mapTitle, modeTitle;
    public float duration=3.4f;
    public CanvasGroup captionReveal;
    public bool IsPlaying { get; private set; }
    uint room;
    int map;
    float elapsed;
    public void Begin(PickStateData state)
    {
        if(state==null || state.roomId==0 || room==state.roomId) return;
        room=state.roomId;
        map=state.mapName=="Desert"?1:state.mapName=="TestMap"?0:state.mapName=="Construction"?2:-1;
        if(map<0) { Finish(); return; }
        elapsed=0; IsPlaying=true; mapTitle.text=state.mapName=="Construction"?"CONSTRUCTION":state.mapName=="Desert"?"DESERT":"TEST MAP";
        modeTitle.text=NetworkManager.Instance!=null && NetworkManager.Instance.LastMatchmakingMode==1?"랭크전  /  3 대 3":"팀 전투  /  3 대 3";
        overlay.SetActive(true); overlay.transform.SetAsLastSibling(); overviewCamera.enabled=true; Evaluate(0);
    }
    public void Evaluate(float t)
    {
        if(map<0 || map>=startPoses.Length) return;
        if(captionReveal!=null) captionReveal.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.10f)/.16f));
        t=Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        overviewCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPoses[map].position,endPoses[map].position,t),Quaternion.Slerp(startPoses[map].rotation,endPoses[map].rotation,t));
    }
    void Update() { if(!IsPlaying)return; elapsed+=Time.unscaledDeltaTime; Evaluate(elapsed/duration); if(elapsed>=duration)Finish(); }
    public void Finish() { IsPlaying=false; if(overlay!=null)overlay.SetActive(false); if(overviewCamera!=null)overviewCamera.enabled=false; }
    void OnDisable() => Finish();
}

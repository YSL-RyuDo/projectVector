using System;
using UnityEngine;

// Move the complete authored viewmodel, including both hands and held props.
// Restore before other pose writers so switching never accumulates an offset.
[DefaultExecutionOrder(-200)]
public sealed class FirstPersonEquipmentTransition : MonoBehaviour
{
    public Transform model;
    const float StowTime=.10f, DrawTime=.18f;
    PlayerItem inventory;
    Action commit;
    float started;
    int target=-1, observed=-1;
    bool applied, drawing, wasVisible;
    Vector3 basePosition;
    Quaternion baseRotation;
    public bool Busy { get; private set; }
    bool Local => PracticePlayerContext.For(this)!=null ||
        (NetworkManager.Instance && GetComponent<PlayerIdentity>()?.PlayerId==NetworkManager.Instance.MyPlayerId);
    void Awake()=>inventory=GetComponent<PlayerItem>();
    public void Request(int slot,Action change)
    {
        if(!model || !Local){change();return;}
        if(Busy && target==slot)return;
        target=slot;commit=change;started=Time.time;drawing=false;Busy=true;
        GetComponentInChildren<FPSCameraLook>()?.ForceExitADS();
    }
    void Restore()
    {
        if(!applied||!model)return;
        model.localPosition=basePosition;model.localRotation=baseRotation;applied=false;
    }
    void Update()
    {
        Restore();
        if(!Busy)return;
        if(!Local || !GameplayActionPolicy.CanAct(this,GameplayAction.WeaponChange,false)) { Cancel(); return; }
        if(!drawing && Time.time-started>=StowTime)
        {
            var action=commit;commit=null;action?.Invoke();
            observed=inventory.CurrentIndex;drawing=true;started=Time.time;
        }
        else if(drawing && Time.time-started>=DrawTime) { Busy=false;target=-1; }
    }
    public void ApplyViewOffset()
    {
        if(!model||!Local)return;
        // Inventory acknowledgements and skill recovery use the same draw,
        // while remaining authoritative and immediately updating their data.
        bool visible=inventory.CurrentIndex>=0 && inventory.CurrentIndex<inventory.Items.Length &&
            inventory.Items[inventory.CurrentIndex] && inventory.Items[inventory.CurrentIndex].gameObject.activeInHierarchy;
        var device=GetComponent<SpikeInteractionPresentation>();
        visible|=device && device.IsCarryingDevice;
        if(observed!=inventory.CurrentIndex || visible&&!wasVisible)
        {
            bool hadWeapon=observed>=0;
            observed=inventory.CurrentIndex;
            if(!Busy&&hadWeapon&&GameplayActionPolicy.CanAct(this,GameplayAction.WeaponChange,false))
            {drawing=true;started=Time.time;Busy=true;target=observed;}
        }
        wasVisible=visible;
        if(!Busy)return;
        float fraction=drawing?1-Mathf.SmoothStep(0,1,(Time.time-started)/DrawTime):Mathf.SmoothStep(0,1,(Time.time-started)/StowTime);
        basePosition=model.localPosition;baseRotation=model.localRotation;applied=true;
        var camera=GetComponent<PlayerCameraController>()?.PlayerCamera;
        if(camera)model.position+=camera.transform.rotation*new Vector3(.015f,-.34f,-.06f)*fraction;
        model.localRotation=baseRotation*Quaternion.Euler(10*fraction,0,0);
    }
    public void Cancel(){Restore();Busy=false;drawing=false;commit=null;target=-1;}
    void OnDisable()=>Cancel();
}

using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class VectorSettingRow : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler
{
    public string key;
    public bool binding;
    public TMP_Text title, value;
    public TMP_InputField number;
    public Slider slider;
    public Button previous, next, capture;
    public Image selected;
    public Image[] choiceMarks;
    VectorSettingsPanel owner;
    public void Bind(VectorSettingsPanel panel)
    {
        owner=panel;
        if(binding)capture.onClick.AddListener(()=>owner.BeginBinding(this));
        else {
            var d=VectorSettingsData.Find(key);
            if(slider!=null) {slider.minValue=d.min;slider.maxValue=d.max;slider.onValueChanged.AddListener(v=>Change(v));}
            if(previous!=null)previous.onClick.AddListener(()=>Step(-1));
            if(next!=null)next.onClick.AddListener(()=>Step(1));
            if(number!=null)number.onEndEdit.AddListener(s=> { if(float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out float v))Change(v);Refresh(); });
        }
        Refresh();
    }
    void Change(float v) { if(!NetworkManager.CanEditAccountSettings){Refresh();return;}VectorSettingsData.Set(key,v);Refresh();owner.Describe(this);owner.Edited(); }
    void Step(int dir) { var d=VectorSettingsData.Find(key);float v=VectorSettingsData.Get(key)+dir*d.step; if(d.choices!=null){if(v>d.max)v=d.min;if(v<d.min)v=d.max;}Change(v); }
    public void Refresh()
    {
        bool editable=NetworkManager.CanEditAccountSettings;
        foreach(var control in new Selectable[]{slider,number,previous,next,capture})if(control!=null)control.interactable=editable;
        if(binding){value.text=VectorInput.Label(key);return;}
        var d=VectorSettingsData.Find(key);float v=VectorSettingsData.Get(key);
        if(slider!=null)slider.SetValueWithoutNotify(v);
        if(choiceMarks!=null)for(int i=0;i<choiceMarks.Length;i++)choiceMarks[i].color=i==Mathf.Clamp((int)v,0,choiceMarks.Length-1)?new Color(.94f,.23f,.32f):new Color(.25f,.28f,.32f);
        if(number!=null)number.SetTextWithoutNotify(v.ToString(d.step<1?"0.00":"0",CultureInfo.InvariantCulture));
        if(value!=null)value.text=d.choices!=null?d.choices[Mathf.Clamp((int)v,0,d.choices.Length-1)]:v.ToString("0.##",CultureInfo.InvariantCulture);
    }
    public void OnPointerEnter(PointerEventData e)=>owner?.Describe(this);
    public void OnPointerClick(PointerEventData e)=>owner?.Describe(this);
    public void OnSelect(BaseEventData e)=>owner?.Describe(this);
}

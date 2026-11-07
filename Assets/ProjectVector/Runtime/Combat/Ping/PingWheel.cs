using UnityEngine;
using UnityEngine.UI;

namespace Game.Pings
{
    public sealed class PingWheel:MonoBehaviour
    {
        public PingGraphics[] segments,rims,icons;
        public Text[] labels;
        public RectTransform pointer;
        public Text centerLabel;
        public PingGraphics cancelCircle;
        PingOption[] options;
        Vector2 cursorPosition;
        float radius,deadZone;
        public int SelectedIndex{get;private set;}=-1;
        public void Initialize(PingOption[] choices,float wheelRadius,float selectionDeadZone)
        {options=choices;radius=wheelRadius;deadZone=selectionDeadZone;gameObject.SetActive(false);}
        public void Open(){cursorPosition=Vector2.zero;SelectedIndex=-1;gameObject.SetActive(true);Move(Vector2.zero);}
        public void Move(Vector2 delta)
        {
            cursorPosition=Vector2.ClampMagnitude(cursorPosition+delta,radius*.91f);pointer.anchoredPosition=cursorPosition;
            SelectedIndex=-1;
            if(cursorPosition.sqrMagnitude>deadZone*deadZone)
            {
                float angle=Mathf.Repeat(Mathf.Atan2(cursorPosition.x,cursorPosition.y)*Mathf.Rad2Deg,360);
                SelectedIndex=Mathf.FloorToInt((angle+45)/90)%options.Length;
            }
            centerLabel.text=SelectedIndex<0?"취소":options[SelectedIndex].label;
            centerLabel.color=SelectedIndex<0?new Color(.7f,.75f,.8f):options[SelectedIndex].color;
            cancelCircle.color=new Color(.055f,.075f,.10f,.94f);
            for(int i=0;i<segments.Length;i++)
            {
                bool active=i==SelectedIndex;
                segments[i].color=active?new Color(.13f,.19f,.24f,.97f):new Color(.055f,.075f,.10f,.9f);
                rims[i].color=active?options[i].color:new Color(.30f,.40f,.49f,.6f);
                labels[i].color=active?Color.white:new Color(.73f,.78f,.82f);
                icons[i].color=active?options[i].color:new Color(.65f,.74f,.81f);
            }
        }
        public void Close()=>gameObject.SetActive(false);
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Game.Pings
{
    public sealed class PingMarker:MonoBehaviour
    {
        public RectTransform worldRoot,hudIcon,fullIcon;
        public PingGraphics icon,hudGlyph,fullGlyph;
        public Image accent;
        public Text title,detail;
        public CanvasGroup fade;
        public float ExpiresAt{get;private set;}
        public uint SenderId{get;private set;}
        public bool Active{get;private set;}
        Vector3 worldPosition;
        string senderName;
        public void Show(uint sender,Vector3 position,PingOption option,string nickname,float expires)
        {
            SenderId=sender;worldPosition=position;senderName=nickname;ExpiresAt=expires;Active=true;
            title.text=option.label;title.color=option.color;accent.color=option.color;
            foreach(var graphic in new[]{icon,hudGlyph,fullGlyph})
            {graphic.glyph=(int)option.type+1;graphic.color=option.color;graphic.SetVerticesDirty();}
            gameObject.SetActive(true);
        }
        public void Hide(){Active=false;gameObject.SetActive(false);}
        public void Refresh(Camera camera,RectTransform canvas,Camera map,RectTransform hud,RectTransform full)
        {
            bool available=camera&&camera.isActiveAndEnabled;
            worldRoot.gameObject.SetActive(available);
            if(available)
            {
                var projected=camera.WorldToScreenPoint(worldPosition);
                var viewport=camera.pixelRect;var offset=(Vector2)projected-viewport.center;
                var extents=Vector2.Max(new Vector2(viewport.width*.5f-55,viewport.height*.5f-65),Vector2.one*10);
                bool outside=projected.z<=0||Mathf.Abs(offset.x)>extents.x||Mathf.Abs(offset.y)>extents.y;
                if(outside)
                {
                    if(projected.z<=0)offset=-offset;
                    if(offset.sqrMagnitude<.001f)offset=Vector2.down;
                    offset*=Mathf.Min(extents.x/Mathf.Max(Mathf.Abs(offset.x),.001f),extents.y/Mathf.Max(Mathf.Abs(offset.y),.001f));
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,viewport.center+offset,null,out var local);
                worldRoot.anchoredPosition=local;
                detail.text=senderName+"  /  "+Mathf.RoundToInt(Vector3.Distance(camera.transform.position,worldPosition))+" m";
                fade.alpha=Mathf.Clamp01((ExpiresAt-Time.unscaledTime)/.35f)*(offset.magnitude<60?.65f:1);
            }
            PlaceMap(hudIcon,hud,map,canvas);PlaceMap(fullIcon,full,map,canvas);
        }
        void PlaceMap(RectTransform icon,RectTransform mapRect,Camera map,RectTransform canvas)
        {
            bool show=mapRect&&mapRect.gameObject.activeInHierarchy&&map&&map.isActiveAndEnabled;
            Vector3 uv=show?map.WorldToViewportPoint(worldPosition):Vector3.zero;
            show&=uv.z>0&&uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1;
            icon.gameObject.SetActive(show);if(!show)return;
            var r=mapRect.rect;
            var point=mapRect.TransformPoint(new Vector3(r.xMin+uv.x*r.width,r.yMin+uv.y*r.height));
            var mapCanvas=mapRect.GetComponentInParent<Canvas>();
            var screen=RectTransformUtility.WorldToScreenPoint(mapCanvas&&mapCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?mapCanvas.worldCamera:null,point);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out var local);
            icon.anchoredPosition=local;
        }
    }
}

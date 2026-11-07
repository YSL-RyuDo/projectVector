#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ShopReadabilityVerification
{
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
    public static void Run()
    {
        InGameUiVerification.VerifySaved();
        var store=UnityEngine.Object.FindFirstObjectByType<StorePanelUI>(FindObjectsInactive.Include);
        var hud=UnityEngine.Object.FindFirstObjectByType<InGameUIManager>(FindObjectsInactive.Include);
        var root=InGameUiAuthoring.Get<GameObject>(store,"storePanelRoot");var details=root.GetComponentInChildren<StoreWeaponDetailsUI>(true);
        var bars=details.stats.Select(s=>s.fill).Concat(new[]{InGameUiAuthoring.Get<Image>(hud,"hpBar"),InGameUiAuthoring.Get<Image>(hud,"barrierBar")}).ToArray();
        var canvasGO=new GameObject("Fill geometry test",typeof(RectTransform),typeof(Canvas));canvasGO.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        foreach(var source in bars){
            Check(source.sprite.texture.GetPixels().All(c=>c.a==1),"fill sprite has no transparent pixels: "+source.name);
            Check(source.fillOrigin==0 && source.type==Image.Type.Filled && source.fillMethod==Image.FillMethod.Horizontal && !source.preserveAspect,"left-origin horizontal solid fill");
            var image=UnityEngine.Object.Instantiate(source,canvasGO.transform,false);image.enabled=true;image.gameObject.SetActive(true);
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.sizeDelta=new Vector2(400,8);rect.anchoredPosition=Vector2.zero;
            foreach(float fraction in new[]{0f,.28f,.5f,1f}){
                image.fillAmount=fraction;image.SetAllDirty();Canvas.ForceUpdateCanvases();var mesh=image.canvasRenderer.GetMesh();
                if(fraction==0)Check(mesh==null || mesh.vertexCount==0,"zero fill has no geometry");
                else{
                    var vertices=mesh.vertices;Check(vertices.Length>=4,"visible fill mesh");
                    Check(Mathf.Abs(vertices.Min(p=>p.x)-rect.rect.xMin)<.01f,"fill begins at track start");
                    Check(Mathf.Abs(vertices.Max(p=>p.x)-(rect.rect.xMin+rect.rect.width*fraction))<.01f,"fill endpoint equals actual value");
                    Check(Mathf.Abs(vertices.Max(p=>p.y)-vertices.Min(p=>p.y)-rect.rect.height)<.01f,"fill uses full track height");
                }

            }
            UnityEngine.Object.DestroyImmediate(image.gameObject);
        }
        UnityEngine.Object.DestroyImmediate(canvasGO);
        var board=new ScoreboardData{roomId=33,myTeam=0};board.members.Add(new ScoreboardMemberData{playerId=1,team=0});board.members.Add(new ScoreboardMemberData{playerId=2,team=0});board.members.Add(new ScoreboardMemberData{playerId=3,team=1});
        byte[] Packet(uint player=2,uint payer=0,byte status=1){using var ms=new MemoryStream();using var w=new BinaryWriter(ms);w.Write((uint)33);w.Write((byte)0);w.Write((byte)1);w.Write((byte)1);w.Write(player);w.Write((ulong)1001);w.Write(12);w.Write(4000);w.Write(payer);w.Write(status);return ms.ToArray();}
        Check(NetworkManager.TryReadTeamPurchases(Packet(),board,out var requests) && requests[0].price==4000 && requests[0].requestId==1001,"real wire layout for pending request");
        Check(NetworkManager.TryReadTeamPurchases(Packet(2,1,2),board,out _),"fulfilled request includes payer");
        Check(!NetworkManager.TryReadTeamPurchases(Packet(3),board,out _),"enemy request excluded");
        Check(!NetworkManager.TryReadTeamPurchases(Packet(2,3,2),board,out _),"foreign payer excluded");
        var packet=Packet();for(int n=0;n<packet.Length;n++)Check(!NetworkManager.TryReadTeamPurchases(packet.Take(n).ToArray(),board,out _),"truncation rejected");
        packet[0]++;Check(!NetworkManager.TryReadTeamPurchases(packet,board,out _),"foreign room rejected");
        var team=root.GetComponentInChildren<StoreTeamPanelUI>(true);Check(team.rows.Length==3,"three saved team rows");
        var ally=new ScoreboardMemberData{playerId=2,nickname="KSW",credits=100,hasLoadout=true,hasLifeState=true,equipmentAlive=true};
        var q=new TeamPurchaseRequest{playerId=2,weaponId=12,price=4000,requestId=1001,status=1};
        team.rows[1].Present(ally,null,1,3999,q,true,board);Check(!team.rows[1].action.interactable,"insufficient credit disabled");
        team.rows[1].Present(ally,null,1,4000,q,true,board);Check(team.rows[1].action.interactable && team.rows[1].actionText.text.Contains("4,000"),"exact budget can fulfil");
        team.rows[1].Present(ally,null,2,0,q,true,board);Check(team.rows[1].action.interactable && team.rows[1].actionText.text=="요청 취소","owner can cancel without funds");
        q.status=2;q.payerId=1;team.rows[1].Present(ally,null,1,4000,q,true,board);Check(!team.rows[1].action.gameObject.activeSelf && team.rows[1].requestText.text.Contains("구매 완료"),"delivered request cannot repeat");
        File.WriteAllText(ShopReadabilityAuthoring.Output+"/Verification.txt","SHOP_VERIFICATION_PASS checks="+checks+"\nExisting in-game UI regression suite also passed. Solid mesh 0/28/50/100%, no texture alpha, request protocol privacy/truncation, affordability/cancel/completion controls.\n");
        Debug.Log("SHOP_VERIFICATION_PASS checks="+checks);
    }
}
#endif

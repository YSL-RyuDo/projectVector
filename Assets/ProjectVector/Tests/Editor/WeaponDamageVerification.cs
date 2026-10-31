#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WeaponDamageVerification
{
    static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    static readonly int[] Base={20,25,50,24,23,20,19,75,35,40,37,105,150,50};
    // Independently specified golden values, columns=head/body/legs.
    static readonly int[,] Near={{40,20,17},{50,25,21},{125,50,42},{48,24,19},{46,23,18},{30,20,15},{28,19,14},{150,75,63},{140,35,29},{160,40,34},{92,37,31},{210,105,78},{300,150,112},{50,50,50}};
    static readonly int[,] Far={{26,13,11},{37,18,15},{100,40,34},{26,13,10},{29,14,11},{10,7,5},{8,5,4},{127,63,54},{112,28,23},{144,36,30},{74,29,25},{189,94,70},{270,135,101},{50,50,50}};
    static readonly int[] Start={15,20,30,10,15,5,6,40,20,30,25,50,80,0};
    static readonly int[] End={35,40,60,30,40,20,25,80,50,60,65,100,140,0};
    public static void Run()
    {
        Check(Application.isBatchMode,"Batch mode only");
        var cross=new StringBuilder();
        for(int id=0;id<14;id++) for(int region=0;region<3;region++)
        {
            int zone=region==0?1:region==1?0:2;
            Check(WeaponDamage.Calculate(id,Base[id],Start[id],zone)==Near[id,region],$"Near {id}/{zone}");
            Check(WeaponDamage.Calculate(id,Base[id],End[id],zone)==Far[id,region],$"Far {id}/{zone}");
            int previous=int.MaxValue;
            for(int step=0;step<=600;step++)
            {
                double distance=step*0.25;
                int damage=WeaponDamage.Calculate(id,Base[id],distance,zone);
                Check(damage<=previous && damage>0,$"Monotonic {id}/{zone}/{distance}");previous=damage;
                cross.AppendLine(FormattableString.Invariant($"{id} {Base[id]} {distance} {zone} {damage}"));
            }
        }
        Check(WeaponDamage.Calculate(-1,20,0,0)==0 && WeaponDamage.Calculate(0,20,double.NaN,0)==0 &&
            WeaponDamage.Calculate(0,20,double.PositiveInfinity,0)==0 && WeaponDamage.Calculate(0,20,-1,0)==0 &&
            WeaponDamage.Calculate(0,20,0,3)==0,"Invalid input");
        string output=Path.Combine(Path.GetTempPath(),"VectorDamageVerification");Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"client-damage-fixtures.txt"),cross.ToString());
        VerifyPackets();
        ShotConsistencyVerification.Run();
        Debug.Log("WEAPON_DAMAGE_CLIENT_PASS: 14 weapons, 84 golden endpoints, 25242 monotonic samples, real spatial TCP packets and existing collision/region tests");
    }
    static void VerifyPackets()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var net=new GameObject("DamagePacketFixture").AddComponent<NetworkManager>();
        var transport=(NetworkClient)typeof(NetworkManager).GetField("client",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(net);
        var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);
        System.Net.Sockets.TcpClient peer=null;
        try
        {
            listener.Start();transport.Connect("127.0.0.1",((System.Net.IPEndPoint)listener.LocalEndpoint).Port);
            peer=listener.AcceptTcpClient();peer.GetStream().ReadTimeout=3000;
            byte[] Read(int length) { var data=new byte[length];int at=0;while(at<length){int n=peer.GetStream().Read(data,at,length-at);Check(n>0,"Closed socket");at+=n;}return data; }
            uint shot=net.SendTrackedFire(8,new Vector3(1,2,3));var fire=Read(24);
            Check(BitConverter.ToUInt16(fire,0)==24 && BitConverter.ToUInt32(fire,4)==shot && BitConverter.ToInt32(fire,8)==8 &&
                BitConverter.ToSingle(fire,12)==1 && BitConverter.ToSingle(fire,16)==2 && BitConverter.ToSingle(fire,20)==3,"Spatial fire layout");
            net.SendTrackedHit(2,(byte)2,shot,3,new Vector3(4,5,6));var hit=Read(26);
            Check(BitConverter.ToUInt16(hit,0)==26 && hit[8]==2 && BitConverter.ToUInt32(hit,9)==shot && hit[13]==3 &&
                BitConverter.ToSingle(hit,14)==4 && BitConverter.ToSingle(hit,18)==5 && BitConverter.ToSingle(hit,22)==6,"Spatial hit layout");
        }
        finally {transport.Close();peer?.Close();listener.Stop();UnityEngine.Object.DestroyImmediate(net.gameObject);}
    }
}
#endif

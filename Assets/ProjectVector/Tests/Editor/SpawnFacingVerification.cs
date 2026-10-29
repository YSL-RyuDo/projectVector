#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpawnFacingVerification
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static void Call(object obj,string method) => obj.GetType().GetMethod(method,Flags).Invoke(obj,null);
    static void Check(bool ok,string message) {if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode,"Batch only; temporary scene");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var report=new StringBuilder();int count=0;
        foreach(string map in new[]{"TestMap","Desert"})
        {
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Maps/{map}.prefab"));
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var info=root.GetComponent<MapInfo>();
            foreach(var points in new[]{info.attackSpawnPoints,info.defendSpawnPoints})
            {
                var opposite=points==info.attackSpawnPoints?info.defendSpawnPoints:info.attackSpawnPoints;
                var center=opposite.Aggregate(Vector3.zero,(sum,p)=>sum+p.position)/opposite.Length;
                foreach(var spawn in points)
                {
                    Check(Vector3.Dot(spawn.forward,(center-spawn.position).normalized)>.7f,map+" spawn faces away from battlefield");
                    foreach(var teammate in points.Where(p=>p!=spawn))
                        Check(Mathf.Abs(Vector3.Dot(spawn.forward,teammate.position-spawn.position))<.1f,map+" teammates stand in front instead of side by side");
                    foreach(string role in new[]{"Initiator","Controller","Sentinel","Entry"})
                    {
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Character/{role}/{role}.prefab");
                        Check(prefab!=null,"Missing role "+role);
                        var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        actor.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
                        var look=actor.GetComponentInChildren<FPSCameraLook>(true);
                        var movement=actor.GetComponent<PlayerMovement>();
                        Call(look,"Awake");Call(movement,"Awake");
                        look.SetLocalPlayer(true,actor.transform);
                        Check(Mathf.Abs(Mathf.DeltaAngle(look.GetYaw(),spawn.eulerAngles.y))<.01f,"First spawn yaw overwritten: "+role);
                        Call(movement,"Start");
                        look.ResetLookRotation(spawn.eulerAngles.y+37);
                        look.SetLocalPlayer(true,actor.transform);
                        Check(Mathf.Abs(Mathf.DeltaAngle(look.GetYaw(),spawn.eulerAngles.y+37))<.01f,"View refresh erased aim");
                        actor.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
                        movement.ResetAfterRespawn(spawn.eulerAngles.y);
                        Check(Mathf.Abs(Mathf.DeltaAngle(look.GetYaw(),spawn.eulerAngles.y))<.01f,"Round two camera yaw wrong");
                        Check(Vector3.Dot(actor.transform.forward,spawn.forward)>.999f,"Root facing disagrees with camera");
                        Check(Mathf.Abs(look.GetPitch())<.01f,"Respawn pitch not reset");
                        count++;UnityEngine.Object.DestroyImmediate(actor);
                    }
                }
            }
            report.AppendLine($"PASS {map}: attack/defend face battlefield; all three slots abreast; 4 characters first spawn/round reset/repeated view initialization");
            UnityEngine.Object.DestroyImmediate(root);
        }
        report.AppendLine($"SPAWN_FACING_PASS: {count} map/slot/character combinations; saved spawn transforms retained; no account or scene writes");
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"VectorSpawnFacingVerification.txt"),report.ToString());Debug.Log(report);
    }
}
#endif

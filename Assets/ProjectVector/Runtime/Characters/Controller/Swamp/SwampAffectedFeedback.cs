using System.Collections.Generic;
using UnityEngine;

public sealed class SwampAffectedFeedback : MonoBehaviour
{
    private readonly HashSet<SwampArea> _sources=new();
    private GameObject _effect;
    public void Enter(SwampArea source,GameObject prefab,AudioClip sound)
    {
        if(!_sources.Add(source)||_sources.Count!=1)return;
        if(prefab)_effect=Instantiate(prefab,transform);
        if(sound)AudioSource.PlayClipAtPoint(sound,transform.position+Vector3.up,.35f);
    }
    public void Exit(SwampArea source){_sources.Remove(source);if(_sources.Count==0)ClearEffect();}
    private void LateUpdate()
    {
        if(!_effect)return;
        var cc=GetComponent<CharacterController>();
        float foot=cc?(cc.center.y-cc.height*.5f)*transform.lossyScale.y:0;
        _effect.transform.SetPositionAndRotation(transform.position+Vector3.up*(foot+.06f),Quaternion.identity);
        _effect.transform.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1/transform.lossyScale.z);
    }
    private void ClearEffect(){if(_effect)Destroy(_effect);_effect=null;}
    private void OnDisable(){_sources.Clear();ClearEffect();}
}

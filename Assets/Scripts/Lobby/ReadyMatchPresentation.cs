using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ReadyMatchPresentation : MonoBehaviour
{
    public MatchIdentityCard[] cards;
    uint token;
    Coroutine entering;
    public void SetState(uint id, byte phase)
    {
        if (phase == 1) return;
        if (phase == 2) { token=0; return; }
        if(token==id) return;
        token=id;
        foreach(var card in cards) card.Present("",null,false,false);
    }
    public bool EnterMatchScene()
    {
        if(token==0 || !isActiveAndEnabled) return false;
        if(entering==null) entering=StartCoroutine(EnterAfterReveal(token));
        return true;
    }
    IEnumerator EnterAfterReveal(uint acceptedToken)
    {
        yield return new WaitForSecondsRealtime(1.2f);
        var net=NetworkManager.Instance;
        if(token==acceptedToken && net!=null && net.IsConnected && net.IsAuthenticated)
            SceneManager.LoadSceneAsync("Game_TestVersion");
        entering=null;
    }
    void OnDisable() { if(entering!=null)StopCoroutine(entering);entering=null; }
    public void Present(uint id,string[] names,uint[] players,Dictionary<uint,MatchAppearanceData> appearance,byte acceptedMask,uint me)
    {
        if(token!=id || names.Length!=cards.Length || players.Length!=cards.Length) return;
        for(int i=0;i<cards.Length;i++)
            cards[i].Present(names[i],appearance.TryGetValue(players[i],out var a)?a:null,(acceptedMask&(1<<i))!=0,players[i]==me);
    }
}

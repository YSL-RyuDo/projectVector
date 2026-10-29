using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TeamLifeBadge : MonoBehaviour
{
    public TMP_Text state,nickname;
    public Image line;
    public GameObject deathMark;
    public Color teamColor=Color.white;
    public void Present(bool dead)
    {
        if(deathMark)deathMark.SetActive(dead);
        if(state){state.text=dead?"사망":"";state.color=new Color(.93f,.76f,.77f);}
        if(line)line.color=dead?new Color(.33f,.37f,.41f):teamColor;
    }
}

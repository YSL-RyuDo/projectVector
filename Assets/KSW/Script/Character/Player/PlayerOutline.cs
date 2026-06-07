using UnityEngine;

public class PlayerOutline : MonoBehaviour
{
    private Outline _outline;

    private void Start()
    {
        _outline = GetComponent<Outline>();
    }

    public void Team(int value)
    {
        switch(value)
        {
            case 0: _outline.OutlineColor = Color.red; break;
            case 1: _outline.OutlineColor = Color.blue; break;
        }
    }
}
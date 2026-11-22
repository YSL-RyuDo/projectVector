using UnityEngine;

public class MovementSpeedItem : UsableItem
{
    public MovementSpeedItem()
    {
        respawnTime = 1f;
    }

    private void Reset()
    {
        respawnTime = 1f;
    }

    protected override void Awake()
    {
        base.Awake();
        if (respawnTime <= 0f)
        {
            respawnTime = 1f;
        }
    }
}

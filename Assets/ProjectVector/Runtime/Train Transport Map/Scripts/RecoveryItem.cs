using UnityEngine;

public class RecoveryItem : UsableItem
{
    public RecoveryItem()
    {
        respawnTime = 60f;
    }

    private void Reset()
    {
        respawnTime = 60f;
    }

    protected override void Awake()
    {
        base.Awake();
        if (respawnTime <= 0f)
        {
            respawnTime = 60f;
        }
    }
}

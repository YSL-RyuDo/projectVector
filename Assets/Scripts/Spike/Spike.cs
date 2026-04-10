using UnityEngine;

public class Spike : Item
{
    private bool isPlayerInside;
    private PlayerSpike playerSpike;

    [Header("Defuse")]
    public float defuseTime = 5f;
    private float currentDefuseTime;
    private bool isDefusing;

    [Header("Explosion")]
    public float explosionTime = 40f;
    private float currentExplosionTime;
    private bool isExploded;

    private void Awake()
    {
        playerSpike = GetComponent<PlayerSpike>();
    }

    void Update()
    {
        ExplosionTimer();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerSpike = null;
        isPlayerInside = false;

        CancelDefuse();
    }

    public override void OnUse()
    {
        playerSpike.TryStartPlant();
    }

    public override void OnDrop()
    {
        playerSpike.RequestDrop();
        Destroy(this);
    }

    //  Defuse ½ÃÀÛ
    public void TryStartDefuse()
    {
        if (!isPlayerInside) return;

        Debug.Log("Start Defuse");

        isDefusing = true;
        currentDefuseTime = 0f;
    }

    public void Defusing()
    {
        if (!isDefusing) return;

        currentDefuseTime += Time.deltaTime;

        if (currentDefuseTime >= defuseTime)
        {
            CompleteDefuse();
        }
    }

    public void CancelDefuse()
    {
        if (!isDefusing) return;

        Debug.Log("Cancel Defuse");

        isDefusing = false;
        currentDefuseTime = 0f;
    }

    void CompleteDefuse()
    {
        Debug.Log("Defuse Complete - ¼öºñÆÀ ½Â¸®");

        isExploded = true;

        Destroy(gameObject);
    }

    //  Æø¹ß Å¸ÀÌ¸Ó
    void ExplosionTimer()
    {
        if (isExploded) return;

        currentExplosionTime += Time.deltaTime;

        if (currentExplosionTime >= explosionTime)
        {
            Explode();
        }
    }

    void Explode()
    {
        isExploded = true;

        Debug.Log("Spike Exploded - °ø°ÝÆÀ ½Â¸®");

        Destroy(gameObject);
    }
}
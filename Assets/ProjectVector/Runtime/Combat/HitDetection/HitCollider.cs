using UnityEngine;

public enum HitPart
{
    Head = 0, // ¸Ó¸®
    Body = 1, // ¸öÅë(ÆÈ, ¼Õ Æ÷ÇÔ)
    Legs = 2 // ´Ù¸®
}

public class HitCollider : MonoBehaviour
{
    [SerializeField] private HitPart hitPart;

    public HitPart Part => hitPart;
}
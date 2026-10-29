using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public virtual void OnEquip() { gameObject.SetActive(true); }
    public virtual void OnUnequip() { gameObject.SetActive(false); }

    public virtual void OnUse() { }      // 좌클릭
    public virtual void OnAltUse() { }   // 우클릭
    public virtual void OnDrop() { }     // G
}
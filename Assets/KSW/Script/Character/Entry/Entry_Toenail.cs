using UnityEngine;

public class Entry_Toenail : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyStatus>().TakeDamage(10f);
            Destroy(this.gameObject);
        }
    }
}

using UnityEngine;

public class FlashGrenade : MonoBehaviour
{
    bool exploded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (exploded) return;

        exploded = true;

        if (
           // collision.gameObject.CompareTag("Player") || ÇÃ·¹ÀÌ¾î ¿µÇâ ¿©ºÎ 
            collision.gameObject.CompareTag("Enemy")
        )
        {
            Flash flash =
                collision.gameObject.GetComponentInChildren<Flash>();

            if (flash != null)
            {
                flash.flash();
            }

            Debug.Log("¼¶±¤Åº ¸ÂÀ½");
        }

        Destroy(gameObject);
    }
}
using UnityEngine;
using TMPro;

public class TutoEnemymove : MonoBehaviour
{
    public Transform leftPoint;
    public Transform rightPoint;
    public float speed = 3f;
    public int hp = 100;
    public TMP_Text hpText;

    Transform target;

    void Start()
    {
        target = rightPoint;
        hpText.text = hp.ToString();
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (transform.position == target.position)
            target = target == rightPoint ? leftPoint : rightPoint;
    }
    public void Damage(int damage)
    {
        hp -= damage;
        hpText.text = hp.ToString();

        if (hp <= 0)
            Destroy(gameObject);
    }

}

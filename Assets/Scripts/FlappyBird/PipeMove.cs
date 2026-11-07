
using UnityEngine;

public class PipeMove : MonoBehaviour
{
    [SerializeField] private float speed = 3f;

    private void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;

        if (transform.position.x < -12f)
        {
            Destroy(gameObject);
        }
    }
}

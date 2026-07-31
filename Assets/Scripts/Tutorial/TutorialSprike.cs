using UnityEngine;

public class TutorialSprike : MonoBehaviour
{
    public bool isPlanted = false;

    public void Plant(Vector3 position)
    {
        transform.position = position;
        isPlanted = true;
    }
}

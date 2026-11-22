using System.Collections;
using UnityEngine;

public class ItemRespawnRunner : MonoBehaviour
{
    private static ItemRespawnRunner instance;

    public static ItemRespawnRunner Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ItemRespawnRunner>();
                if (instance == null)
                {
                    GameObject runnerObj = new GameObject("[ItemRespawnRunner]");
                    instance = runnerObj.AddComponent<ItemRespawnRunner>();
                    DontDestroyOnLoad(runnerObj);
                }
            }
            return instance;
        }
    }

    public void Respawn(GameObject target, float delay)
    {
        if (target != null)
        {
            StartCoroutine(RespawnRoutine(target, delay));
        }
    }

    private IEnumerator RespawnRoutine(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target != null && !target.activeSelf)
        {
            target.SetActive(true);
        }
    }
}

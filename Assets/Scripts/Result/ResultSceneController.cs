using UnityEngine;

public class ResultSceneController : MonoBehaviour
{
    public void OnClickLeaveResult()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.SendResultLeave();
    }
}
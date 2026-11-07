using UnityEngine;

public class LocalPlayerDroneVisibility : MonoBehaviour
{
    [Header("드론 모드일 때 보이게 할 내 캐릭터 3D 모델")]
    [SerializeField] private Renderer[] _bodyRenderers;

    [Header("드론 모드일 때 숨길 1인칭 팔/무기")]
    [SerializeField] private GameObject[] _firstPersonObjects;

    private void Awake()
    {
        SetDroneModeVisible(false);
    }

    public void SetDroneModeVisible(bool droneMode)
    {
        if (_bodyRenderers != null)
        {
            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                if (_bodyRenderers[i] != null)
                    _bodyRenderers[i].enabled = droneMode;
            }
        }

        if (_firstPersonObjects != null)
        {
            for (int i = 0; i < _firstPersonObjects.Length; i++)
            {
                if (_firstPersonObjects[i] != null)
                    _firstPersonObjects[i].SetActive(!droneMode);
            }
        }
    }
}
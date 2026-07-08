using UnityEngine;

public class LocalDroneBodyProxy : MonoBehaviour
{
    [Header("드론 카메라에서 보일 내 3인칭 캐릭터 프리팹")]
    [SerializeField] private GameObject _bodyPrefab;

    [Header("위치 보정")]
    [SerializeField] private Vector3 _positionOffset = Vector3.zero;
    [SerializeField] private Vector3 _rotationOffset = Vector3.zero;

    private GameObject _bodyInstance;

    private void LateUpdate()
    {
        if (_bodyInstance == null)
            return;

        _bodyInstance.transform.position =
            transform.position + transform.TransformDirection(_positionOffset);

        _bodyInstance.transform.rotation =
            transform.rotation * Quaternion.Euler(_rotationOffset);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            EnsureBodyCreated();

            if (_bodyInstance != null)
                _bodyInstance.SetActive(true);
        }
        else
        {
            if (_bodyInstance != null)
                _bodyInstance.SetActive(false);
        }
    }

    private void EnsureBodyCreated()
    {
        if (_bodyInstance != null)
            return;

        if (_bodyPrefab == null)
        {
            Debug.LogError("[LOCAL DRONE BODY] bodyPrefab is null");
            return;
        }

        _bodyInstance = Instantiate(
            _bodyPrefab,
            transform.position,
            transform.rotation
        );

        _bodyInstance.name = $"LocalDroneBody_{name}";

        RemoveLocalOnlyComponents(_bodyInstance);
    }

    private void RemoveLocalOnlyComponents(GameObject obj)
    {
        PlayerMovement movement = obj.GetComponentInChildren<PlayerMovement>(true);
        if (movement != null)
            Destroy(movement);

        PlayerItem item = obj.GetComponentInChildren<PlayerItem>(true);
        if (item != null)
            Destroy(item);

        PlayerHP hp = obj.GetComponentInChildren<PlayerHP>(true);
        if (hp != null)
            Destroy(hp);

        CharacterController cc = obj.GetComponentInChildren<CharacterController>(true);
        if (cc != null)
            Destroy(cc);

        Camera cam = obj.GetComponentInChildren<Camera>(true);
        if (cam != null)
            Destroy(cam.gameObject);

        AudioListener listener = obj.GetComponentInChildren<AudioListener>(true);
        if (listener != null)
            Destroy(listener);
    }
}
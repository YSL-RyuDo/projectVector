using UnityEngine;

public class WeaponADS : MonoBehaviour
{
    [System.Serializable]
    public struct RoleHipTransform
    {
        public Vector3 position;
        public Vector3 rotation;
    }

    [Header("Default Hip (Fallback)")]
    [SerializeField] private Vector3 defaultHipPosition = new Vector3(0.4f, -0.15f, 0.4f);
    [SerializeField] private Vector3 defaultHipRotation = Vector3.zero;

    [Header("Role Transforms")]
    [SerializeField] private RoleHipTransform initiatorHip = new RoleHipTransform { position = new Vector3(0.4f, -0.15f, 0.4f), rotation = new Vector3(-90f, 90f, 0f) };
    [SerializeField] private RoleHipTransform controllerHip = new RoleHipTransform { position = new Vector3(0.35f, -0.15f, 0.4f), rotation = Vector3.zero };
    [SerializeField] private RoleHipTransform entryHip = new RoleHipTransform { position = new Vector3(0.45f, -0.12f, 0.38f), rotation = Vector3.zero };
    [SerializeField] private RoleHipTransform sentinelHip = new RoleHipTransform { position = new Vector3(0.38f, -0.18f, 0.42f), rotation = Vector3.zero };

    [Header("Hip Movement Speed")]
    [SerializeField] private float hipMoveSpeed = 15f;

    private Vector3 activeHipPosition;
    private Quaternion activeHipRotation;
    private Animator rootAnimator;

    private void Awake()
    {
        rootAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        UpdateHipTransformByCharacter();
    }

    private void OnEnable()
    {
        UpdateHipTransformByCharacter();
    }

    public void UpdateHipTransformByCharacter()
    {
        CharacterType characterType = GetComponentInParent<CharacterType>();

        AgentRole role = characterType != null ? characterType.Role : AgentRole.Default;

        switch (role)
        {
            case AgentRole.Initiator:
                SetHipTransform(initiatorHip);
                break;
            case AgentRole.Controller:
                SetHipTransform(controllerHip);
                break;
            case AgentRole.Entry:
                SetHipTransform(entryHip);
                break;
            case AgentRole.Sentinel:
                SetHipTransform(sentinelHip);
                break;
            default:
                activeHipPosition = defaultHipPosition;
                activeHipRotation = Quaternion.Euler(defaultHipRotation);
                break;
        }
    }

    private void SetHipTransform(RoleHipTransform roleTransform)
    {
        activeHipPosition = roleTransform.position;
        activeHipRotation = Quaternion.Euler(roleTransform.rotation);
    }

    private void LateUpdate()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            activeHipPosition,
            hipMoveSpeed * Time.deltaTime
        );

        if (rootAnimator == null || !rootAnimator.enabled)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                activeHipRotation,
                hipMoveSpeed * Time.deltaTime
            );
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

public class FirstPersonViewSetup : MonoBehaviour
{
    [Header("1인칭 시점에서도 계속 보여줄 파츠")]
    [SerializeField] private List<Renderer> armParts = new List<Renderer>();

    [Header("1인칭 시점에서 숨길 파츠")]
    [SerializeField] private List<Renderer> bodyParts = new List<Renderer>();

    [SerializeField] private Camera playerCamera;

    private const string LAYER_PLAYER_BODY = "PlayerBody";
    private const string LAYER_FP_ARMS = "FirstPersonArms";

    public void Setup(bool isMine)
    {
        int bodyLayer = LayerMask.NameToLayer(LAYER_PLAYER_BODY);
        int armsLayer = LayerMask.NameToLayer(LAYER_FP_ARMS);

        foreach (var r in bodyParts)
        {
            if (r != null) r.gameObject.layer = bodyLayer;
        }

        foreach (var r in armParts)
        {
            if (r != null) r.gameObject.layer = armsLayer;
        }

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null && isMine)
        {
            // 내 카메라에서만 몸통 레이어 제외
            playerCamera.cullingMask &= ~(1 << bodyLayer);
        }
    }
}
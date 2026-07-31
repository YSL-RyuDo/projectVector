using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraLook : MonoBehaviour
{
    [Header("Mouse")]
    // ê¸°ë³¸ ë§ˆìš°ìŠ¤ ê°ë„
    [SerializeField] private float _baseSensitivity = 0.2f;
    // ADS(ì¡°ì¤€) ìƒíƒœì¼ ë•Œ ê°ë„ ë°°ìœ¨ (0.5ë©´ ì ˆë°˜ìœ¼ë¡œ ê°ì†Œ)
    [SerializeField] private float _adsSensitivityMultiplier = 0.5f;

    [Header("Recoil")]
    public float recoilReturnSpeed = 10f;

<<<<<<< Updated upstream
    private float xRotation = 0f;   // Camera Pitch
    private float yaw = 0f;         // Player Yaw
    private float recoilOffset = 0f;
    private bool _isADS = false;
=======
    [Header("Test Settings")]
    [SerializeField] private bool _isTestMode = false;
    //Æ©Åä¸®¾ó ÀÓÀÇ Ãß°¡ºÎºĞ^^^^^^^^^^^^^

    private PlayerIdentity _playerIdentity;
>>>>>>> Stashed changes

    Transform player;

    public bool CanMove = true;

    void Start()
    {
<<<<<<< Updated upstream
        player = transform.parent;
=======

        _playerIdentity =
            GetComponentInParent<PlayerIdentity>();

        if (_isTestMode)
        {
            Transform root = transform.parent != null ? transform.parent : transform;
            SetLocalPlayer(true, root);
            return;
        }

        enabled = false;

        /*_playerIdentity =
            GetComponentInParent<PlayerIdentity>();

        enabled = false;
        */
    }

    public void SetLocalPlayer(
        bool isLocal,
        Transform root)
    {
        isLocalPlayer = isLocal;
        playerRoot = root;

        CanMove = isLocal;
        enabled = isLocal;

        if (!isLocal)
            return;

        Cursor.lockState =
            CursorLockMode.Locked;
>>>>>>> Stashed changes

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        MiniMapUI ui = FindFirstObjectByType<MiniMapUI>();

        if (ui != null)
        {
            ui.RegisterLocalPlayerCamera(this);
            ui.RegisterPlayer(this.gameObject);
        }
    }

    void Update()
    {
<<<<<<< Updated upstream
        if (!CanMove) return;
=======

        if (_isTestMode && !TESTInitiatorSkill_Q.IsLocalDroneMode)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null && !cam.enabled)
                cam.enabled = true;

            var listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null && !listener.enabled)
                listener.enabled = true;
        }

        if (!isLocalPlayer)
            return;
        //Æ©Åä¸®¾ó¿ë

        if (!isLocalPlayer)
            return;
>>>>>>> Stashed changes

        // ADS(ì¡°ì¤€) ìƒíƒœë©´ ê°ë„ ë‚®ì¶¤
        float sensitivity = _baseSensitivity * (_isADS ? _adsSensitivityMultiplier : 1f);
        
        // ë§ˆìš°ìŠ¤ ì´ë™ê°’ ì½ê¸°
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        yaw += mouseX;
        player.rotation = Quaternion.Euler(0f, yaw, 0f);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        recoilOffset = Mathf.Lerp(
            recoilOffset,
            0f,
            recoilReturnSpeed * Time.deltaTime
        );

        transform.localRotation =
            Quaternion.Euler(xRotation + recoilOffset, 0f, 0f);
    }
    private void LateUpdate()
    {
        //(0,0,0)À¸·Î ¹Ù²Û Ä«¸Ş¶ó À§Ä¡¸¦ ¸Ó¸® ¾ÕÀ¸·Î »ìÂ¦ º¸Á¤
        if (_isTestMode)
        {
            var camTransform = GetComponentInChildren<Camera>()?.transform;
            if (camTransform != null)
            {
                camTransform.localPosition = new Vector3(0f, 0f, 0.25f);
            }
        }
    }


    public float GetYaw()
    {
        return yaw;
    }

    public void AddRecoil(float amount)
    {
        recoilOffset -= amount;
    }

    public void SetADS(bool ads)
    {
        _isADS = ads;
    }

    public void AddPitch(float amount)
    {
        xRotation += amount;
    }

    internal void AddYaw(float amount)
    {
        yaw += amount;
        //throw new NotImplementedException();
    }
}

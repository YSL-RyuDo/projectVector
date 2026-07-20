using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DroneHUDUI : MonoBehaviour
{
    public static DroneHUDUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Texts")]
    [SerializeField] private TMP_Text _taserStatusText;
    [SerializeField] private TMP_Text _droneHpText;
    [SerializeField] private TMP_Text _controlGuideText;

    [Header("Feedback")]
    [SerializeField] private GameObject _hitMarker;
    [SerializeField] private Image _fireFlashImage;

    [Header("Settings")]
    [SerializeField] private float _hitMarkerTime = 0.25f;
    [SerializeField] private float _flashFadeSpeed = 8f;

    private Coroutine _hitCoroutine;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        Instance = this;

        SetVisible(false);

        if (_hitMarker != null)
            _hitMarker.SetActive(false);

        if (_fireFlashImage != null)
        {
            Color c = _fireFlashImage.color;
            c.a = 0f;
            _fireFlashImage.color = c;
        }
    }

    public void SetVisible(bool visible)
    {
        if (_root != null)
            _root.SetActive(visible);

        if (visible)
        {
            SetTaserReady(true);
            SetDroneHp(100, 100);
            SetControlGuide();
        }
    }

    public void SetTaserReady(bool ready)
    {
        if (_taserStatusText == null)
            return;

        _taserStatusText.text = ready
            ? "TASER READY"
            : "TASER USED";
    }

    public void SetDroneHp(int hp, int maxHp)
    {
        if (_droneHpText == null)
            return;

        maxHp = Mathf.Max(1, maxHp);

        int percent = Mathf.RoundToInt((float)hp / maxHp * 100f);
        percent = Mathf.Clamp(percent, 0, 100);

        _droneHpText.text = $"DRONE {percent}%";
    }

    private void SetControlGuide()
    {
        if (_controlGuideText == null)
            return;

        _controlGuideText.text =
            "WASD MOVE  /  SPACE UP  /  CTRL DOWN  /  Q EXIT";
    }

    public void PlayTaserFireFeedback()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FireFlashRoutine());
    }

    public void PlayTaserHitConfirm()
    {
        if (_hitCoroutine != null)
            StopCoroutine(_hitCoroutine);

        _hitCoroutine = StartCoroutine(HitMarkerRoutine());
    }

    public void PlayTaserMiss()
    {
        // 빗나감은 일단 상태만 USED 처리.
        // 나중에 MISS 텍스트 넣고 싶으면 여기서 처리.
    }

    private IEnumerator HitMarkerRoutine()
    {
        if (_hitMarker != null)
            _hitMarker.SetActive(true);

        yield return new WaitForSeconds(_hitMarkerTime);

        if (_hitMarker != null)
            _hitMarker.SetActive(false);
    }

    private IEnumerator FireFlashRoutine()
    {
        if (_fireFlashImage == null)
            yield break;

        Color c = _fireFlashImage.color;
        c.a = 0.45f;
        _fireFlashImage.color = c;

        while (c.a > 0.01f)
        {
            c.a = Mathf.Lerp(c.a, 0f, _flashFadeSpeed * Time.deltaTime);
            _fireFlashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        _fireFlashImage.color = c;
    }

    public void SetSpectatorDroneMode(bool active, uint ownerPlayerId = 0)
    {
        SetVisible(active);

        if (!active)
            return;

        if (_taserStatusText != null)
            _taserStatusText.text = "SPECTATING DRONE";

        if (_droneHpText != null)
            _droneHpText.text = "DRONE VIEW";

        if (_controlGuideText != null)
            _controlGuideText.text = "SPECTATOR MODE";

        if (_hitMarker != null)
            _hitMarker.SetActive(false);
    }
}
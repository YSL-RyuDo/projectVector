using TMPro;
using UnityEngine;
using UnityEngine.UI;

// All graphics are authored in the scene, never instantiated or destroyed in play.
public sealed class SmokeTargetingView : MonoBehaviour
{
    [SerializeField] private RectTransform _map, _marker;
    [SerializeField] private Graphic _ring;
    [SerializeField] private TMP_Text _status, _distance;
    [SerializeField] private GameObject _presentation;
    public RectTransform Map => _map;
    public void ResetSelection()
    {
        if (_presentation) _presentation.SetActive(ControllerSkill_E.IsSelectingSmoke);
        _marker.gameObject.SetActive(false);
        _status.text = "투하 위치를\n선택하세요";
        _distance.text = "좌클릭  위치 선택";
    }
    public void Select(Vector2 point, Vector2 footprint, float distance)
        => ShowFootprint(point, footprint, distance, true);
    public void Preview(Vector2 point, Vector2 footprint, float distance)
        => ShowFootprint(point, footprint, distance, false);
    private void ShowFootprint(Vector2 point, Vector2 footprint, float distance, bool selected)
    {
        if (_presentation) _presentation.SetActive(true);
        _marker.gameObject.SetActive(true);
        _marker.anchoredPosition = point;
        _marker.sizeDelta = footprint;
        _ring.color = selected ? new Color(.27f, .72f, .92f, .95f) : new Color(.92f, .97f, 1, .95f);
        _status.text = selected ? "선택 완료\n우클릭으로 투하" : "예상 연막 범위\n좌클릭으로 선택";
        _distance.text = $"거리 {distance:0} m\n연막 지름 9 m";
    }
    public void Invalid()
    {
        _status.text = "설치할 수 없는\n위치입니다";
        _distance.text = "다른 위치를 선택하세요";
        _marker.gameObject.SetActive(false);
    }
    private void OnEnable() { ResetSelection(); if (_presentation) _presentation.SetActive(ControllerSkill_E.IsSelectingSmoke); }
}

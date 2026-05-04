using UnityEngine;

public class ColorController : MonoBehaviour
{
    [SerializeField] private Material _yellow; // 평상시 혹은 특정 상태
    [SerializeField] private Material _purple; // 둔화 상태 (보라색)

    private Renderer _renderer;
    private Material _originalMaterial;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _originalMaterial = _renderer.material; // 원래 색상 기억
        }
    }

    // 외부에서 색상 이름을 전달받아 변경하는 함수
    public void GetColor(string colorName)
    {
        if (_renderer == null) return;

        switch (colorName.ToLower())
        {
            case "yellow":
                _renderer.material = _yellow;
                break;
            case "purple":
                _renderer.material = _purple;
                break;
            case "original":
                _renderer.material = _originalMaterial;
                break;
            default:
                break;
        }
    }
}
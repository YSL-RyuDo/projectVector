using UnityEngine;
using UnityEngine.UI;

public class DrawingMachine : MonoBehaviour
{
    [SerializeField] private Button _draw1;
    [SerializeField] private Button _draw2;

    private void Start()
    {
        _draw1.onClick.AddListener(draw1);
        _draw2.onClick.AddListener(draw2);
    }

    private void draw1()
    {

    }

    private void draw2()
    {

    }
}
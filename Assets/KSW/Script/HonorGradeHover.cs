using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HonorGradeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int grade;
    [SerializeField] private MyPageHonorController controller;
    public void Configure(int value, MyPageHonorController target) { grade = value; controller = target; }
    public void OnPointerEnter(PointerEventData eventData) => controller?.HoverGrade(grade);
    public void OnPointerExit(PointerEventData eventData) => controller?.LeaveGrade();
}

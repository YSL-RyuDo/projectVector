using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HonorGradeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int grade;
    [SerializeField] private MyPageHonorController controller;
    public void Configure(int value, MyPageHonorController target) { grade = value; controller = target; }
    // Older scenes were authored under an inactive MyPagePanel with an empty reference.
    // Resolve on input as well so those scenes work without recreating or saving the UI.
    private MyPageHonorController ResolveController()
    {
        if (controller == null) controller = GetComponentInParent<MyPageHonorController>(true);
        return controller;
    }

    public void OnPointerEnter(PointerEventData eventData) => ResolveController()?.HoverGrade(grade);
    public void OnPointerExit(PointerEventData eventData) => ResolveController()?.LeaveGrade();
}

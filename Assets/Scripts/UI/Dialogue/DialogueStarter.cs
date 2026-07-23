using UnityEngine;

/// <summary>
/// 대화 시작 테스트용 캐릭터 버튼이나 이벤트에서 호출
/// </summary>
public class DialogueStarter : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;

    [Header("시작할 대화 ID")]
    [SerializeField] private int startDialogueIevel = 0;

    // 버튼 OnClick에 연결하거나 아무 데서나 호출
    public void StartDialogue()
    {
        dialogueManager.StartFavorLevel(startDialogueIevel);
    }

    // 테스트용 스페이스 누르면 시작
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            StartDialogue();
    }
}
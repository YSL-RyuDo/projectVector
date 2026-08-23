using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialSection
    {
        Prologue,
        EP1,
        EP2,
        EP3,
        EP4,
        Transition,
        EP5,
        EP6,
        Epilogue
    }

    [Header("현재 튜토리얼 구간")]
    [SerializeField]
    private TutorialSection currentSection = TutorialSection.Prologue;
    [SerializeField] private ScenarioManager scenarioManager;

    [Header("EP1 에너미 관리")]
    [SerializeField] private int remainingEnemies;

    [Header("플레이어 전환")]
    [SerializeField] private GameObject Sentinal;
    [SerializeField] private GameObject iniciater;


    public void OnEnemyKilled()
    {
        remainingEnemies--;

        // 몬스터를 다 처치했으면 대사 출력
        if (remainingEnemies <= 0)
        {
            // 대사만 출력하고 싶을 때 (구간 변경 X)
            scenarioManager.PlaySection("EP1");

            // 만약 대사 출력과 동시에 다음 챕터로 넘어가고 싶다면 아래 주석 해제
            // CompleteCurrentSection();
        }
    }


    public TutorialSection CurrentSection => currentSection;

    private void Start()
    {
        Sentinal.SetActive(false);
        StartSection(currentSection);
    }

    public void StartSection(TutorialSection section)
    {
        currentSection = section;

        Debug.Log($"튜토리얼 시작: {currentSection}");

        switch (currentSection)
        {
            case TutorialSection.Prologue:
                StartPrologue();
                break;

            case TutorialSection.EP1:
                StartEP1();
                break;

            case TutorialSection.EP2:
                StartEP2();
                break;

            case TutorialSection.EP3:
                StartEP3();
                break;

            case TutorialSection.EP4:
                StartEP4();
                break;

            case TutorialSection.Transition:
                StartTransition();
                break;

            case TutorialSection.EP5:
                StartEP5();
                break;

            case TutorialSection.EP6:
                StartEP6();
                break;

            case TutorialSection.Epilogue:
                StartEpilogue();
                break;
        }
    }

    public void CompleteCurrentSection()
    {
        int nextSectionIndex = (int)currentSection + 1;

        if (nextSectionIndex >= System.Enum.GetValues(typeof(TutorialSection)).Length)
        {
            Debug.Log("튜토리얼 전체 완료");
            return;
        }

        StartSection((TutorialSection)nextSectionIndex);
    }

    private void StartPrologue()
    {
        scenarioManager.PlaySection("Prologue");
        Debug.Log("프롤로그: 비인가 접속");
    }

    private void StartEP1()
    {
        scenarioManager.PlaySection("EP1");
        Debug.Log("EP1: 접속 지점");
    }

    private void StartEP2()
    {
        scenarioManager.PlaySection("EP2");
        Debug.Log("EP2: 방어 프로세스");
    }

    private void StartEP3()
    {
        scenarioManager.PlaySection("EP3");
        Debug.Log("EP3: 보이지 않는 적");
    }

    private void StartEP4()
    {
        scenarioManager.PlaySection("EP4");
        Debug.Log("EP4: 중앙 코어");
    }

    private void StartTransition()
    {
        Debug.Log("전환 컷신: 침입 경보");
    }

    private void StartEP5()
    {
        iniciater.SetActive(false);
        Sentinal.SetActive(true);

        scenarioManager.PlaySection("EP5");

        Debug.Log("EP5: 관리자 개입");
    }

    private void StartEP6()
    {
        Debug.Log("EP6: 연결 해제");
    }

    private void StartEpilogue()
    {
        Debug.Log("에필로그: 두 개의 시선");
    }
}

using System.Collections;
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

    [Header("에너미 관리")]
    [SerializeField] private int remainingEnemies;

    [Header("플레이어 전환")]
    [SerializeField] private GameObject Sentinal;
    [SerializeField] private GameObject iniciater;

    [Header("EP2")]
    [SerializeField] private GameObject ep2Enemy;

    [Header("EP3")]
    [SerializeField] private int ep3EnemyCount = 3;
    [SerializeField] private GameObject ep3Wall;

    [Header("EP4")]
    [SerializeField] private GameObject plantZone;

    [Header("프롤로그 UI")]
    [SerializeField] private GameObject tutorialUI;

    public TutorialSection CurrentSection => currentSection;

    private void Start()
    {
        Sentinal.SetActive(false);
        ep2Enemy.SetActive(false);

        StartSection(currentSection);
    }

    public void OnEnemyKilled()
    {
        remainingEnemies--;

        if (remainingEnemies > 0)
            return;

        if (currentSection == TutorialSection.EP1)
        {
            scenarioManager.PlaySection("EP1");
        }
        else if (currentSection == TutorialSection.EP3)
        {
            if (ep3Wall != null)
                ep3Wall.SetActive(false);
        }
        else if (currentSection == TutorialSection.EP4)
        {
            plantZone.SetActive(true);
        }
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

        if (nextSectionIndex >=
            System.Enum.GetValues(typeof(TutorialSection)).Length)
        {
            Debug.Log("튜토리얼 전체 완료");
            return;
        }

        StartSection((TutorialSection)nextSectionIndex);
    }

    private void StartPrologue()
    {
        StartCoroutine(DelayedPrologue());
    }

    private IEnumerator DelayedPrologue()
    {
        if (tutorialUI != null)
            tutorialUI.SetActive(false);

        yield return new WaitForSeconds(3f);

        if (tutorialUI != null)
            tutorialUI.SetActive(true);

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
        remainingEnemies = 1;

        scenarioManager.PlaySection("EP2");
        Debug.Log("EP2: 방어 프로세스");
    }

    private void StartEP3()
    {
        remainingEnemies = ep3EnemyCount;

        if (ep3Wall != null)
            ep3Wall.SetActive(true);

        scenarioManager.PlaySection("EP3");

        Debug.Log("EP3: 보이지 않는 적");
    }

    private void StartEP4()
    {
        remainingEnemies = 3;

        plantZone.SetActive(false);

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

    public void OnEP2DialogueEnd()
    {
        ep2Enemy.SetActive(true);
    }
}

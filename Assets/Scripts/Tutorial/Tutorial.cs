using UnityEngine;
using TMPro;

public class Playertutorial : MonoBehaviour
{
    public GameObject SteppointPrefab;
    public TextMeshProUGUI StepText;
    public TextAsset Tuto;

    public Transform[] stepPositions;

    int step = 0;
    string[] lines;
    GameObject currentStepPoint;

    void Start()
    {
        lines = Tuto.text.Replace("\r", "").Split('\n');

        StepText.text = lines[step].Trim();
        step++;

        SpawnStepPoint();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Steppoint"))
            return;

        Destroy(other.gameObject);

        if (step < lines.Length)
        {
            StepText.text = lines[step].Trim();
            step++;
        }

        SpawnStepPoint();
    }

    void SpawnStepPoint()
    {
        if (step - 1 >= stepPositions.Length)
            return;

        currentStepPoint = Instantiate(
            SteppointPrefab,
            stepPositions[step - 1].position,
            stepPositions[step - 1].rotation
        );
    }
}
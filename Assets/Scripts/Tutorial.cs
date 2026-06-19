using UnityEngine;
using TMPro;

public class Playertutorial : MonoBehaviour
{
    public GameObject SteppointPrefab;
    public TextMeshProUGUI StepText;
    public TextAsset Tuto;


    int step = 0;
    string[] lines;

    void Start()
    {
        lines = Tuto.text.Replace("\r", "").Split('\n');

        StepText.text = lines[step];
        step++;
    }


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Steppoint"))
            return;

        Vector3 newPos = other.transform.position + new Vector3(0f, 0f, -30f);
        Quaternion newRot = other.transform.rotation;

        Instantiate(SteppointPrefab, newPos, newRot);
        Destroy(other.gameObject);

        if (step < lines.Length)
        {
            StepText.text = lines[step].Trim();
            step++;
        }
    }
}

   
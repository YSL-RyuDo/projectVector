using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{

    public TutorialManager.TutorialSection targetSection;
    public TutorialManager tutorialManager;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
       
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;

            if (tutorialManager != null)
            {
                tutorialManager.StartSection(targetSection);
            }

            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

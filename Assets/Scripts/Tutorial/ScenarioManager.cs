using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScenarioManager : MonoBehaviour
{
    class DialogueData
    {
        public string section;
        public string speaker;
        public string dialogue;
        public string mode;
    }

    [SerializeField] TextAsset csv;

    [SerializeField] GameObject talkPanel;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text dialogText;
    [SerializeField] TMP_Text ingameDialog;

    [SerializeField] float ingameTime = 4f;

    List<DialogueData> dialogues = new();

    bool waitSpace;
    Coroutine playCoroutine;

    void Awake()
    {
        talkPanel.SetActive(false);
        ingameDialog.gameObject.SetActive(false);

        string[] lines = csv.text.Replace("\r", "").Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] value = lines[i].Split(',');

            dialogues.Add(new DialogueData
            {
                section = value[0],
                speaker = value[2],
                dialogue = value[3],
                mode = value[4]
            });
        }
    }

    void Update()
    {
        if (waitSpace &&
            Keyboard.current != null &&
            Keyboard.current.zKey.wasPressedThisFrame)
        {
            waitSpace = false;
        }
    }

    public void PlaySection(string section)
    {

        if (playCoroutine != null)
            StopCoroutine(playCoroutine);

        playCoroutine = StartCoroutine(Play(section));
    }

    IEnumerator Play(string section)
    {
        foreach (DialogueData data in dialogues)
        {
            if (data.section != section)
                continue;

            if (data.mode == "Cutscene")
                yield return Cutscene(data);
            else
                yield return Ingame(data);
        }

        playCoroutine = null;
    }

    IEnumerator Cutscene(DialogueData data)
    {
        talkPanel.SetActive(true);

        nameText.text = data.speaker;
        dialogText.text = data.dialogue;

        waitSpace = true;
        yield return new WaitUntil(() => !waitSpace);

        talkPanel.SetActive(false);
    }

    IEnumerator Ingame(DialogueData data)
    {
        ingameDialog.gameObject.SetActive(true);
        ingameDialog.text = data.dialogue;

        yield return new WaitForSeconds(ingameTime);

        ingameDialog.gameObject.SetActive(false);
    }
}
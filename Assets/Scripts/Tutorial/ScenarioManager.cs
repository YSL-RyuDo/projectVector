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
        public int order;
        public string speaker;
        public string dialogue;
        public string mode;
    }

    [SerializeField] private TextAsset csv;

    [SerializeField] private GameObject talkPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private TMP_Text ingameDialog;
    [SerializeField] private GameObject enemy;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TESTPlayerItem playerItem;
    [SerializeField] private WeaponData ep2WeaponData;

    [SerializeField] private float ingameTime = 4f;

    private List<DialogueData> dialogues = new();

    private bool waitSpace;
    private Coroutine playCoroutine;

    private void Awake()
    {
        if (csv == null)
            return;

        string text = csv.text.TrimStart('\uFEFF').Replace("\r", "");
        string[] lines = text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Trim();

            if (line.StartsWith("\"") && line.EndsWith("\""))
                line = line.Substring(1, line.Length - 2);

            string[] value = line.Split(',');

            if (value.Length < 5)
                continue;

            int.TryParse(
                value[1].Replace("\"", "").Trim(),
                out int parsedOrder
            );

            dialogues.Add(new DialogueData
            {
                section = value[0].Replace("\"", "").Trim(),
                order = parsedOrder,
                speaker = value[2].Replace("\"", "").Trim(),
                dialogue = value[3].Replace("\"", "").Trim(),
                mode = value[4].Replace("\"", "").Trim()
            });
        }

        ResetUI();
    }

    private void Start()
    {
        enemy.SetActive(false);
    }

    private void Update()
    {
        if (waitSpace &&
            Keyboard.current != null &&
            Keyboard.current.zKey.wasPressedThisFrame)
        {
            waitSpace = false;
        }
    }

    private void ResetUI()
    {
        if (talkPanel != null)
            talkPanel.SetActive(false);

        if (ingameDialog != null)
            ingameDialog.gameObject.SetActive(false);
    }

    public void PlaySection(string section)
    {

        if (section.Equals("EP2", System.StringComparison.OrdinalIgnoreCase))
        {
            playerItem.ApplyPickedWeaponLocal(
                ep2WeaponData,
                ep2WeaponData.magSize,
                ep2WeaponData.maxReserveAmmo
            );
        }

        PlaySectionFromTo(section, 0, int.MaxValue);
    }

    public void PlaySectionFromTo(string section, int startOrder, int endOrder)
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        waitSpace = false;
        ResetUI();

        playCoroutine = StartCoroutine(
            Play(section, startOrder, endOrder)
        );
    }

    private IEnumerator Play(string section, int startOrder, int endOrder)
    {
        foreach (DialogueData data in dialogues)
        {
            if (!data.section.Equals(
                    section.Trim(),
                    System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (data.order < startOrder || data.order > endOrder)
                continue;

            if (data.mode.Equals(
                    "Cutscene",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                yield return Cutscene(data);
            }
            else if (data.mode.Equals(
                         "Gameplay",
                         System.StringComparison.OrdinalIgnoreCase))
            {
                yield return Ingame(data);
            }
        }

        ResetUI();
        playCoroutine = null;

        if (section.Equals("EP2", System.StringComparison.OrdinalIgnoreCase))
        {
            enemy.SetActive(true);
        }
    }

    private IEnumerator Cutscene(DialogueData data)
    {
        if (talkPanel == null)
            yield break;

        talkPanel.SetActive(true);

        if (ingameDialog != null)
            ingameDialog.gameObject.SetActive(false);

        if (nameText != null)
            nameText.text = data.speaker;

        if (dialogText != null)
            dialogText.text = data.dialogue;

        yield return null;

        waitSpace = true;

        yield return new WaitUntil(() => !waitSpace);
    }

    private IEnumerator Ingame(DialogueData data)
    {
        if (talkPanel != null)
            talkPanel.SetActive(false);

        if (ingameDialog == null)
            yield break;

        ingameDialog.gameObject.SetActive(true);
        ingameDialog.text = data.dialogue;

        yield return new WaitForSeconds(ingameTime);
    }
}
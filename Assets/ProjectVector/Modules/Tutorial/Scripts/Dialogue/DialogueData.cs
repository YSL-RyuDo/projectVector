using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public List<DialogueEntry> entries = new List<DialogueEntry>();

    /// 특정 호감도 레벨의 모든 대화 ID 목록 (중복 제거, 정렬)
    public List<string> GetDialogueIds(int favorLevel)
    {
        var ids = new List<string>();
        foreach (var e in entries)
        {
            if (e.favorLevel == favorLevel && !ids.Contains(e.dialogueId))
                ids.Add(e.dialogueId);
        }
        ids.Sort();
        return ids;
    }

    /// 특정 DialogueID의 줄 목록 (sequence 순 정렬)
    public List<DialogueEntry> GetLines(string dialogueId)
    {
        var list = entries.FindAll(e => e.dialogueId == dialogueId);
        list.Sort((a, b) => a.sequence.CompareTo(b.sequence));
        return list;
    }

    /// 해당 DialogueID의 호감도 레벨
    public int GetFavorLevel(string dialogueId)
    {
        var e = entries.Find(x => x.dialogueId == dialogueId);
        return e != null ? e.favorLevel : 0;
    }
}

[Serializable]
public class DialogueEntry
{
    public int favorLevel;
    public string dialogueId;
    public int sequence;
    public string speaker;
    [TextArea] public string text;
    public DialogueChoice choice1;
    public DialogueChoice choice2;
    public DialogueChoice choice3;

    public bool HasChoices =>
        (choice1 != null && !string.IsNullOrEmpty(choice1.text)) ||
        (choice2 != null && !string.IsNullOrEmpty(choice2.text)) ||
        (choice3 != null && !string.IsNullOrEmpty(choice3.text));
}

[Serializable]
public class DialogueChoice
{
    public string text;
    public string nextDialogueId;
}
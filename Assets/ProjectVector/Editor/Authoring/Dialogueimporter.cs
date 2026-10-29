using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    private string csvPath = "Assets/Resources/Dialogue/dialogue.csv";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("─── CSV 임포트 ───", EditorStyles.boldLabel);

        csvPath = EditorGUILayout.TextField("CSV 경로", csvPath);

        GUILayout.Space(4);

        if (GUILayout.Button("CSV 임포트 실행", GUILayout.Height(30)))
        {
            ImportCSV((DialogueData)target, csvPath);
        }
    }

    private void ImportCSV(DialogueData asset, string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"[IMPORTER] CSV 파일 없음: {path}");
            EditorUtility.DisplayDialog("오류", $"CSV 파일을 찾을 수 없어요:\n{path}", "확인");
            return;
        }

        asset.entries.Clear();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        int imported = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] cols = SplitCSVLine(line);
            if (cols.Length < 5) continue;

            DialogueEntry entry = new DialogueEntry();

            int.TryParse(cols[0].Trim(), out entry.favorLevel);
            entry.dialogueId = cols[1].Trim();
            int.TryParse(cols[2].Trim(), out entry.sequence);
            entry.speaker = cols[3].Trim();
            entry.text = cols[4].Trim();

            if (cols.Length >= 7 && !string.IsNullOrEmpty(cols[5].Trim()))
                entry.choice1 = new DialogueChoice { text = cols[5].Trim(), nextDialogueId = cols[6].Trim() };

            if (cols.Length >= 9 && !string.IsNullOrEmpty(cols[7].Trim()))
                entry.choice2 = new DialogueChoice { text = cols[7].Trim(), nextDialogueId = cols[8].Trim() };

            if (cols.Length >= 11 && !string.IsNullOrEmpty(cols[9].Trim()))
                entry.choice3 = new DialogueChoice { text = cols[9].Trim(), nextDialogueId = cols[10].Trim() };

            asset.entries.Add(entry);
            imported++;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        Debug.Log($"[IMPORTER] 완료! {imported}개 줄 임포트됨");
        EditorUtility.DisplayDialog("완료", $"{imported}개 대화 줄을 임포트했어요!", "확인");
    }

    private string[] SplitCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuote = false;
        var sb = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ',' && !inQuote) { result.Add(sb.ToString()); sb.Clear(); continue; }
            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }
}
#endif
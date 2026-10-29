#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EquipmentIconCatalog))]
public sealed class EquipmentIconCatalogEditor : Editor
{
    static readonly string[] Names = { "P92", "P1911", "R1895", "UZI", "UMP", "S1897", "AA-12", "Dragunov", "M416", "AK47", "M249", "Kar98", "AWM", "Knife / 칼" };
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("상점, 인게임 무기 슬롯, 아군 전광판에서 공통으로 사용하는 아이콘입니다.", MessageType.Info);
        var weapons=serializedObject.FindProperty("weapons");
        weapons.arraySize=Names.Length;
        for (int i=0;i<Names.Length;i++) EditorGUILayout.PropertyField(weapons.GetArrayElementAtIndex(i),new GUIContent(Names[i]));
        var armor=serializedObject.FindProperty("armor");armor.arraySize=2;
        EditorGUILayout.PropertyField(armor.GetArrayElementAtIndex(0),new GUIContent("경량 방어막"));
        EditorGUILayout.PropertyField(armor.GetArrayElementAtIndex(1),new GUIContent("중량 방어막"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("device"),new GUIContent("Device / 디바이스"));
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

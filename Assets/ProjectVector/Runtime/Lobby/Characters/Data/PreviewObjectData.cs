using UnityEngine;

[CreateAssetMenu(fileName = "PreviewObject", menuName = "UI/Preview Object Data")]
public class PreviewObjectData : ScriptableObject
{
    [Header("Basic Info")]
    public int id;
    public string objectName;
    public PreviewObjectType objectType;

    [Header("3D Model Prefab")]
    public GameObject objectPrefab;

    [Header("UI Display Data")]
    public Sprite iconSprite;
    [TextArea] public string description;
}
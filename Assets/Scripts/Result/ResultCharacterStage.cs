using UnityEngine;

// Every model, camera and pose is saved in the scene. No runtime UI/model creation.
public sealed class ResultCharacterStage : MonoBehaviour
{
    public Camera stageCamera;
    public Transform closePose, settledPose;
    public GameObject[] models;
    public Animator[] animators;
    public GameObject[] maps;
    public int SelectedMap { get; private set; }
    public void SelectMap(string mapName)
    {
        SelectedMap = string.Equals(mapName, "Construction", System.StringComparison.OrdinalIgnoreCase) ? 2 : string.Equals(mapName, "Desert", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        for (int i = 0; i < maps.Length; i++) maps[i].SetActive(i == SelectedMap);
    }
    public int SelectedCharacter { get; private set; } = -1;
    public void Select(int characterId, bool celebrate)
    {
        SelectedCharacter = characterId >= 0 && characterId < models.Length ? characterId : -1;
        for (int i = 0; i < models.Length; ++i)
        {
            models[i].SetActive(i == SelectedCharacter);
            if (i != SelectedCharacter || animators[i] == null) continue;
            var animator = animators[i];
            animator.Rebind(); animator.Update(0);
            animator.Play(celebrate ? "Celebrate" : "Idle", 0, 0);
            animator.Update(0);
        }
        SetFraming(0);
    }
    public void SetFraming(float progress)
    {
        float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress));
        stageCamera.transform.position = Vector3.Lerp(closePose.position, settledPose.position, t);
        stageCamera.transform.rotation = Quaternion.Slerp(closePose.rotation, settledPose.rotation, t);
        stageCamera.fieldOfView = Mathf.Lerp(34, 38, t);
    }
    public void HideCharacters()
    {
        foreach (var model in models) model.SetActive(false);
    }
}

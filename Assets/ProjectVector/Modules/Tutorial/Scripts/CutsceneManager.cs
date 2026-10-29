using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

public class CutsceneManager : MonoBehaviour
{
    [System.Serializable]
    public class Cutscene
    {
        public string cutsceneName;
        public PlayableAsset timeline;
    }

    [Header("Cutscenes")]
    [SerializeField] private List<Cutscene> cutscenes = new();

    [Header("References")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private FPSCameraLook fpsCameraLook;

    private bool playing;
    private bool previousCanMove;

    private Transform cameraTransform;
    private Transform lookTransform;

    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private Quaternion savedLookRotation;

    private void Awake()
    {
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        director.stopped += OnCutsceneStopped;

        cameraTransform = brain.transform;
        lookTransform = fpsCameraLook.transform;
    }

    private void Start()
    {
        PlayCutscene("Intro");
    }

    public void PlayCutscene(string cutsceneName)
    {
        if (playing)
            return;

        foreach (Cutscene cutscene in cutscenes)
        {
            if (cutscene.cutsceneName != cutsceneName)
                continue;

            playing = true;

            // 컷신 시작 전 원래 FPS 카메라 상태 저장
            savedCameraPosition = cameraTransform.localPosition;
            savedCameraRotation = cameraTransform.localRotation;
            savedLookRotation = lookTransform.localRotation;

            previousCanMove = fpsCameraLook.CanMove;
            fpsCameraLook.CanMove = false;

            brain.enabled = true;

            director.playableAsset = cutscene.timeline;
            director.time = 0;
            director.Play();

            return;
        }
    }

    private void OnCutsceneStopped(PlayableDirector stoppedDirector)
    {
        if (!playing)
            return;

        playing = false;

        // Cinemachine 제어 중단
        brain.enabled = false;

        // 기존 FPS 카메라 상태 복원
        cameraTransform.localPosition = savedCameraPosition;
        cameraTransform.localRotation = savedCameraRotation;

        // FPSCameraLook이 붙은 오브젝트 회전 복원
        lookTransform.localRotation = savedLookRotation;

        // 카메라와 시점 제어가 다른 오브젝트인 경우
        // 카메라의 기울기를 원래 값으로 복원
        if (cameraTransform != lookTransform)
        {
            cameraTransform.localRotation = savedCameraRotation;
        }

        fpsCameraLook.CanMove = previousCanMove;
    }

    private void OnDestroy()
    {
        if (director != null)
            director.stopped -= OnCutsceneStopped;
    }
}
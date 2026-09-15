using System;
using UnityEngine;

namespace Game.Pings
{
    [DisallowMultipleComponent]
    public sealed class PingCameraProvider : MonoBehaviour
    {
        [Tooltip("로컬 플레이어의 실제 렌더링 Camera. Cinemachine 가상 카메라가 아닙니다.")]
        [SerializeField] private Camera aimCamera;
        [Tooltip("싱글 플레이 테스트용. 멀티에서는 끄고 BindCamera로 명시적으로 연결하세요.")]
        [SerializeField] private bool allowMainCameraFallback;

        public event Action<Camera> CameraChanged;
        private Camera lastResolved;

        public Camera Current
        {
            get
            {
                if (aimCamera != null && aimCamera.isActiveAndEnabled) return aimCamera;
                if (!allowMainCameraFallback) return null;
                Camera candidate = Camera.main;
                return candidate != null && candidate.isActiveAndEnabled ? candidate : null;
            }
        }

        // 스폰/리스폰/관전/카메라 교체 시 로컬 카메라 관리자가 호출합니다.
        // Cinemachine에서 같은 출력 Camera의 가상 카메라만 전환하면 재연결 불필요.
        public void BindCamera(Camera camera)
        {
            aimCamera = camera;
            // 프로젝트 어댑터의 null 바인딩은 확실한 해제입니다. 다른 MainCamera로 넘어가지 않습니다.
            allowMainCameraFallback = false;
            PublishChange();
        }

        private void Update() => PublishChange();

        private void PublishChange()
        {
            Camera current = Current;
            if (ReferenceEquals(current, lastResolved)) return;
            lastResolved = current;
            CameraChanged?.Invoke(current);
        }
    }
}

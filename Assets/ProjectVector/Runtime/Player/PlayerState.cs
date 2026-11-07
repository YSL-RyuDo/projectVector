using TMPro;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [SerializeField] private int MaxHP = 100;
    public int CurrentHP { get; private set; }

    public bool IsDead => CurrentHP <= 0;

    public enum BattleState { Attack, Defence }
    public BattleState state;

    // 플레이어 초기화를 위한 컴포넌트 참조
    private PlayerItem playerItem;
    private CharacterController characterController;

    private void Awake()
    {
        CurrentHP = MaxHP;

        playerItem = GetComponent<PlayerItem>();
        characterController = GetComponent<CharacterController>();
    }

    public void SetHP(int hp)
    {
        CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
    }

    public void ResetHP()
    {
        CurrentHP = MaxHP;
    }

    public void Relocate(Vector3 spawnPosition)
    {
        // CharacterController나 Rigidbody를 쓴다면 이동 전에 잠시 꺼주는 것이 안전
        characterController.enabled = false; // 콜라이더 잠시 끄기
        transform.position = spawnPosition; // 위치 이동
        characterController.enabled = true;  // 다시 켜기

        ResetHP();
        playerItem?.ResetAllAmmo();
    }

    // 죽은 플레이어: 위치 이동 + 정보(장비, 체력) 초기화 + 부활
    public void ReviveAndResetInfo(Vector3 spawnPosition)
    {
        //IsDead = false;
        ResetHP();

        gameObject.SetActive(true); // 죽었을 때 SetActive(false) 했다면 다시 켜기
        characterController.enabled = false; // 콜라이더 잠시 끄기
        transform.position = spawnPosition; // 위치 이동
        characterController.enabled = true;  // 다시 켜기

        // 장비 초기화 로직 (예: 가지고 있던 무기 다 버리고 권총만 지급)
        // playerItem.ResetToDefault(); // PlayerItem 스크립트에 이 함수를 만들어야 합니다.

        playerItem?.ResetToDefault();

        Debug.Log($"{gameObject.name} 부활 및 정보 초기화 완료");
    }

    public void Die()
    {
        //IsDead = true;
        SetHP(0);
        // 스파이크를 들고 있었다면 드랍하는 로직 등 호출
        // gameObject.SetActive(false); // 모델 숨기기
    }

    public void SetBattleState(BattleState state)
    {
        this.state = state;
    }
}
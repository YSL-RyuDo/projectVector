using UnityEngine;
using System.Collections.Generic; // 리스트 사용을 위해 추가

public class SwampArea : MonoBehaviour
{
    [SerializeField] private float _slowMultiplier = 0.2f;

    private List<Slow> _affectedObjects = new List<Slow>();

    private void OnTriggerEnter(Collider other)
    {
        var slow = other.GetComponent<Slow>();
        if (slow != null)
        {
            slow.ApplySlow(_slowMultiplier);
            _affectedObjects.Add(slow); // 리스트에 추가
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var slow = other.GetComponent<Slow>();
        if (slow != null)
        {
            slow.RemoveSlow();
            _affectedObjects.Remove(slow); // 리스트에서 제거
        }
    }

    private void OnDestroy()
    {
        foreach (var slow in _affectedObjects)
        {
            if (slow != null)
            {
                slow.RemoveSlow(); // 남아있는 몬스터들 강제 복구
            }
        }
        _affectedObjects.Clear();
    }
}
using UnityEngine;

// Apply after firearm/knife/skill/device poses; restoration happens before them.
[DefaultExecutionOrder(2400)]
public sealed class FirstPersonEquipmentPose : MonoBehaviour
{
    FirstPersonEquipmentTransition transition;
    void Awake()=>transition=GetComponent<FirstPersonEquipmentTransition>();
    void LateUpdate()=>transition?.ApplyViewOffset();
}

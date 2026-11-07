using UnityEngine;

// Authored only on inaccessible map surfaces. A top-down utility pick must stop
// here, not pass through a sealed building and select the floor beneath it.
[DisallowMultipleComponent]
public sealed class MapUtilityPlacementBlocker : MonoBehaviour
{
}

using System;
using UnityEngine;

// All locations/volumes are authored in WeaponTest before Play.
[Serializable]
public sealed class PracticeTrainingZone
{
    public string displayName;
    public int defaultMode;
    public Transform station;
    public BoxCollider walkInVolume;
    public Transform[] distanceTargets;
    public Transform[] spawnPoints;
    public Transform console;
    public float movementWidth = 3;
    public bool Supports(int mode) => defaultMode == mode || (defaultMode == 2 && mode == 3);
}

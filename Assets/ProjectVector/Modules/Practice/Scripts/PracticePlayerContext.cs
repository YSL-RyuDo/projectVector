using UnityEngine;

// Marker on the saved Game-scene character prefab instance only. Normal matches have no marker.
public sealed class PracticePlayerContext : MonoBehaviour
{
    public PracticeRangeController range;
    public static PracticePlayerContext For(Component component) => component != null ? component.GetComponentInParent<PracticePlayerContext>() : null;
    private void Awake()
    {
        var identity=GetComponent<PlayerIdentity>();
        identity.PlayerId=uint.MaxValue;identity.Team=255;identity.UserId="practice";
        gameObject.tag="Player";
        // This shooting range has no objective zones or match SpikeSlider. Preserve the prefab component.
        var spike=GetComponent<PlayerSpike>();if(spike!=null){spike.hasSpike=false;spike.enabled=false;}
        foreach(var skill in GetComponentsInChildren<CharacterSkill>(true)) skill.enabled=false;
    }
    private void Start()
    {
        GetComponent<PlayerMovement>().SetLocalPlayer(true);
        GetComponent<PlayerCameraController>().SetLocalPlayer(true);
    }
}

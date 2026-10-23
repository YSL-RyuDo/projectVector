using UnityEngine;

// Saved world-space rings; data-driven scaling/animation, no runtime UI layout.
public sealed class SentinelFieldVfx : MonoBehaviour
{
    [SerializeField] private Transform boundary, inner, spokes;
    [SerializeField] private AudioSource hum;
    private float began, duration=5, radius=7;
    public void Begin(float size,float seconds){radius=size;duration=seconds;began=Time.time;gameObject.SetActive(true);if(hum)hum.Play();Apply(0);}
    private void Update(){Apply(Time.time-began);}
    private void Apply(float elapsed){
        if(boundary)boundary.localScale=Vector3.one*radius;
        if(inner){inner.localScale=Vector3.one*radius*(.3f+Mathf.Repeat(elapsed*.6f,.7f));inner.localRotation=Quaternion.Euler(0,elapsed*24,0);}
        if(spokes){spokes.localScale=Vector3.one*radius;spokes.localRotation=Quaternion.Euler(0,-elapsed*9,0);}
    }
}

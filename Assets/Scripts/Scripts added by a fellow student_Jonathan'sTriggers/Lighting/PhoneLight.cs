using UnityEngine;

public class SmoothLightFlicker : MonoBehaviour
{
    [SerializeField] public Light lightSource;
    public float minIntensity = 0f;
    public float maxIntensity = 1.5f;
    
    [Tooltip("Higher values make it flicker faster")]
    public float speed = 1.0f;

    void Start()
    {
        if (lightSource == null) lightSource = GetComponent<Light>();
    }

   void Update()
{
    // 1. Get raw noise (typically stays between ~0.3 and ~0.7)
    float rawNoise = Mathf.PerlinNoise(Time.time * speed, 0);

    // 2. STRETCH: Treat 0.3 as the new '0' and 0.7 as the new '1'.
    // This forces the value to hit the absolute 0 and 1 boundaries more often.
    float stretchedNoise = Mathf.InverseLerp(0.3f, 0.7f, rawNoise);

    // 3. Apply to your 0 to 1.5 intensity range
    lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, stretchedNoise);
}
}

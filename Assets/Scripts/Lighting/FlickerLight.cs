using UnityEngine;

public class LightFlicker : MonoBehaviour {
    public Light lightSource;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 0.1f;

    void Start() {
        if (lightSource == null) lightSource = GetComponent<Light>();
        InvokeRepeating("Flicker", 0, flickerSpeed);
    }

    void Flicker() {
        lightSource.intensity = Random.Range(minIntensity, maxIntensity);
    }
}
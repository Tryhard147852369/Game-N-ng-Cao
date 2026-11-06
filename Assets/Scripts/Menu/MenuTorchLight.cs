using UnityEngine;

[RequireComponent(typeof(Light))]
public sealed class MenuTorchLight : MonoBehaviour
{
    private Light torch;
    private float brightness;

    private void Awake()
    {
        torch = GetComponent<Light>();
        brightness = torch.intensity;
    }

    // Dao động nhẹ để ánh đuốc tự nhiên hơn.
    private void Update()
    {
        float noise = Mathf.PerlinNoise(transform.position.x, Time.unscaledTime * 3);
        torch.intensity = brightness * Mathf.Lerp(.88f, 1.12f, noise);
    }
}

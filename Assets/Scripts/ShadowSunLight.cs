using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowSunLight : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private float maxIntensity = 0.5f;
    [SerializeField] private float orbitRadius = 20f;
    [Range(0f, 1f)]
    [SerializeField] private float horizontalFactor = 0.35f;

    private Light2D _light;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        if (dayNightCycle == null)
            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
    }

    private void Update()
    {
        if (_light == null || dayNightCycle == null) return;

        float t = dayNightCycle.cycleTimer / dayNightCycle.cycleDuration;
        float angle = t * 2f * Mathf.PI;
        float sinAngle = Mathf.Sin(angle);

        if (sinAngle <= 0f)
        {
            _light.enabled = false;
            return;
        }

        _light.enabled = true;

        float cosAngle = Mathf.Cos(angle);
        Vector3 camPos = Camera.main.transform.position;
        transform.position = new Vector3(
            camPos.x - cosAngle * orbitRadius * horizontalFactor,
            camPos.y + sinAngle * orbitRadius,
            0f
        );
        _light.intensity = sinAngle * maxIntensity;
    }
}

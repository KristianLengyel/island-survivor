using UnityEngine;

public class FakeShadow2DLight : MonoBehaviour
{
    [SerializeField] private bool syncWithDayNight = true;
    [SerializeField] private DayNightCycle dayNightCycle;

    [Header("Day-Night Sync")]
    [SerializeField] private float baseShadowLength = 1.8f;
    [SerializeField] private float maxShadowLength = 4.5f;
    [SerializeField] private bool scaleLengthWithAngle = true;
    [Range(0f, 1f)] [SerializeField] private float maxShadowAlpha = 0.5f;

    [Header("Manual Override (syncWithDayNight = false)")]
    [Tooltip("Compass direction the sun is coming FROM. 0=N 90=E 180=S 270=W. Shadow points opposite.")]
    [Range(0f, 360f)] [SerializeField] private float manualSunCompass = 180f;
    [SerializeField] [Min(0f)] private float manualShadowLength = 1.5f;
    [Range(0f, 1f)] [SerializeField] private float manualShadowAlpha = 0.45f;

    [Header("Appearance")]
    [Range(0f, 1f)] public float shadowSquish = 0.15f;

    public Vector2 ShadowDirection { get; private set; }
    public float ShadowLength { get; private set; }
    public float ShadowAlpha { get; private set; }
    public bool IsVisible { get; private set; }

    private void Awake()
    {
        if (syncWithDayNight && dayNightCycle == null)
            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
    }

    private void LateUpdate()
    {
        if (syncWithDayNight && dayNightCycle != null)
        {
            float t = dayNightCycle.cycleTimer / dayNightCycle.cycleDuration;
            float sinAngle = Mathf.Sin(t * 2f * Mathf.PI);

            if (sinAngle <= 0f)
            {
                IsVisible = false;
                return;
            }

            IsVisible = true;

            // Sun arc (northern hemisphere, top-down):
            //   t=0.00 (dawn)  → sun in East  (90°)  → shadow points West  (270°)
            //   t=0.25 (noon)  → sun in South (180°) → shadow points North (0°/360°) = UP
            //   t=0.50 (dusk)  → sun in West  (270°) → shadow points East  (90°)
            // Sun compass = 90 + t * 360  (sweeps 90°→180°→270° during daytime half)
            // Shadow compass = sun + 180  = 270 + t * 360
            float shadowCompassDeg = 270f + t * 360f;
            float shadowRad = shadowCompassDeg * Mathf.Deg2Rad;
            ShadowDirection = new Vector2(Mathf.Sin(shadowRad), Mathf.Cos(shadowRad));

            ShadowAlpha = sinAngle * maxShadowAlpha;
            ShadowLength = scaleLengthWithAngle
                ? Mathf.Min(baseShadowLength / sinAngle, maxShadowLength)
                : baseShadowLength;
        }
        else
        {
            IsVisible = true;
            float shadowRad = (manualSunCompass + 180f) * Mathf.Deg2Rad;
            ShadowDirection = new Vector2(Mathf.Sin(shadowRad), Mathf.Cos(shadowRad));
            ShadowLength = manualShadowLength;
            ShadowAlpha = manualShadowAlpha;
        }
    }
}

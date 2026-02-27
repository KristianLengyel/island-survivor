using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
	[Header("References")]
	public Light2D globalLight;
	public TMP_Text clockText;
	public TMP_Text dayCountText;
	public WeatherManager weatherManager;
	public ThunderstormController thunderstormController;

	[Header("Settings")]
	public Gradient[] lightColorSegments;
	public AnimationCurve lightIntensity;
	public float cycleDuration = 60f;

	[Header("Weather Colors")]
	[SerializeField] private Color sunnyColor = Color.white;
	[SerializeField] private Color rainyColor = Color.gray;

	[Header("Storm Settings")]
	[SerializeField] private bool stormsEnabled = true;
	[SerializeField] private int stormChanceOutOf = 4;

	[Header("Debug")]
	[SerializeField] public float cycleTimer = 0f;
	[SerializeField] private int dayCount = 0;
	[SerializeField] private bool isRainyDay;
	[SerializeField] private bool isStormyDay;

	private float startOffset;
	private float previousCycleTimer;

	private const int hoursInDay = 24;
	private const int minutesInHour = 60;

	private void Awake()
	{
		if (globalLight == null)
			globalLight = FindFirstObjectByType<Light2D>();

		if (weatherManager == null)
			weatherManager = FindFirstObjectByType<WeatherManager>();

		if (thunderstormController == null)
			thunderstormController = FindFirstObjectByType<ThunderstormController>();
	}

	private void Start()
	{
		InitializeCycle();
		ValidateAndFixSetup();
		UpdateWeatherColors();
		SyncStormToCurrentWeather();
	}

	private void Update()
	{
		if (cycleDuration <= 0f) return;

		UpdateCycleTimer();

		if (IsSetupValidForLighting())
			UpdateLighting();

		UpdateClockText();

		if (IsNewDay())
		{
			OnNewDay();
			IncrementDayCount();
			UpdateWeatherColors();
		}

		previousCycleTimer = cycleTimer;
	}

	private void InitializeCycle()
	{
		startOffset = 6 * cycleDuration / hoursInDay;
		cycleTimer = 0f;
		UpdateClockText();
		UpdateDayCountText();
	}

	private void UpdateCycleTimer()
	{
		cycleTimer += Time.deltaTime;
		if (cycleTimer > cycleDuration)
			cycleTimer = 0f;
	}

	private bool IsSetupValidForLighting()
	{
		return globalLight != null &&
			   lightColorSegments != null &&
			   lightColorSegments.Length > 0 &&
			   lightColorSegments[0] != null &&
			   lightIntensity != null;
	}

	private void UpdateLighting()
	{
		float cycleProgress = cycleTimer / cycleDuration;

		int segmentCount = lightColorSegments.Length;
		float segmentDuration = 1f / segmentCount;

		int currentSegment = Mathf.FloorToInt(cycleProgress / segmentDuration);
		currentSegment = Mathf.Clamp(currentSegment, 0, segmentCount - 1);

		float segmentStart = currentSegment * segmentDuration;
		float segmentProgress = (cycleProgress - segmentStart) / segmentDuration;

		Gradient g = lightColorSegments[currentSegment];
		if (g == null) return;

		globalLight.color = g.Evaluate(segmentProgress);
		globalLight.intensity = lightIntensity.Evaluate(cycleProgress);
	}

	private void UpdateClockText()
	{
		if (cycleDuration <= 0f) return;

		float adjustedTime = (cycleTimer + startOffset) % cycleDuration;
		int hours = Mathf.FloorToInt(adjustedTime * hoursInDay / cycleDuration);
		int minutes = Mathf.FloorToInt((adjustedTime * hoursInDay / cycleDuration * minutesInHour) % minutesInHour);

		if (clockText != null)
			clockText.text = $"{hours:00}:{minutes:00}";
	}

	private bool IsNewDay()
	{
		return previousCycleTimer > cycleTimer;
	}

	private void OnNewDay()
	{
		if (weatherManager == null) return;

		int rainChance = Random.Range(0, 7);
		int fogChance = Random.Range(0, 14);

		if (rainChance == 5)
		{
			weatherManager.EnableRain(true);
			weatherManager.EnableFog(false);
			isRainyDay = true;

			isStormyDay = stormsEnabled && stormChanceOutOf > 0 && Random.Range(0, stormChanceOutOf) == 0;
		}
		else if (fogChance == 10)
		{
			weatherManager.EnableRain(false);
			weatherManager.EnableFog(true);
			isRainyDay = false;
			isStormyDay = false;
		}
		else
		{
			weatherManager.EnableRain(false);
			weatherManager.EnableFog(false);
			isRainyDay = false;
			isStormyDay = false;
		}

		if (thunderstormController != null)
			thunderstormController.SetStormActive(isStormyDay);
	}

	private void SyncStormToCurrentWeather()
	{
		if (thunderstormController == null) return;

		bool raining = weatherManager != null && weatherManager.IsRainActive();
		thunderstormController.SetStormActive(stormsEnabled && isStormyDay && raining);
	}

	private void IncrementDayCount()
	{
		dayCount++;
		UpdateDayCountText();
	}

	private void UpdateDayCountText()
	{
		if (dayCountText != null)
			dayCountText.text = $"Day {dayCount}";
	}

	private void ValidateAndFixSetup()
	{
		if (lightColorSegments == null || lightColorSegments.Length == 0)
		{
			Debug.LogError("lightColorSegments array is empty or null! Assign at least one gradient.");
			lightColorSegments = new Gradient[] { new Gradient() };
		}

		for (int i = 0; i < lightColorSegments.Length; i++)
		{
			if (lightColorSegments[i] == null)
				lightColorSegments[i] = new Gradient();
		}

		if (lightIntensity == null || lightIntensity.keys == null || lightIntensity.keys.Length == 0)
		{
			lightIntensity = new AnimationCurve(
				new Keyframe(0f, 1f),
				new Keyframe(1f, 1f)
			);
		}

		if (globalLight == null)
			globalLight = FindFirstObjectByType<Light2D>();

		if (weatherManager == null)
			weatherManager = FindFirstObjectByType<WeatherManager>();

		if (thunderstormController == null)
			thunderstormController = FindFirstObjectByType<ThunderstormController>();
	}

	private void UpdateWeatherColors()
	{
		if (lightColorSegments == null || lightColorSegments.Length < 1) return;
		if (lightColorSegments[0] == null) return;

		Gradient segment0 = lightColorSegments[0];
		GradientColorKey[] colorKeys = segment0.colorKeys;

		if (colorKeys != null && colorKeys.Length >= 3)
		{
			colorKeys[1].color = isRainyDay ? rainyColor : sunnyColor;
			colorKeys[2].color = isRainyDay ? rainyColor : sunnyColor;
			segment0.SetKeys(colorKeys, segment0.alphaKeys);
			lightColorSegments[0] = segment0;
		}
	}

	public int GetDayCount() => dayCount;
	public bool GetIsRainyDay() => isRainyDay;
	public bool GetIsStormyDay() => isStormyDay;

	public void SetState(float cycleTimerValue, int dayCountValue, bool isRainyDayValue)
	{
		cycleTimer = cycleTimerValue;
		dayCount = dayCountValue;
		isRainyDay = isRainyDayValue;

		UpdateClockText();
		UpdateDayCountText();
		UpdateWeatherColors();
		SyncStormToCurrentWeather();
	}
}

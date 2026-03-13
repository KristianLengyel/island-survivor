using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ElectricLamp : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;
	private Light2D lightSource;
	private DayNightCycle dayNightCycle;

	[Header("Day/Night Settings")]
	public Sprite lampOffSprite;
	public Sprite lampOnSprite;
	[Range(0f, 1f)]
	public float turnOnTime = 0.5f;
	[Range(0f, 1f)]
	public float turnOffTime = 0.958f;
	private bool isLampActive = false;
	private bool isFlickering = false;

	[Header("Flicker Settings")]
	public float flickerChance = 0.1f;
	public float flickerOnDuration = 0.1f;
	public float flickerOffDuration = 0.05f;
	public float flickerFinalOnDelay = 0.1f;

	private void Start()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		lightSource = GetComponentInChildren<Light2D>();
		dayNightCycle = FindAnyObjectByType<DayNightCycle>();

		if (spriteRenderer == null)
		{
			Debug.LogError("SpriteRenderer missing on ElectricLamp!");
		}
		if (lightSource == null)
		{
			Debug.LogError("Light2D missing on ElectricLamp or its children!");
		}
		if (dayNightCycle == null)
		{
			Debug.LogError("DayNightCycle not found in scene!");
		}
		UpdateLampState();
	}

	private void Update()
	{
		if (dayNightCycle != null && !isFlickering)
		{
			float cycleProgress = dayNightCycle.cycleTimer / dayNightCycle.cycleDuration;
			bool shouldBeOn = ShouldLampBeOn(cycleProgress);

			if (shouldBeOn != isLampActive)
			{
				isLampActive = shouldBeOn;
				if (isLampActive)
				{
					if (Random.value <= flickerChance)
					{
						StartCoroutine(FlickerRoutine());
					}
					else
					{
						UpdateLampState();
					}
				}
				else
				{
					UpdateLampState();
				}
			}
		}
	}

	private bool ShouldLampBeOn(float cycleProgress)
	{
		if (turnOnTime > turnOffTime)
		{
			return cycleProgress >= turnOnTime || cycleProgress < turnOffTime;
		}
		else
		{
			return cycleProgress >= turnOnTime && cycleProgress < turnOffTime;
		}
	}

	private void UpdateLampState()
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.sprite = isLampActive ? lampOnSprite : lampOffSprite;
		}
		if (lightSource != null)
		{
			lightSource.enabled = isLampActive;
		}
	}

	private System.Collections.IEnumerator FlickerRoutine()
	{
		isFlickering = true;

		isLampActive = true;
		UpdateLampState();
		yield return new WaitForSeconds(flickerOnDuration);

		isLampActive = false;
		UpdateLampState();
		yield return new WaitForSeconds(flickerOffDuration);

		isLampActive = true;
		UpdateLampState();
		yield return new WaitForSeconds(flickerFinalOnDelay);

		isFlickering = false;
	}
}
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ThunderstormController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Light2D globalLight;
	[SerializeField] private WeatherManager weatherManager;

	[Header("Storm Settings")]
	[SerializeField] private bool stormActive;
	[SerializeField] private Vector2 timeBetweenFlashes = new Vector2(6f, 18f);

	[Header("Lightning Flash")]
	[SerializeField] private int flashesPerStrikeMin = 1;
	[SerializeField] private int flashesPerStrikeMax = 3;

	[SerializeField] private float flashIntensityMultiplier = 1.8f;
	[SerializeField] private float flashUpTime = 0.03f;
	[SerializeField] private float flashDownTime = 0.10f;
	[SerializeField] private float flashHoldTime = 0.02f;

	[SerializeField] private Color flashColor = Color.white;
	[SerializeField] private float colorBlend = 0.6f;

	[Header("Optional Thunder Audio")]
	[SerializeField] private bool playThunderSound;
	[SerializeField] private Vector2 thunderDelay = new Vector2(0.2f, 1.2f);
	[SerializeField] private string thunderSoundName = "Thunder";

	private Coroutine loopCo;

	private float baseIntensity;
	private Color baseColor;
	private bool capturedBase;

	private void Awake()
	{
		if (globalLight == null)
			globalLight = FindFirstObjectByType<Light2D>();

		if (weatherManager == null)
			weatherManager = FindFirstObjectByType<WeatherManager>();
	}

	private void OnEnable()
	{
		RestartLoop();
	}

	private void OnDisable()
	{
		if (loopCo != null) StopCoroutine(loopCo);
		loopCo = null;
		RestoreBaseLight();
	}

	public void SetStormActive(bool active)
	{
		stormActive = active;
		RestartLoop();
	}

	public bool IsStormActive() => stormActive;

	private void RestartLoop()
	{
		if (loopCo != null) StopCoroutine(loopCo);
		loopCo = StartCoroutine(StormLoop());
	}

	private IEnumerator StormLoop()
	{
		while (true)
		{
			yield return null;

			if (!stormActive) continue;
			if (weatherManager != null && !weatherManager.IsRainActive()) continue;
			if (globalLight == null) continue;

			float wait = Random.Range(timeBetweenFlashes.x, timeBetweenFlashes.y);
			yield return new WaitForSeconds(wait);

			int flashes = Random.Range(flashesPerStrikeMin, flashesPerStrikeMax + 1);
			for (int i = 0; i < flashes; i++)
			{
				yield return FlashOnce();

				if (i < flashes - 1)
					yield return new WaitForSeconds(Random.Range(0.05f, 0.25f));
			}

			if (playThunderSound && AudioManager.instance != null)
			{
				yield return new WaitForSeconds(Random.Range(thunderDelay.x, thunderDelay.y));
				AudioManager.instance.PlaySound(thunderSoundName);
			}
		}
	}

	private IEnumerator FlashOnce()
	{
		CaptureBaseLightIfNeeded();

		float targetIntensity = baseIntensity * flashIntensityMultiplier;
		Color targetColor = Color.Lerp(baseColor, flashColor, Mathf.Clamp01(colorBlend));

		float t = 0f;
		while (t < flashUpTime)
		{
			t += Time.deltaTime;
			float a = flashUpTime <= 0f ? 1f : Mathf.Clamp01(t / flashUpTime);
			ApplyLight(Mathf.Lerp(baseIntensity, targetIntensity, a), Color.Lerp(baseColor, targetColor, a));
			yield return null;
		}

		if (flashHoldTime > 0f)
			yield return new WaitForSeconds(flashHoldTime);

		t = 0f;
		while (t < flashDownTime)
		{
			t += Time.deltaTime;
			float a = flashDownTime <= 0f ? 1f : Mathf.Clamp01(t / flashDownTime);
			ApplyLight(Mathf.Lerp(targetIntensity, baseIntensity, a), Color.Lerp(targetColor, baseColor, a));
			yield return null;
		}

		RestoreBaseLight();
	}

	private void CaptureBaseLightIfNeeded()
	{
		if (capturedBase) return;
		if (globalLight == null) return;

		baseIntensity = globalLight.intensity;
		baseColor = globalLight.color;
		capturedBase = true;
	}

	private void RestoreBaseLight()
	{
		if (!capturedBase) return;
		if (globalLight == null) return;

		globalLight.intensity = baseIntensity;
		globalLight.color = baseColor;
		capturedBase = false;
	}

	private void ApplyLight(float intensity, Color color)
	{
		if (globalLight == null) return;

		globalLight.intensity = intensity;
		globalLight.color = color;
	}
}

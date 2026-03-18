using UnityEngine;
using UnityEngine.VFX;

public class WeatherManager : MonoBehaviour
{
	[Header("References")]
	public Transform player;
	public ParticleSystem rainParticleSystem;
	public ParticleSystem rainSplashParticleSystem;
	public VisualEffect fogEffect;

	[Header("Settings")]
	[SerializeField] private float offset = 5f;
	[SerializeField] private float rainEmissionRate = 150f;

	[Header("Weather States")]
	[SerializeField] private bool rainActive;
	[SerializeField] private bool fogActive;

	private bool playerIndoors;
	private ParticleSystem.Particle[] rainParticles;
	private ParticleSystem.Particle[] splashParticles;
	private byte rainParticleOriginalAlpha = 255;

	private const string ColliderPosParam = "ColliderPos";

	private void Start()
	{
		if (rainParticleSystem != null)
		{
			rainParticles = new ParticleSystem.Particle[rainParticleSystem.main.maxParticles];
			rainParticleOriginalAlpha = (byte)(rainParticleSystem.main.startColor.color.a * 255f);
		}

		if (rainSplashParticleSystem != null)
			splashParticles = new ParticleSystem.Particle[rainSplashParticleSystem.main.maxParticles];

		UpdateWeatherState();
	}

	private void LateUpdate()
	{
		UpdateRainPosition();
		UpdateFogPosition();
		FilterIndoorRainParticles();
	}

	private void UpdateRainPosition()
	{
		if (rainParticleSystem != null)
		{
			Vector3 newPosition = new Vector3(player.position.x + 3f, player.position.y + offset, transform.position.z);
			rainParticleSystem.transform.position = newPosition;
		}
	}

	private void UpdateFogPosition()
	{
		if (fogEffect == null || player == null) return;
		fogEffect.SetVector3(ColliderPosParam, player.position);
	}

	private void FilterIndoorRainParticles()
	{
		if (!rainActive || rainParticleSystem == null || rainParticles == null) return;

		var bm = GameManager.Instance != null ? GameManager.Instance.BuildingManager : null;
		if (bm == null) return;

		bool worldSpace = rainParticleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World;
		int count = rainParticleSystem.GetParticles(rainParticles);
		bool changed = false;

		for (int i = 0; i < count; i++)
		{
			Vector3 worldPos = worldSpace
				? rainParticles[i].position
				: rainParticleSystem.transform.TransformPoint(rainParticles[i].position);

			bool indoors = bm.IsIndoors(worldPos);
			Color32 col = rainParticles[i].startColor;
			byte targetAlpha = (playerIndoors && indoors) ? (byte)0 : rainParticleOriginalAlpha;

			if (col.a != targetAlpha)
			{
				col.a = targetAlpha;
				rainParticles[i].startColor = col;
				changed = true;
			}
		}

		if (changed)
			rainParticleSystem.SetParticles(rainParticles, count);

		if (rainSplashParticleSystem == null || splashParticles == null) return;

		bool splashWorldSpace = rainSplashParticleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World;
		int splashCount = rainSplashParticleSystem.GetParticles(splashParticles);
		bool splashChanged = false;

		for (int i = 0; i < splashCount; i++)
		{
			Vector3 splashWorldPos = splashWorldSpace
				? splashParticles[i].position
				: rainSplashParticleSystem.transform.TransformPoint(splashParticles[i].position);

			if (playerIndoors && bm.IsIndoors(splashWorldPos))
			{
				splashParticles[i].remainingLifetime = 0f;
				splashChanged = true;
			}
		}

		if (splashChanged)
			rainSplashParticleSystem.SetParticles(splashParticles, splashCount);
	}

	public void EnableRain(bool enable)
	{
		rainActive = enable;
		if (enable) fogActive = false;
		UpdateWeatherState();
	}

	public void EnableFog(bool enable)
	{
		fogActive = enable;
		if (enable) rainActive = false;
		UpdateWeatherState();
	}

	public void ToggleRain()
	{
		rainActive = !rainActive;
		if (rainActive) fogActive = false;
		UpdateWeatherState();
	}

	public void ToggleFog()
	{
		fogActive = !fogActive;
		if (fogActive) rainActive = false;
		UpdateWeatherState();
	}

	public void SetPlayerIndoors(bool indoors)
	{
		playerIndoors = indoors;
		UpdateWeatherState();
	}

	public void UpdateWeatherState()
	{
		UpdateRainEmission();
		UpdateFogActivation();

		if (rainActive)
			AudioManager.instance.PlaySound("LightRain");
		else
			AudioManager.instance.StopSound("LightRain");
	}

	private void UpdateRainEmission()
	{
		if (rainParticleSystem != null)
		{
			var emission = rainParticleSystem.emission;
			emission.rateOverTime = rainActive ? rainEmissionRate : 0f;
		}
	}

	private void UpdateFogActivation()
	{
		if (fogEffect != null)
			fogEffect.gameObject.SetActive(fogActive && !playerIndoors);
	}

	public void SetRainEmissionRate(float newRate)
	{
		rainEmissionRate = newRate;
		UpdateWeatherState();
	}

	public bool IsRainActive() => rainActive;
	public bool IsFogActive() => fogActive;
}

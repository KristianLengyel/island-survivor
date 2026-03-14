using UnityEngine;
using UnityEngine.VFX;

public class WeatherManager : MonoBehaviour
{
	[Header("References")]
	public Transform player;
	public ParticleSystem rainParticleSystem;
	public VisualEffect fogEffect;

	[Header("Settings")]
	[SerializeField] private float offset = 5f;
	[SerializeField] private float rainEmissionRate = 150f;

	[Header("Weather States")]
	[SerializeField] private bool rainActive;
	[SerializeField] private bool fogActive;

	private bool playerIndoors;

	private const string ColliderPosParam = "ColliderPos";

	private void Start()
	{
		UpdateWeatherState();
	}

	private void LateUpdate()
	{
		UpdateRainPosition();
		UpdateFogPosition();
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
		{
			AudioManager.instance.PlaySound("LightRain");
		}
		else
		{
			AudioManager.instance.StopSound("LightRain");
		}
	}

	private void UpdateRainEmission()
	{
		if (rainParticleSystem != null)
		{
			var emission = rainParticleSystem.emission;
			emission.rateOverTime = (rainActive && !playerIndoors) ? rainEmissionRate : 0f;
		}
	}

	private void UpdateFogActivation()
	{
		if (fogEffect != null)
		{
			fogEffect.gameObject.SetActive(fogActive && !playerIndoors);
		}
	}

	public void SetRainEmissionRate(float newRate)
	{
		rainEmissionRate = newRate;
		UpdateWeatherState();
	}

	public bool IsRainActive() => rainActive;
	public bool IsFogActive() => fogActive;
}

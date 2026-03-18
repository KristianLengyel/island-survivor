using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer))]
public class FireflyAgent : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] private float moveSpeedMin = 0.15f;
	[SerializeField] private float moveSpeedMax = 0.35f;
	[SerializeField] private float roamRadius = 1.5f;
	[SerializeField] private float directionChangeIntervalMin = 1.2f;
	[SerializeField] private float directionChangeIntervalMax = 3.2f;
	[SerializeField] private float bobAmplitude = 0.08f;
	[SerializeField] private float bobFrequency = 1.2f;

	[Header("Fade / Pulse")]
	[SerializeField] private float alphaMin = 0.15f;
	[SerializeField] private float alphaMax = 0.9f;
	[SerializeField] private float pulseSpeedMin = 0.8f;
	[SerializeField] private float pulseSpeedMax = 1.8f;

	[Header("Light")]
	[SerializeField] private bool allowLight = true;
	[SerializeField] private float lightIntensityMin = 0.15f;
	[SerializeField] private float lightIntensityMax = 0.45f;

	private SpriteRenderer spriteRenderer;
	private Light2D light2D;

	private Vector3 anchorPosition;
	private Vector3 targetOffset;
	private float moveSpeed;
	private float changeDirectionTimer;
	private float changeDirectionDuration;

	private float pulseOffset;
	private float pulseSpeed;
	private float bobOffset;

	private bool isInitialized;

	public void Initialize(Vector3 anchor, bool enableLight)
	{
		if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
		if (light2D == null) light2D = GetComponent<Light2D>();

		anchorPosition = anchor;
		transform.position = anchor;
		targetOffset = Random.insideUnitCircle * roamRadius;
		moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);
		changeDirectionDuration = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
		changeDirectionTimer = changeDirectionDuration;

		pulseOffset = Random.Range(0f, 100f);
		pulseSpeed = Random.Range(pulseSpeedMin, pulseSpeedMax);
		bobOffset = Random.Range(0f, 100f);

		if (light2D != null)
		{
			light2D.enabled = allowLight && enableLight;
			if (light2D.enabled)
			{
				light2D.intensity = Random.Range(lightIntensityMin, lightIntensityMax);
			}
		}

		isInitialized = true;
		gameObject.SetActive(true);
	}

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		light2D = GetComponent<Light2D>();
	}

	private void Update()
	{
		if (!isInitialized) return;

		UpdateMovement();
		UpdatePulse();
	}

	private void UpdateMovement()
	{
		changeDirectionTimer -= Time.deltaTime;
		if (changeDirectionTimer <= 0f)
		{
			targetOffset = Random.insideUnitCircle * roamRadius;
			moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);
			changeDirectionDuration = Random.Range(directionChangeIntervalMin, directionChangeIntervalMax);
			changeDirectionTimer = changeDirectionDuration;
		}

		Vector3 desired = anchorPosition + targetOffset;
		transform.position = Vector3.MoveTowards(transform.position, desired, moveSpeed * Time.deltaTime);

		Vector3 pos = transform.position;
		pos.y += Mathf.Sin((Time.time + bobOffset) * bobFrequency) * bobAmplitude * Time.deltaTime;
		transform.position = pos;
	}

	private void UpdatePulse()
	{
		float t = (Mathf.Sin((Time.time + pulseOffset) * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
		float alpha = Mathf.Lerp(alphaMin, alphaMax, t);

		Color c = spriteRenderer.color;
		c.a = alpha;
		spriteRenderer.color = c;

		if (light2D != null && light2D.enabled)
		{
			light2D.intensity = Mathf.Lerp(lightIntensityMin, lightIntensityMax, t);
		}
	}

	public void Deactivate()
	{
		isInitialized = false;
		gameObject.SetActive(false);
	}
}
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(AudioSource))]
public class WaterPurifier : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Renderers")]
	[SerializeField] private SpriteRenderer bottomRenderer;
	[SerializeField] private SpriteRenderer topRenderer;
	[SerializeField] private SpriteRenderer woodRenderer;
	[SerializeField] private SpriteRenderer bottleRenderer;
	[SerializeField] private SpriteRenderer bucketRenderer;

	[Header("Outline")]
	[SerializeField] private SpriteRenderer outlineBottomRenderer;
	[SerializeField] private SpriteRenderer outlineTopRenderer;

	[Header("Base Stand Sprites")]
	[SerializeField] private Sprite idleBottomSprite;
	[SerializeField] private Sprite idleTopSprite;

	[Header("Processing Animation (4 frames)")]
	[SerializeField] private Sprite[] processingBottomFrames;
	[SerializeField] private Sprite[] processingTopFrames;
	[SerializeField] private float animationFrameRate = 0.2f;

	[Header("Modules")]
	[SerializeField] private Sprite woodEmptySprite;
	[SerializeField] private Sprite woodFullSprite;
	[SerializeField] private Sprite woodLastUseSprite;

	[SerializeField] private Sprite bottleEmptySprite;
	[SerializeField] private Sprite bottleFullSprite;

	[SerializeField] private Sprite bucketEmptySprite;
	[SerializeField] private Sprite bucketFullSprite;

	[Header("Settings")]
	[SerializeField] private float purificationTime = 10f;
	[SerializeField] private string fuelItemName = "Plank";
	[SerializeField] private int fuelUses = 1;
	private const int WATER_FILL_AMOUNT = 1;

	[Header("Audio and Light")]
	[SerializeField] private Light2D purifyingLight;
	[SerializeField] private float maxOuterRadius = 2f;
	[SerializeField] private float radiusGrowDuration = 1f;

	[Header("Smoke Particle System")]
	[SerializeField] private ParticleSystem smokeParticleSystem;
	[SerializeField] private float maxEmissionRate = 50f;
	[SerializeField] private float emissionIncreaseDuration = 2f;

	private enum PurifierState { Idle, Purifying, Purified }

	public string SaveKey => "WaterPurifier";

	private AudioSource purifyingAudioSource;
	private InventoryManager inventoryManager;
	private AudioManager audioManager;

	private PurifierState state = PurifierState.Idle;

	private bool hasWood = false;
	private int remainingFuelUses = 0;

	private bool hasSaltWaterLoaded = false;

	private float purifyTimer = 0f;
	private float animTimer = 0f;
	private int currentFrameIndex = 0;
	private Coroutine purifyCoroutine;
	private Coroutine emissionCoroutine;

	private void Awake()
	{
		purifyingAudioSource = GetComponent<AudioSource>();
		if (purifyingLight == null) purifyingLight = GetComponentInChildren<Light2D>();
	}

	private void Start()
	{
		inventoryManager = GameManager.Instance?.InventoryManager;
		audioManager = GameManager.Instance?.AudioManager;

		if (inventoryManager == null || audioManager == null) { enabled = false; return; }
		if (bottomRenderer == null || topRenderer == null || woodRenderer == null || bottleRenderer == null || bucketRenderer == null) { enabled = false; return; }
		if (purifyingLight == null) { enabled = false; return; }
		if (smokeParticleSystem == null) { enabled = false; return; }

		if (outlineBottomRenderer != null) outlineBottomRenderer.gameObject.SetActive(true);
		if (outlineTopRenderer != null) outlineTopRenderer.gameObject.SetActive(true);

		if (outlineBottomRenderer != null) outlineBottomRenderer.enabled = false;
		if (outlineTopRenderer != null) outlineTopRenderer.enabled = false;

		purifyingAudioSource.loop = true;
		purifyingAudioSource.Stop();

		purifyingLight.enabled = false;
		purifyingLight.pointLightOuterRadius = 0f;

		smokeParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		var emission = smokeParticleSystem.emission;
		emission.rateOverTime = 0f;

		ApplyVisualState();
	}

	public void Interact()
	{
		Item selectedItem = inventoryManager.GetSelectedItem();
		int selectedSlotIndex = inventoryManager.SelectedSlotIndex;
		var slotItem = inventoryManager.inventorySlots[selectedSlotIndex].GetComponentInChildren<WaterContainerInventoryItem>();

		if (state == PurifierState.Purified)
		{
			if (slotItem != null && slotItem.item is WaterContainerItem && slotItem.currentFill < slotItem.item.maxFillCapacity)
			{
				if (slotItem.Fill(WATER_FILL_AMOUNT, false))
				{
					state = PurifierState.Idle;
					purifyTimer = 0f;
					animTimer = 0f;
					currentFrameIndex = 0;
					audioManager.PlaySound("ItemPickup");
					ApplyVisualState();
				}
			}
			return;
		}

		if (state == PurifierState.Purifying) return;

		bool insertedSomething = false;

		if (!hasWood && selectedItem != null && selectedItem.name == fuelItemName)
		{
			hasWood = true;
			remainingFuelUses = Mathf.Max(1, fuelUses);
			inventoryManager.RemoveItem(selectedItem.name, 1);
			audioManager.PlaySound("PlaceSound");
			insertedSomething = true;
		}

		if (!hasSaltWaterLoaded &&
			slotItem != null &&
			slotItem.item is WaterContainerItem &&
			slotItem.isSaltWater &&
			slotItem.currentFill >= WATER_FILL_AMOUNT)
		{
			slotItem.currentFill -= WATER_FILL_AMOUNT;
			slotItem.UpdateSprite();
			slotItem.RefreshCount();

			hasSaltWaterLoaded = true;
			audioManager.PlaySound("PlaceSound");
			insertedSomething = true;
		}

		if (!insertedSomething) return;

		TryStartPurifyFromLoadedInputs();
		ApplyVisualState();
	}

	private void TryStartPurifyFromLoadedInputs()
	{
		if (state != PurifierState.Idle) return;
		if (!(hasWood && remainingFuelUses > 0)) return;
		if (!hasSaltWaterLoaded) return;

		state = PurifierState.Purifying;
		purifyTimer = 0f;
		animTimer = 0f;
		currentFrameIndex = 0;

		if (purifyCoroutine != null)
		{
			StopCoroutine(purifyCoroutine);
			purifyCoroutine = null;
		}

		purifyCoroutine = StartCoroutine(PurifyRoutine());
	}

	private System.Collections.IEnumerator PurifyRoutine()
	{
		purifyingAudioSource.Play();
		purifyingLight.enabled = true;

		smokeParticleSystem.Play();
		if (emissionCoroutine != null)
		{
			StopCoroutine(emissionCoroutine);
			emissionCoroutine = null;
		}
		emissionCoroutine = StartCoroutine(IncreaseEmissionRate());

		while (purifyTimer < purificationTime)
		{
			float dt = Time.deltaTime;
			purifyTimer += dt;

			float radiusT = radiusGrowDuration <= 0f ? 1f : Mathf.Clamp01(purifyTimer / radiusGrowDuration);
			purifyingLight.pointLightOuterRadius = Mathf.Lerp(0f, maxOuterRadius, radiusT);

			AdvanceProcessingAnimation(dt);

			yield return null;
		}

		state = PurifierState.Purified;

		hasSaltWaterLoaded = false;
		ConsumeWoodUse();

		purifyingAudioSource.Stop();
		purifyingLight.enabled = false;
		purifyingLight.pointLightOuterRadius = 0f;

		smokeParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		if (emissionCoroutine != null)
		{
			StopCoroutine(emissionCoroutine);
			emissionCoroutine = null;
		}
		var emission = smokeParticleSystem.emission;
		emission.rateOverTime = 0f;

		audioManager.PlaySound("PurificationDone");

		purifyCoroutine = null;
		ApplyVisualState();
	}

	private System.Collections.IEnumerator IncreaseEmissionRate()
	{
		float timer = 0f;
		var emission = smokeParticleSystem.emission;

		while (timer < emissionIncreaseDuration)
		{
			timer += Time.deltaTime;
			float rate = Mathf.Lerp(0f, maxEmissionRate, timer / Mathf.Max(0.0001f, emissionIncreaseDuration));
			emission.rateOverTime = rate;
			yield return null;
		}

		emission.rateOverTime = maxEmissionRate;
		emissionCoroutine = null;
	}

	private void AdvanceProcessingAnimation(float dt)
	{
		if (processingBottomFrames == null || processingTopFrames == null) return;
		if (processingBottomFrames.Length == 0 || processingTopFrames.Length == 0) return;
		if (animationFrameRate <= 0f) return;

		animTimer += dt;
		if (animTimer < animationFrameRate) return;
		animTimer -= animationFrameRate;

		int frameCount = Mathf.Min(processingBottomFrames.Length, processingTopFrames.Length);
		currentFrameIndex = (currentFrameIndex + 1) % frameCount;

		bottomRenderer.sprite = processingBottomFrames[currentFrameIndex];
		topRenderer.sprite = processingTopFrames[currentFrameIndex];
	}

	private void ConsumeWoodUse()
	{
		if (!hasWood) return;

		remainingFuelUses--;
		if (remainingFuelUses <= 0)
		{
			hasWood = false;
			remainingFuelUses = 0;
		}
	}

	private Sprite GetWoodSprite()
	{
		if (!hasWood || remainingFuelUses <= 0) return woodEmptySprite;
		if (remainingFuelUses == 1) return woodLastUseSprite != null ? woodLastUseSprite : woodFullSprite;
		return woodFullSprite;
	}

	public void SetHighlighted(bool highlight)
	{
		Item selectedItem = inventoryManager?.GetSelectedItem();
		var slotItem = inventoryManager?.inventorySlots[inventoryManager.SelectedSlotIndex]
			.GetComponentInChildren<WaterContainerInventoryItem>();

		bool canHighlight = false;

		if (state == PurifierState.Purified)
		{
			if (slotItem != null && slotItem.item is WaterContainerItem &&
				slotItem.currentFill < slotItem.item.maxFillCapacity)
				canHighlight = true;
		}
		else if (state == PurifierState.Idle)
		{
			bool canInsertWood = !hasWood && selectedItem != null && selectedItem.name == fuelItemName;

			bool canInsertSalt = !hasSaltWaterLoaded &&
								 slotItem != null &&
								 slotItem.item is WaterContainerItem &&
								 slotItem.isSaltWater &&
								 slotItem.currentFill >= WATER_FILL_AMOUNT;

			if (canInsertWood || canInsertSalt)
				canHighlight = true;
		}

		bool enabled = highlight && canHighlight;

		if (outlineBottomRenderer != null) outlineBottomRenderer.enabled = enabled;
		if (outlineTopRenderer != null) outlineTopRenderer.enabled = enabled;
	}

	public float GetInteractionRange() => 2f;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		Bounds b = bottomRenderer.bounds;
		b.Encapsulate(topRenderer.bounds);
		b.Encapsulate(woodRenderer.bounds);
		b.Encapsulate(bottleRenderer.bounds);
		b.Encapsulate(bucketRenderer.bounds);

		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}

	private void SetModuleVisibility(bool showWood, bool showBottle, bool showBucket)
	{
		if (woodRenderer != null) woodRenderer.enabled = showWood;
		if (bottleRenderer != null) bottleRenderer.enabled = showBottle;
		if (bucketRenderer != null) bucketRenderer.enabled = showBucket;
	}

	private void ApplyVisualState()
	{
		if (purifyCoroutine != null && state != PurifierState.Purifying)
		{
			StopCoroutine(purifyCoroutine);
			purifyCoroutine = null;
		}

		if (state != PurifierState.Purifying)
		{
			purifyingAudioSource.Stop();
			purifyingLight.enabled = false;
			purifyingLight.pointLightOuterRadius = 0f;

			smokeParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			if (emissionCoroutine != null)
			{
				StopCoroutine(emissionCoroutine);
				emissionCoroutine = null;
			}
			var emission = smokeParticleSystem.emission;
			emission.rateOverTime = 0f;
		}

		if (state == PurifierState.Purifying)
		{
			SetModuleVisibility(showWood: false, showBottle: false, showBucket: false);

			if (processingBottomFrames != null && processingTopFrames != null &&
				processingBottomFrames.Length > 0 && processingTopFrames.Length > 0)
			{
				int frameCount = Mathf.Min(processingBottomFrames.Length, processingTopFrames.Length);
				currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, frameCount - 1);
				bottomRenderer.sprite = processingBottomFrames[currentFrameIndex];
				topRenderer.sprite = processingTopFrames[currentFrameIndex];
			}
			else
			{
				bottomRenderer.sprite = idleBottomSprite;
				topRenderer.sprite = idleTopSprite;
			}

			return;
		}

		bottomRenderer.sprite = idleBottomSprite;
		topRenderer.sprite = idleTopSprite;

		SetModuleVisibility(showWood: true, showBottle: true, showBucket: true);

		if (state == PurifierState.Purified)
		{
			woodRenderer.sprite = GetWoodSprite();
			bottleRenderer.sprite = bottleEmptySprite;
			bucketRenderer.sprite = bucketFullSprite;
			return;
		}

		woodRenderer.sprite = GetWoodSprite();
		bottleRenderer.sprite = hasSaltWaterLoaded ? bottleFullSprite : bottleEmptySprite;
		bucketRenderer.sprite = bucketEmptySprite;
	}

	[System.Serializable]
	private class SaveState
	{
		public int state;
		public bool hasWood;
		public int remainingFuelUses;
		public bool hasSaltWaterLoaded;
		public float remainingPurifyTime;
		public int currentFrameIndex;
		public float animTimer;
	}

	public string CaptureStateJson()
	{
		float remaining = 0f;
		if (state == PurifierState.Purifying)
			remaining = Mathf.Max(0f, purificationTime - purifyTimer);

		var st = new SaveState
		{
			state = (int)state,
			hasWood = hasWood,
			remainingFuelUses = remainingFuelUses,
			hasSaltWaterLoaded = hasSaltWaterLoaded,
			remainingPurifyTime = remaining,
			currentFrameIndex = currentFrameIndex,
			animTimer = animTimer
		};

		return JsonUtility.ToJson(st);
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var st = JsonUtility.FromJson<SaveState>(json);
		if (st == null) return;

		state = (PurifierState)st.state;
		hasWood = st.hasWood;
		remainingFuelUses = st.remainingFuelUses;
		hasSaltWaterLoaded = st.hasSaltWaterLoaded;

		currentFrameIndex = st.currentFrameIndex;
		animTimer = Mathf.Max(0f, st.animTimer);

		if (purifyCoroutine != null)
		{
			StopCoroutine(purifyCoroutine);
			purifyCoroutine = null;
		}

		purifyingAudioSource.Stop();
		purifyingLight.enabled = false;
		purifyingLight.pointLightOuterRadius = 0f;

		smokeParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		if (emissionCoroutine != null)
		{
			StopCoroutine(emissionCoroutine);
			emissionCoroutine = null;
		}
		var emission = smokeParticleSystem.emission;
		emission.rateOverTime = 0f;

		if (state == PurifierState.Purifying)
		{
			float remaining = Mathf.Max(0f, st.remainingPurifyTime);
			purifyTimer = Mathf.Clamp(purificationTime - remaining, 0f, purificationTime);

			if (purifyTimer >= purificationTime)
			{
				state = PurifierState.Purified;
				purifyTimer = purificationTime;
				hasSaltWaterLoaded = false;
				ApplyVisualState();
			}
			else
			{
				ApplyVisualState();

				smokeParticleSystem.Play();
				emissionCoroutine = StartCoroutine(IncreaseEmissionRate());

				purifyCoroutine = StartCoroutine(PurifyRoutine());
			}
		}
		else
		{
			purifyTimer = 0f;
			ApplyVisualState();
		}
	}

	private void OnDisable()
	{
		if (purifyCoroutine != null)
		{
			StopCoroutine(purifyCoroutine);
			purifyCoroutine = null;
		}

		if (emissionCoroutine != null)
		{
			StopCoroutine(emissionCoroutine);
			emissionCoroutine = null;
		}

		if (smokeParticleSystem != null)
		{
			smokeParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			var emission = smokeParticleSystem.emission;
			emission.rateOverTime = 0f;
		}
	}
}

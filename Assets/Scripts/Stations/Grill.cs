using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer), typeof(AudioSource))]
public class Grill : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Grill Base Sprites")]
	[SerializeField] private Sprite emptyNoFuelSprite;
	[SerializeField] private Sprite emptyWithFuelSprite;

	[Header("Grill Base Animation (optional)")]
	[SerializeField] private Sprite[] cookingAnimationFrames;
	[SerializeField] private float animationFrameRate = 0.2f;

	[Header("Grill Settings")]
	[SerializeField] private float defaultCookingTime = 10f;
	[SerializeField] private string fuelItemName = "Plank";
	[SerializeField] private int fuelUses = 2;

	[Header("Processing")]
	[SerializeField] private ProcessingRecipeBook recipeBook;

	[Header("Food Overlay")]
	[SerializeField] private SpriteRenderer foodOverlayRenderer;

	[Header("Outline")]
	[SerializeField] private SpriteRenderer outlineRenderer;

	[Header("Audio and Light")]
	[SerializeField] private Light2D cookingLight;
	[SerializeField] private float maxOuterRadius = 2f;
	[SerializeField] private float radiusGrowDuration = 1f;

	[Header("Smoke Particle System")]
	[SerializeField] private ParticleSystem smokeParticleSystem;
	[SerializeField] private float maxEmissionRate = 50f;
	[SerializeField] private float emissionIncreaseDuration = 2f;

	private enum GrillState
	{
		Idle,
		Loaded,
		Cooking,
		Cooked
	}

	private SpriteRenderer spriteRenderer;
	private AudioSource cookingAudioSource;
	private InventoryManager inventoryManager;
	private AudioManager audioManager;
	private GameObject outlineChild;

	private GrillState state = GrillState.Idle;

	private bool hasFuel = false;
	private int remainingFuelUses = 0;

	private ProcessingRecipe activeRecipe;
	private Item pendingOutputItem;

	private readonly List<Item> loadedInputs = new();
	private float cookRemaining = 0f;
	private float cookTotal = 0f;

	private int currentFrameIndex = 0;
	private float frameTimer = 0f;

	private Coroutine cookCoroutine;
	private Coroutine emissionCoroutine;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		cookingAudioSource = GetComponent<AudioSource>();
		if (cookingLight == null) cookingLight = GetComponentInChildren<Light2D>();
	}

	private void Start()
	{
		if (spriteRenderer == null || cookingAudioSource == null || cookingLight == null || smokeParticleSystem == null)
		{
			enabled = false;
			return;
		}

		if (foodOverlayRenderer == null)
		{
			var t = transform.Find("FoodOverlay");
			if (t != null) foodOverlayRenderer = t.GetComponent<SpriteRenderer>();
		}

		if (foodOverlayRenderer != null)
		{
			foodOverlayRenderer.enabled = false;
			foodOverlayRenderer.sprite = null;
			foodOverlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
			foodOverlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
		}

		if (outlineRenderer == null)
		{
			outlineChild = transform.Find("Outline")?.gameObject;
			if (outlineChild != null) outlineRenderer = outlineChild.GetComponent<SpriteRenderer>();
		}
		else
		{
			outlineChild = outlineRenderer.gameObject;
		}

		if (outlineChild != null && !outlineChild.activeSelf) outlineChild.SetActive(true);
		if (outlineRenderer != null) outlineRenderer.enabled = false;

		cookingAudioSource.loop = true;
		cookingAudioSource.Stop();

		cookingLight.enabled = false;
		cookingLight.pointLightOuterRadius = 0f;

		smokeParticleSystem.Stop();
		var emission = smokeParticleSystem.emission;
		emission.rateOverTime = 0f;

		inventoryManager = GameManager.Instance?.InventoryManager;
		audioManager = GameManager.Instance?.AudioManager;

		if (inventoryManager == null || audioManager == null)
		{
			enabled = false;
			return;
		}

		UpdateBaseSprite();
		ApplyRestoredVisuals();

		if (state == GrillState.Cooking && activeRecipe != null && pendingOutputItem != null && cookRemaining > 0f)
		{
			StartCookingVisuals();
			cookCoroutine = StartCoroutine(CookRoutine());
		}
		else if (state == GrillState.Loaded)
		{
			TryStartCookingIfReady();
		}
	}

	private void Update()
	{
		if (state != GrillState.Cooking) return;

		if (cookingAnimationFrames != null && cookingAnimationFrames.Length > 0 && animationFrameRate > 0f)
		{
			frameTimer += Time.deltaTime;
			while (frameTimer >= animationFrameRate)
			{
				frameTimer -= animationFrameRate;
				currentFrameIndex = (currentFrameIndex + 1) % cookingAnimationFrames.Length;
				spriteRenderer.sprite = cookingAnimationFrames[currentFrameIndex];
			}
		}
	}

	private void UpdateBaseSprite()
	{
		spriteRenderer.sprite = hasFuel ? emptyWithFuelSprite : emptyNoFuelSprite;
	}

	private void SetFoodOverlay(Sprite sprite)
	{
		if (foodOverlayRenderer == null) return;

		foodOverlayRenderer.sprite = sprite;
		foodOverlayRenderer.enabled = sprite != null;
	}

	private float GetRecipeProcessTime(ProcessingRecipe recipe)
	{
		if (recipe != null && recipe.processTime > 0f) return recipe.processTime;
		return Mathf.Max(0.01f, defaultCookingTime);
	}

	private bool CanAcceptItem(Item item)
	{
		if (item == null) return false;
		if (state == GrillState.Cooked || state == GrillState.Cooking) return false;

		if (item.name == fuelItemName) return true;

		if (recipeBook == null) return false;

		if (loadedInputs.Count == 0)
		{
			return recipeBook.FindMatch(ProcessingRecipe.ProcessorType.Grill, new[] { item }) != null;
		}

		var test = new List<Item>(loadedInputs) { item };
		return recipeBook.FindMatch(ProcessingRecipe.ProcessorType.Grill, test) != null;
	}

	private void TryStartCookingIfReady()
	{
		if (state != GrillState.Loaded) return;
		if (!hasFuel) return;
		if (activeRecipe == null || pendingOutputItem == null) return;

		state = GrillState.Cooking;

		cookTotal = Mathf.Max(0.01f, cookTotal <= 0f ? GetRecipeProcessTime(activeRecipe) : cookTotal);
		cookRemaining = Mathf.Clamp(cookRemaining <= 0f ? cookTotal : cookRemaining, 0f, cookTotal);

		StartCookingVisuals();
		cookCoroutine = StartCoroutine(CookRoutine());
		audioManager.PlaySound("PlaceSound");
	}

	private void StartCookingVisuals()
	{
		currentFrameIndex = 0;
		frameTimer = 0f;

		if (cookingAnimationFrames != null && cookingAnimationFrames.Length > 0)
			spriteRenderer.sprite = cookingAnimationFrames[0];
		else
			UpdateBaseSprite();

		SetFoodOverlay(activeRecipe != null ? activeRecipe.inputOverlaySprite : null);

		cookingAudioSource.Play();
		cookingLight.enabled = true;

		smokeParticleSystem.Play();
		emissionCoroutine = StartCoroutine(IncreaseEmissionRate());
	}

	private void StopCookingVisuals()
	{
		cookingAudioSource.Stop();

		cookingLight.enabled = false;
		cookingLight.pointLightOuterRadius = 0f;

		smokeParticleSystem.Stop();
		if (emissionCoroutine != null)
		{
			StopCoroutine(emissionCoroutine);
			emissionCoroutine = null;
		}

		UpdateBaseSprite();
	}

	private System.Collections.IEnumerator CookRoutine()
	{
		float total = Mathf.Max(0.01f, cookTotal <= 0f ? defaultCookingTime : cookTotal);
		float remaining = Mathf.Clamp(cookRemaining <= 0f ? total : cookRemaining, 0f, total);

		while (remaining > 0f)
		{
			remaining -= Time.deltaTime;
			cookRemaining = remaining;

			float elapsed = total - remaining;

			float radiusT = radiusGrowDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / radiusGrowDuration);
			cookingLight.pointLightOuterRadius = Mathf.Lerp(0f, maxOuterRadius, radiusT);

			yield return null;
		}

		cookRemaining = 0f;

		state = GrillState.Cooked;

		SetFoodOverlay(activeRecipe != null ? activeRecipe.outputOverlaySprite : null);

		ConsumeFuel();

		audioManager.PlaySound("CookingDone");

		StopCookingVisuals();

		cookCoroutine = null;
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

	private void ConsumeFuel()
	{
		if (!hasFuel) return;

		remainingFuelUses--;
		if (remainingFuelUses <= 0)
		{
			hasFuel = false;
			remainingFuelUses = 0;
		}

		UpdateBaseSprite();
	}

	private bool CanAddFuel()
	{
		return !hasFuel;
	}

	private void AddFuel()
	{
		hasFuel = true;
		remainingFuelUses = fuelUses;
		UpdateBaseSprite();
	}

	private void RecomputeActiveRecipe()
	{
		if (recipeBook == null)
		{
			activeRecipe = null;
			pendingOutputItem = null;
			return;
		}

		activeRecipe = recipeBook.FindMatch(ProcessingRecipe.ProcessorType.Grill, loadedInputs);

		if (activeRecipe != null && activeRecipe.output != null)
		{
			pendingOutputItem = activeRecipe.output;
			cookTotal = GetRecipeProcessTime(activeRecipe);
		}
		else
		{
			pendingOutputItem = null;
			cookTotal = 0f;
		}
	}

	public void Interact()
	{
		Item selectedItem = inventoryManager?.GetSelectedItem();

		if (state == GrillState.Cooked)
		{
			if (pendingOutputItem != null && inventoryManager.AddItem(pendingOutputItem, 1))
			{
				pendingOutputItem = null;
				activeRecipe = null;
				loadedInputs.Clear();
				cookRemaining = 0f;
				cookTotal = 0f;

				state = GrillState.Idle;
				SetFoodOverlay(null);
				UpdateBaseSprite();

				audioManager.PlaySound("ItemPickup");
			}
			return;
		}

		if (selectedItem == null) return;

		if (selectedItem.name == fuelItemName)
		{
			if (hasFuel) return;

			inventoryManager.RemoveItem(selectedItem.name, 1);
			AddFuel();
			audioManager.PlaySound("PlaceSound");
			TryStartCookingIfReady();
			return;
		}

		if (state == GrillState.Idle || state == GrillState.Loaded)
		{
			if (!CanAcceptItem(selectedItem)) return;

			inventoryManager.RemoveItem(selectedItem.name, 1);
			loadedInputs.Add(selectedItem);

			state = GrillState.Loaded;

			RecomputeActiveRecipe();

			SetFoodOverlay(activeRecipe != null ? activeRecipe.inputOverlaySprite : null);
			UpdateBaseSprite();

			audioManager.PlaySound("PlaceSound");

			cookRemaining = cookTotal > 0f ? cookTotal : GetRecipeProcessTime(activeRecipe);

			TryStartCookingIfReady();
			return;
		}
	}

	public void SetHighlighted(bool highlight)
	{
		if (outlineRenderer == null) return;

		Item selectedItem = inventoryManager?.GetSelectedItem();
		bool canHighlight = false;

		if (state == GrillState.Cooked)
		{
			canHighlight = true;
		}
		else if (selectedItem != null)
		{
			if (selectedItem.name == fuelItemName)
			{
				canHighlight = !hasFuel;
			}
			else if (state == GrillState.Idle || state == GrillState.Loaded)
			{
				canHighlight = CanAcceptItem(selectedItem);
			}
		}

		outlineRenderer.enabled = highlight && canHighlight;
	}

	public float GetInteractionRange() => 2f;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		if (spriteRenderer == null || spriteRenderer.sprite == null) return false;

		Bounds b = spriteRenderer.bounds;
		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}

	private void ApplyRestoredVisuals()
	{
		UpdateBaseSprite();

		if (state == GrillState.Cooked)
		{
			SetFoodOverlay(activeRecipe != null ? activeRecipe.outputOverlaySprite : null);
		}
		else if (state == GrillState.Cooking || state == GrillState.Loaded)
		{
			SetFoodOverlay(activeRecipe != null ? activeRecipe.inputOverlaySprite : null);
		}
		else
		{
			SetFoodOverlay(null);
		}
	}

	private void OnDisable()
	{
		if (cookCoroutine != null)
		{
			StopCoroutine(cookCoroutine);
			cookCoroutine = null;
		}

		if (emissionCoroutine != null)
		{
			StopCoroutine(emissionCoroutine);
			emissionCoroutine = null;
		}
	}

	public string SaveKey => "Grill";

	[Serializable]
	private class SavedStack
	{
		public string itemId;
		public int amount;
	}

	[Serializable]
	private class GrillStateData
	{
		public int state;
		public bool hasFuel;
		public int remainingFuelUses;
		public List<SavedStack> inputs;
		public string outputItemId;
		public float cookRemaining;
		public float cookTotal;
	}

	public string CaptureStateJson()
	{
		var grouped = loadedInputs
			.Where(i => i != null)
			.GroupBy(i => i.name)
			.Select(g => new SavedStack { itemId = g.Key, amount = g.Count() })
			.ToList();

		return JsonUtility.ToJson(new GrillStateData
		{
			state = (int)state,
			hasFuel = hasFuel,
			remainingFuelUses = remainingFuelUses,
			inputs = grouped,
			outputItemId = pendingOutputItem != null ? pendingOutputItem.name : null,
			cookRemaining = cookRemaining,
			cookTotal = cookTotal
		});
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var st = JsonUtility.FromJson<GrillStateData>(json);
		if (st == null) return;

		state = (GrillState)st.state;
		hasFuel = st.hasFuel;
		remainingFuelUses = st.remainingFuelUses;
		cookRemaining = Mathf.Max(0f, st.cookRemaining);
		cookTotal = Mathf.Max(0f, st.cookTotal);

		pendingOutputItem = null;
		activeRecipe = null;
		loadedInputs.Clear();

		var db = FindAnyObjectByType<SaveGameManager>()?.itemDatabase;

		if (db != null && st.inputs != null)
		{
			foreach (var s in st.inputs)
			{
				if (s == null || string.IsNullOrEmpty(s.itemId) || s.amount <= 0) continue;
				var item = db.Get(s.itemId);
				if (item == null) continue;
				for (int i = 0; i < s.amount; i++) loadedInputs.Add(item);
			}

			if (!string.IsNullOrEmpty(st.outputItemId))
				pendingOutputItem = db.Get(st.outputItemId);
		}

		RecomputeActiveRecipe();

		if (pendingOutputItem == null && activeRecipe != null && activeRecipe.output != null)
			pendingOutputItem = activeRecipe.output;

		if (cookTotal <= 0f && activeRecipe != null)
			cookTotal = GetRecipeProcessTime(activeRecipe);

		if (cookRemaining <= 0f && (state == GrillState.Loaded || state == GrillState.Cooking))
			cookRemaining = cookTotal > 0f ? cookTotal : defaultCookingTime;

		if (cookCoroutine != null)
		{
			StopCoroutine(cookCoroutine);
			cookCoroutine = null;
		}

		ApplyRestoredVisuals();

		if (state == GrillState.Loaded)
		{
			TryStartCookingIfReady();
		}
	}
}

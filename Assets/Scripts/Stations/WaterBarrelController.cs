using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WaterBarrelController : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Fill Sprites")]
	public Sprite emptySprite;
	public Sprite oneThirdSprite;
	public Sprite twoThirdSprite;
	public Sprite fullSprite;

	[Header("Outline")]
	[Tooltip("SpriteRenderer for the white outline overlay (child GameObject)")]
	public SpriteRenderer outlineRenderer;

	[Header("Fill Settings")]
	[Tooltip("Total time (in seconds) for the barrel to go from empty to full under continuous rain.")]
	public float fillDuration = 10f;
	public int fillsPerStage = 3;
	public int maxStages = 3;

	[Header("Current State")]
	[Range(0, 3)]
	[Tooltip("0: Empty (0 fills), 1: 1/3 (1-3 fills), 2: 2/3 (4-6 fills), 3: Full (7-9 fills)")]
	public int currentStage = 0;
	private int currentFills;

	public float interactionRange = 2f;

	private SpriteRenderer spriteRenderer;
	private InventoryManager inventoryManager;
	private WeatherManager weatherManager;
	private GameObject outlineChild;
	private Coroutine fillCoroutine;
	private bool initialized;
	private bool restoredFromSave;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void EnsureInit()
	{
		if (initialized) return;

		if (spriteRenderer == null)
			spriteRenderer = GetComponent<SpriteRenderer>();

		inventoryManager = GameManager.Instance?.InventoryManager;
		weatherManager = FindFirstObjectByType<WeatherManager>();

		InteractableUtil.ResolveOutlineRenderer(transform, ref outlineRenderer, out outlineChild);

		initialized = true;
	}

	private void Start()
	{
		EnsureInit();

		if (!restoredFromSave)
		{
			currentStage = 0;
			currentFills = 0;
			UpdateSprite();
		}
		else
		{
			UpdateStageFromFills();
			UpdateSprite();
		}

		if (inventoryManager == null || weatherManager == null)
		{
			enabled = false;
			return;
		}

		if (weatherManager.IsRainActive() && fillCoroutine == null && currentFills < maxStages * fillsPerStage)
		{
			fillCoroutine = StartCoroutine(FillRoutine());
		}
	}

	private void Update()
	{
		if (weatherManager == null) return;

		if (weatherManager.IsRainActive() && fillCoroutine == null && currentFills < maxStages * fillsPerStage)
			fillCoroutine = StartCoroutine(FillRoutine());
		else if (!weatherManager.IsRainActive() && fillCoroutine != null)
		{
			StopCoroutine(fillCoroutine);
			fillCoroutine = null;
		}
	}

	private IEnumerator FillRoutine()
	{
		float timePerFill = fillDuration / (maxStages * fillsPerStage);

		while (currentFills < maxStages * fillsPerStage)
		{
			if (weatherManager != null && !weatherManager.IsRainActive())
				break;

			yield return new WaitForSeconds(timePerFill);

			if (weatherManager != null && !weatherManager.IsRainActive())
				break;

			currentFills++;
			UpdateStageFromFills();
			UpdateSprite();
		}

		fillCoroutine = null;
	}

	private void UpdateSprite()
	{
		if (spriteRenderer == null) return;

		switch (currentStage)
		{
			case 0:
				spriteRenderer.sprite = emptySprite;
				break;
			case 1:
				spriteRenderer.sprite = oneThirdSprite;
				break;
			case 2:
				spriteRenderer.sprite = twoThirdSprite;
				break;
			case 3:
				spriteRenderer.sprite = fullSprite;
				break;
			default:
				spriteRenderer.sprite = emptySprite;
				break;
		}
	}

	private void UpdateStageFromFills()
	{
		if (currentFills >= 7)
			currentStage = 3;
		else if (currentFills >= 4)
			currentStage = 2;
		else if (currentFills >= 1)
			currentStage = 1;
		else
			currentStage = 0;
	}

	public void SetHighlighted(bool highlight)
	{
		EnsureInit();

		if (outlineRenderer == null)
			return;

		Item selectedItem = inventoryManager?.GetSelectedItem();
		bool isFillableItem = selectedItem != null &&
							  currentFills > 0 &&
							  (selectedItem.name == ItemNames.Cup ||
							   selectedItem.name == ItemNames.WaterBottle ||
							   selectedItem.name == ItemNames.Canteen);

		outlineRenderer.enabled = highlight && isFillableItem;
	}

	public void Interact()
	{
		EnsureInit();

		if (inventoryManager == null) return;
		if (currentFills <= 0) return;

		Item selectedItem = inventoryManager.GetSelectedItem();
		if (selectedItem == null) return;

		if (selectedItem.name != ItemNames.Cup && selectedItem.name != ItemNames.WaterBottle && selectedItem.name != ItemNames.Canteen)
			return;

		if (inventoryManager.inventorySlots == null) return;

		int slotsCount = (inventoryManager.inventorySlots as ICollection)?.Count ?? 0;
		if (slotsCount <= 0) return;

		int idx = inventoryManager.SelectedSlotIndex;
		if (idx < 0 || idx >= slotsCount) return;

		var slotGo = inventoryManager.inventorySlots[idx];
		if (slotGo == null) return;

		WaterContainerInventoryItem slotItem = slotGo.CurrentItem as WaterContainerInventoryItem;
		if (slotItem == null || slotItem.item == null) return;
		if (slotItem.currentFill >= slotItem.item.maxFillCapacity) return;

		int fillsNeeded = slotItem.item.maxFillCapacity - slotItem.currentFill;
		int fillsToTransfer = Mathf.Min(currentFills, fillsNeeded);
		if (fillsToTransfer <= 0) return;

		slotItem.Fill(fillsToTransfer, false);
		currentFills -= fillsToTransfer;

		UpdateStageFromFills();
		UpdateSprite();
	}

	public float GetInteractionRange()
	{
		return interactionRange;
	}

	public Transform GetTransform()
	{
		return transform;
	}

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		return InteractableUtil.IsMouseOverBounds(spriteRenderer, mouseWorldPos);
	}

	public string SaveKey => "WaterBarrel";

	[System.Serializable]
	private class BarrelState
	{
		public int currentFills;
		public int currentStage;
	}

	public string CaptureStateJson()
	{
		return JsonUtility.ToJson(new BarrelState
		{
			currentFills = currentFills,
			currentStage = currentStage
		});
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		EnsureInit();

		var st = JsonUtility.FromJson<BarrelState>(json);
		if (st == null) return;

		restoredFromSave = true;

		currentFills = st.currentFills;

		UpdateStageFromFills();

		if (fillCoroutine != null)
		{
			StopCoroutine(fillCoroutine);
			fillCoroutine = null;
		}

		UpdateSprite();

		if (weatherManager != null && weatherManager.IsRainActive() &&
			currentFills < maxStages * fillsPerStage)
		{
			fillCoroutine = StartCoroutine(FillRoutine());
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Planter : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Fill Sprites")]
	public Sprite drySprite;
	public Sprite wetSprite;

	[Header("Outline")]
	public SpriteRenderer outlineRenderer;

	[Header("Crop Overlay")]
	public SpriteRenderer cropOverlayRenderer;

	[Header("Growth Settings")]
	public float growthTime = 60f;

	[Header("Plantable Crops")]
	public string[] plantableCropNames = { "Carrot", "Onion", "Potato" };

	[Header("Crop Data")]
	[SerializeField] private CropData[] cropDataArray;

	public float interactionRange = 2f;

	private float growthTimer;
	private bool isWet;
	private bool isPlanted;
	private bool isMature;
	private int yieldAmount;
	private Item currentCrop;

	private SpriteRenderer spriteRenderer;
	private InventoryManager inventoryManager;
	private AudioManager audioManager;
	private WeatherManager weatherManager;

	private GameObject outlineChild;
	private Coroutine growthCoroutine;
	private bool restoredFromSave;

	private Dictionary<string, CropData> cropDataMap;
	private HashSet<string> plantableSet;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		BuildCropMaps();
		EnsureChildRenderers();
	}

	private void Start()
	{
		EnsureChildRenderers();

		inventoryManager = GameManager.Instance?.InventoryManager;
		audioManager = GameManager.Instance?.AudioManager;

		weatherManager = FindAnyObjectByType<WeatherManager>();

		if (inventoryManager == null || audioManager == null || weatherManager == null)
		{
			enabled = false;
			return;
		}

		if (!restoredFromSave)
		{
			isWet = false;
			isPlanted = false;
			isMature = false;
			growthTimer = 0f;
			currentCrop = null;
			yieldAmount = 0;

			if (spriteRenderer != null) spriteRenderer.sprite = drySprite;
			if (cropOverlayRenderer != null) cropOverlayRenderer.sprite = null;
		}
		else
		{
			if (spriteRenderer != null) spriteRenderer.sprite = isWet ? wetSprite : drySprite;
			UpdateCropOverlaySprite();

			if (growthCoroutine != null)
			{
				StopCoroutine(growthCoroutine);
				growthCoroutine = null;
			}

			if (isWet && isPlanted && !isMature)
				growthCoroutine = StartCoroutine(GrowthRoutine());
		}
	}

	private void Update()
	{
		if (weatherManager == null) return;

		if (weatherManager.IsRainActive() && !isWet)
		{
			isWet = true;
			if (spriteRenderer != null) spriteRenderer.sprite = wetSprite;

			if (isPlanted && !isMature && growthCoroutine == null)
				growthCoroutine = StartCoroutine(GrowthRoutine());
		}
		else if (!weatherManager.IsRainActive() && isWet && !isPlanted)
		{
			isWet = false;
			if (spriteRenderer != null) spriteRenderer.sprite = drySprite;
		}
	}

	private void BuildCropMaps()
	{
		plantableSet = new HashSet<string>(plantableCropNames);

		cropDataMap = new Dictionary<string, CropData>(cropDataArray != null ? cropDataArray.Length : 0);
		if (cropDataArray == null) return;

		foreach (var data in cropDataArray)
		{
			if (data == null) continue;
			if (string.IsNullOrEmpty(data.cropName)) continue;
			if (data.growthSprites == null || data.growthSprites.Length != 3) continue;

			cropDataMap[data.cropName] = data;
		}
	}

	private void EnsureChildRenderers()
	{
		if (spriteRenderer == null)
			spriteRenderer = GetComponent<SpriteRenderer>();

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

		if (cropOverlayRenderer == null)
		{
			var existing = transform.Find("CropOverlay");
			if (existing != null) cropOverlayRenderer = existing.GetComponent<SpriteRenderer>();

			if (cropOverlayRenderer == null)
			{
				var cropOverlay = new GameObject("CropOverlay");
				cropOverlay.transform.SetParent(transform);
				cropOverlay.transform.localPosition = Vector3.zero;
				cropOverlay.transform.localRotation = Quaternion.identity;
				cropOverlay.transform.localScale = Vector3.one;

				cropOverlayRenderer = cropOverlay.AddComponent<SpriteRenderer>();
			}
		}

		if (spriteRenderer != null)
		{
			spriteRenderer.sortingOrder = 0;

			if (cropOverlayRenderer != null)
				cropOverlayRenderer.sortingOrder = 1;

			if (outlineRenderer != null)
				outlineRenderer.sortingOrder = 2;
		}
	}

	private IEnumerator GrowthRoutine()
	{
		while (growthTimer < growthTime)
		{
			if (!isWet)
			{
				growthCoroutine = null;
				yield break;
			}

			growthTimer += Time.deltaTime;
			UpdateCropOverlaySprite();
			yield return null;
		}

		isMature = true;
		yieldAmount = Random.Range(1, 4);
		UpdateCropOverlaySprite();
		growthCoroutine = null;
	}

	public void Plant(Item crop)
	{
		if (crop == null) return;
		if (isPlanted) return;
		if (plantableSet == null || !plantableSet.Contains(crop.name)) return;

		currentCrop = crop;
		isPlanted = true;
		isMature = false;
		growthTimer = 0f;

		if (spriteRenderer != null) spriteRenderer.sprite = isWet ? wetSprite : drySprite;
		audioManager?.PlaySound("PlaceSound");

		UpdateCropOverlaySprite();

		if (isWet && growthCoroutine == null)
			growthCoroutine = StartCoroutine(GrowthRoutine());
	}

	public void Water()
	{
		if (isWet) return;

		isWet = true;
		if (spriteRenderer != null) spriteRenderer.sprite = wetSprite;

		audioManager?.PlaySound("WaterFill");

		if (isPlanted && !isMature && growthCoroutine == null)
			growthCoroutine = StartCoroutine(GrowthRoutine());

		UpdateCropOverlaySprite();
	}

	public void Harvest()
	{
		if (!isMature) return;
		if (currentCrop == null) return;

		if (inventoryManager != null && inventoryManager.AddItem(currentCrop, yieldAmount))
			ResetPlanter();
	}

	private void ResetPlanter()
	{
		isWet = weatherManager != null && weatherManager.IsRainActive();
		if (spriteRenderer != null) spriteRenderer.sprite = isWet ? wetSprite : drySprite;

		isPlanted = false;
		isMature = false;
		growthTimer = 0f;
		currentCrop = null;
		yieldAmount = 0;

		if (cropOverlayRenderer != null) cropOverlayRenderer.sprite = null;

		if (growthCoroutine != null)
		{
			StopCoroutine(growthCoroutine);
			growthCoroutine = null;
		}
	}

	private void UpdateCropOverlaySprite()
	{
		if (cropOverlayRenderer == null) return;
		if (!isPlanted || currentCrop == null || cropDataMap == null) { cropOverlayRenderer.sprite = null; return; }
		if (!cropDataMap.TryGetValue(currentCrop.name, out var cropData)) { cropOverlayRenderer.sprite = null; return; }

		var sprites = cropData.growthSprites;
		if (sprites == null || sprites.Length != 3) { cropOverlayRenderer.sprite = null; return; }

		Sprite target =
			isMature ? sprites[2] :
			sprites[(growthTimer / growthTime) < 0.5f ? 0 : 1];

		if (cropOverlayRenderer.sprite != target)
			cropOverlayRenderer.sprite = target;
	}

	public void SetHighlighted(bool highlight)
	{
		if (outlineRenderer == null) return;

		var selectedItem = inventoryManager?.GetSelectedItem();
		var slotItem = inventoryManager?.inventorySlots[inventoryManager.SelectedSlotIndex]
			.GetComponentInChildren<WaterContainerInventoryItem>();

		bool canHighlight = isMature || (selectedItem != null && (
			(!isPlanted && plantableSet != null && plantableSet.Contains(selectedItem.name)) ||
			(!isWet && (selectedItem.name == "Cup" || selectedItem.name == "Water Bottle" || selectedItem.name == "Canteen") &&
			 slotItem != null && slotItem.currentFill > 0 && !slotItem.isSaltWater)
		));

		outlineRenderer.enabled = highlight && canHighlight;
	}

	public void Interact()
	{
		var selectedItem = inventoryManager?.GetSelectedItem();
		var slotItem = inventoryManager?.inventorySlots[inventoryManager.SelectedSlotIndex]
			.GetComponentInChildren<WaterContainerInventoryItem>();

		if (isMature)
		{
			Harvest();
			return;
		}

		if (selectedItem == null) return;

		if (!isPlanted && plantableSet != null && plantableSet.Contains(selectedItem.name))
		{
			Plant(selectedItem);
			inventoryManager.RemoveItem(selectedItem.name, 1);
			return;
		}

		if (!isWet && (selectedItem.name == "Cup" || selectedItem.name == "Water Bottle" || selectedItem.name == "Canteen"))
		{
			if (slotItem != null && slotItem.currentFill > 0 && !slotItem.isSaltWater)
			{
				Water();
				slotItem.Drink();
			}
		}
	}

	public float GetInteractionRange() => interactionRange;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		if (spriteRenderer == null || spriteRenderer.sprite == null) return false;

		var b = spriteRenderer.bounds;
		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}

	public string SaveKey => "Planter";

	[System.Serializable]
	private class PlanterState
	{
		public bool isWet;
		public bool isPlanted;
		public bool isMature;
		public float growthTimer;
		public int yieldAmount;
		public string cropItemId;
	}

	public string CaptureStateJson()
	{
		var st = new PlanterState
		{
			isWet = isWet,
			isPlanted = isPlanted,
			isMature = isMature,
			growthTimer = growthTimer,
			yieldAmount = yieldAmount,
			cropItemId = currentCrop != null ? currentCrop.name : null
		};
		return JsonUtility.ToJson(st);
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		restoredFromSave = true;

		var st = JsonUtility.FromJson<PlanterState>(json);
		if (st == null) return;

		isWet = st.isWet;
		isPlanted = st.isPlanted;
		isMature = st.isMature;
		growthTimer = st.growthTimer;
		yieldAmount = st.yieldAmount;

		currentCrop = null;
		if (!string.IsNullOrEmpty(st.cropItemId))
		{
			var db = FindAnyObjectByType<SaveGameManager>()?.itemDatabase;
			if (db != null) currentCrop = db.Get(st.cropItemId);
		}

		EnsureChildRenderers();

		if (spriteRenderer != null)
			spriteRenderer.sprite = isWet ? wetSprite : drySprite;

		UpdateCropOverlaySprite();

		if (growthCoroutine != null)
		{
			StopCoroutine(growthCoroutine);
			growthCoroutine = null;
		}

		if (isWet && isPlanted && !isMature)
			growthCoroutine = StartCoroutine(GrowthRoutine());
	}
}

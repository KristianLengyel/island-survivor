using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WaterTank : MonoBehaviour, IInteractable, ISaveableComponent, IPipeConnectable
{
	[Header("Empty")]
	[SerializeField] private Sprite emptySprite;

	[Header("Salt Water (index 0 = 1 unit, index 9 = 10 units)")]
	[SerializeField] private Sprite[] saltSprites = new Sprite[10];

	[Header("Fresh Water (index 0 = 1 unit, index 9 = 10 units)")]
	[SerializeField] private Sprite[] freshSprites = new Sprite[10];

	[Header("Outline")]
	[SerializeField] private SpriteRenderer outlineRenderer;

	[Header("Settings")]
	[SerializeField] private int maxStoredUnits = 10;

	public float interactionRange = 2f;

	private SpriteRenderer spriteRenderer;
	private InventoryManager inventoryManager;
	private AudioManager audioManager;
	private Vector2Int gridPos;
	private int storedUnits;
	private WaterType storedWaterType;
	private WaterType preferredType;
	private bool restoredFromSave;

	public Vector2Int GridPosition => gridPos;
	public bool IsPump => true;
	public bool IsActive => storedUnits > 0;
	public WaterType OutputWaterType => storedUnits > 0 ? storedWaterType : preferredType;
	public bool IsColorSource => storedUnits > 0 || preferredType != WaterType.None;

	public bool IsWaterTypeCompatible(WaterType type) => preferredType == WaterType.None || preferredType == type;

	public bool CanReceiveWater(WaterType type)
	{
		if (storedUnits >= maxStoredUnits) return false;
		if (preferredType != WaterType.None && preferredType != type) return false;
		if (storedUnits > 0 && storedWaterType != type) return false;
		return true;
	}

	public void ReceiveWaterUnit(WaterType type)
	{
		if (preferredType == WaterType.None)
			preferredType = type;
		storedWaterType = type;
		storedUnits = Mathf.Min(storedUnits + 1, maxStoredUnits);
		PipeNetwork.Instance?.MarkColorsDirty();
		UpdateSprite();
	}

	public bool CanConsumeWater(WaterType type)
	{
		return storedUnits > 0 && storedWaterType == type;
	}

	public void ConsumeWaterUnit()
	{
		storedUnits = Mathf.Max(storedUnits - 1, 0);
		if (storedUnits == 0)
			storedWaterType = WaterType.None;
		PipeNetwork.Instance?.MarkColorsDirty();
		UpdateSprite();
	}

	public string SaveKey => "WaterTank";

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		gridPos = Vector2Int.FloorToInt((Vector2)transform.position);
	}

	private void Start()
	{
		inventoryManager = GameManager.Instance?.InventoryManager;
		audioManager = GameManager.Instance?.AudioManager;

		if (PipeNetwork.Instance != null)
		{
			gridPos = PipeNetwork.Instance.WorldToCell(transform.position);
			PipeNetwork.Instance.Register(this);
		}

		if (!restoredFromSave)
		{
			storedUnits = 0;
			storedWaterType = WaterType.None;
			preferredType = WaterType.None;
		}

		UpdateSprite();
	}

	private void OnDisable()
	{
		if (PipeNetwork.Instance != null)
			PipeNetwork.Instance.Deregister(this);
	}

	public void Interact()
	{
		if (inventoryManager == null) return;
		if (storedUnits <= 0) return;

		Item selectedItem = inventoryManager.GetSelectedItem();
		if (selectedItem == null) return;
		if (selectedItem.name != ItemNames.Cup && selectedItem.name != ItemNames.WaterBottle && selectedItem.name != ItemNames.Canteen) return;

		int idx = inventoryManager.SelectedSlotIndex;
		var slotItem = inventoryManager.inventorySlots[idx].CurrentItem as WaterContainerInventoryItem;
		if (slotItem == null || slotItem.item == null) return;
		if (slotItem.currentFill >= slotItem.item.maxFillCapacity) return;

		int fillsNeeded = slotItem.item.maxFillCapacity - slotItem.currentFill;
		int fillsToTransfer = Mathf.Min(storedUnits, fillsNeeded);
		if (fillsToTransfer <= 0) return;

		slotItem.Fill(fillsToTransfer, storedWaterType == WaterType.SaltWater);
		storedUnits -= fillsToTransfer;
		if (storedUnits == 0)
			storedWaterType = WaterType.None;
		PipeNetwork.Instance?.MarkColorsDirty();
		audioManager?.PlaySound("WaterFill");
		UpdateSprite();
	}

	public void SetHighlighted(bool highlight)
	{
		if (outlineRenderer == null) return;

		Item selectedItem = inventoryManager?.GetSelectedItem();
		bool canFill = storedUnits > 0 && selectedItem != null &&
					   (selectedItem.name == ItemNames.Cup ||
						selectedItem.name == ItemNames.WaterBottle ||
						selectedItem.name == ItemNames.Canteen);

		outlineRenderer.enabled = highlight && canFill;
	}

	public float GetInteractionRange() => interactionRange;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		return InteractableUtil.IsMouseOverBounds(spriteRenderer, mouseWorldPos);
	}

	private void UpdateSprite()
	{
		if (spriteRenderer == null) return;

		if (storedUnits <= 0)
		{
			spriteRenderer.sprite = emptySprite;
			return;
		}

		int idx = Mathf.Clamp(storedUnits - 1, 0, 9);
		Sprite[] sprites = storedWaterType == WaterType.SaltWater ? saltSprites : freshSprites;
		if (sprites != null && idx < sprites.Length && sprites[idx] != null)
			spriteRenderer.sprite = sprites[idx];
	}

	[System.Serializable]
	private class TankState
	{
		public int storedUnits;
		public WaterType storedWaterType;
		public WaterType preferredType;
	}

	public string CaptureStateJson()
	{
		return JsonUtility.ToJson(new TankState
		{
			storedUnits = storedUnits,
			storedWaterType = storedWaterType,
			preferredType = preferredType
		});
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var st = JsonUtility.FromJson<TankState>(json);
		if (st == null) return;

		restoredFromSave = true;
		storedUnits = st.storedUnits;
		storedWaterType = st.storedWaterType;
		preferredType = st.preferredType;
		if (preferredType == WaterType.None && storedWaterType != WaterType.None)
			preferredType = storedWaterType;
		UpdateSprite();
	}
}

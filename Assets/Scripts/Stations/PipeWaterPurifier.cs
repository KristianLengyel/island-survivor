using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PipeWaterPurifier : MonoBehaviour, IInteractable, ISaveableComponent, IPipeConnectable
{
	[Header("Sprites")]
	[SerializeField] private Sprite activeSprite;
	[SerializeField] private Sprite inactiveSprite;

	[Header("Outline")]
	[SerializeField] private SpriteRenderer outlineRenderer;

	[Header("Settings")]
	[SerializeField] private float purifyInterval = 7f;

	public float interactionRange = 2f;

	private SpriteRenderer spriteRenderer;
	private InventoryManager inventoryManager;
	private AudioManager audioManager;
	private Vector2Int gridPos;
	private float purifyTimer;
	private bool hasPurifiedWater;
	private bool isReceivingSaltWater;
	private bool restoredFromSave;

	public Vector2Int GridPosition => gridPos;
	public bool IsPump => true;
	public bool IsActive => isReceivingSaltWater;
	public WaterType OutputWaterType => WaterType.FreshWater;

	public bool IsColorSource => false;
	public bool IsWaterTypeCompatible(WaterType type) => false;
	public bool CanReceiveWater(WaterType type) => false;
	public void ReceiveWaterUnit(WaterType type) { }
	public bool CanConsumeWater(WaterType type) => false;
	public void ConsumeWaterUnit() { }

	public string SaveKey => "PipeWaterPurifier";

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
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
		else
		{
			gridPos = Vector2Int.RoundToInt((Vector2)transform.position);
		}

		if (!restoredFromSave)
		{
			purifyTimer = 0f;
			hasPurifiedWater = false;
		}

		UpdateSprite();
	}

	private void OnDisable()
	{
		if (PipeNetwork.Instance != null)
			PipeNetwork.Instance.Deregister(this);
	}

	private void Update()
	{
		bool wasReceiving = isReceivingSaltWater;
		isReceivingSaltWater = PipeNetwork.Instance != null &&
			PipeNetwork.Instance.HasConsumableWater(gridPos, WaterType.SaltWater, this);

		if (wasReceiving != isReceivingSaltWater)
		{
			if (!isReceivingSaltWater)
				purifyTimer = 0f;
			PipeNetwork.Instance?.MarkColorsDirty();
			UpdateSprite();
		}

		if (!isReceivingSaltWater) return;
		if (hasPurifiedWater) return;

		purifyTimer += Time.deltaTime;
		if (purifyTimer < purifyInterval) return;
		purifyTimer -= purifyInterval;

		if (!PipeNetwork.Instance.TryConsumeWaterUnit(gridPos, WaterType.SaltWater, out Vector2Int consumedFrom, this))
			return;

		bool pushed = PipeNetwork.Instance.TryPushWaterUnit(gridPos, WaterType.FreshWater, consumedFrom, this);
		if (!pushed)
			hasPurifiedWater = true;

		audioManager?.PlaySound("PurificationDone");
		PipeNetwork.Instance?.MarkColorsDirty();
		UpdateSprite();
	}

	public void Interact()
	{
		if (!hasPurifiedWater) return;
		if (inventoryManager == null) return;

		Item selectedItem = inventoryManager.GetSelectedItem();
		if (selectedItem == null) return;
		if (selectedItem.name != ItemNames.Cup && selectedItem.name != ItemNames.WaterBottle && selectedItem.name != ItemNames.Canteen) return;

		int idx = inventoryManager.SelectedSlotIndex;
		var slotItem = inventoryManager.inventorySlots[idx].CurrentItem as WaterContainerInventoryItem;
		if (slotItem == null || slotItem.item == null) return;
		if (slotItem.currentFill >= slotItem.item.maxFillCapacity) return;

		slotItem.Fill(1, false);
		hasPurifiedWater = false;
		purifyTimer = 0f;
		audioManager?.PlaySound("WaterFill");
		UpdateSprite();
	}

	public void SetHighlighted(bool highlight)
	{
		if (outlineRenderer == null) return;

		Item selectedItem = inventoryManager?.GetSelectedItem();
		bool canCollect = hasPurifiedWater && selectedItem != null &&
						  (selectedItem.name == ItemNames.Cup ||
						   selectedItem.name == ItemNames.WaterBottle ||
						   selectedItem.name == ItemNames.Canteen);

		outlineRenderer.enabled = highlight && canCollect;
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
		spriteRenderer.sprite = (isReceivingSaltWater || hasPurifiedWater) ? activeSprite : inactiveSprite;
	}

	[System.Serializable]
	private class SaveState
	{
		public float purifyTimer;
		public bool hasPurifiedWater;
	}

	public string CaptureStateJson()
	{
		return JsonUtility.ToJson(new SaveState
		{
			purifyTimer = purifyTimer,
			hasPurifiedWater = hasPurifiedWater
		});
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var st = JsonUtility.FromJson<SaveState>(json);
		if (st == null) return;

		restoredFromSave = true;
		purifyTimer = st.purifyTimer;
		hasPurifiedWater = st.hasPurifiedWater;
		UpdateSprite();
	}
}

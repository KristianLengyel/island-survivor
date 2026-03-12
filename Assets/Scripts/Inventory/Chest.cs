using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Chest UI")]
	[SerializeField] private GameObject chestUI;
	[SerializeField] private InventorySlot[] chestSlots;

	[Header("Sprites")]
	public Sprite normalSprite;
	public Sprite highlightedSprite;

	public string SaveKey => "Chest";

	private SpriteRenderer spriteRenderer;
	private bool isOpen = false;
	private InventoryContainer container;

	public float interactionRange = 2f;

	public static Chest CurrentOpenChest { get; private set; }

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();

		if (chestUI == null)
		{
			Transform uiTransform = transform.Find("ChestUI");
			if (uiTransform != null)
				chestUI = uiTransform.gameObject;
		}

		if ((chestSlots == null || chestSlots.Length == 0) && chestUI != null)
			chestSlots = chestUI.GetComponentsInChildren<InventorySlot>(true);
	}

	private void Start()
	{
		if (chestUI != null) chestUI.SetActive(false);

		if (spriteRenderer != null && normalSprite != null)
			spriteRenderer.sprite = normalSprite;

		container = GameManager.Instance.InventoryManager.CreateContainer(chestSlots);
	}

	private void Update()
	{
		if (isOpen && GameInput.CancelDown)
		{
			GameInput.ConsumeCancel();
			ToggleChest();
		}
	}

	public void ToggleChest()
	{
		if (!isOpen && CurrentOpenChest != null && CurrentOpenChest != this)
			CurrentOpenChest.ToggleChest();

		isOpen = !isOpen;

		if (chestUI != null) chestUI.SetActive(isOpen);

		if (isOpen)
		{
			CurrentOpenChest = this;
			GameManager.Instance.InventoryManager.InventoryLocked = true;
			GameManager.Instance.InventoryManager.ShowInventory(true, fromChest: true);
		}
		else
		{
			if (CurrentOpenChest == this) CurrentOpenChest = null;
			GameManager.Instance.InventoryManager.InventoryLocked = false;
			GameManager.Instance.InventoryManager.ShowInventory(false, fromChest: true);
			if (Tooltip.Instance != null) Tooltip.Instance.HideTooltip();
		}
	}

	public bool TryStoreInventoryItem(InventoryItem sourceItem) => container.TryStoreInventoryItem(sourceItem);

	public int AddItemPartial(Item item, int count) => container.AddItemPartial(item, count);

	public bool AddItem(Item item, int count = 1) => container.AddItem(item, count);

	public void RemoveItem(string itemName, int count) => container.RemoveItem(itemName, count);

	public bool HasAnyItems()
	{
		if (chestSlots == null) return false;
		foreach (var slot in chestSlots)
			if (slot != null && slot.GetComponentInChildren<InventoryItem>() != null)
				return true;
		return false;
	}

	public void SetHighlighted(bool highlight)
	{
		if (spriteRenderer != null)
			spriteRenderer.sprite = highlight ? highlightedSprite : normalSprite;
	}

	public bool IsOpen() => isOpen;

	public void Interact() => ToggleChest();

	public float GetInteractionRange() => interactionRange;

	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		if (spriteRenderer == null || spriteRenderer.sprite == null) return false;
		Bounds b = spriteRenderer.bounds;
		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}

	public SpriteRenderer GetSpriteRenderer() => spriteRenderer;

	[System.Serializable]
	private class ChestState
	{
		public List<InventorySlotData> slots = new List<InventorySlotData>();
	}

	public string CaptureStateJson()
	{
		var state = new ChestState { slots = container.CaptureState() };
		return JsonUtility.ToJson(state);
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var state = JsonUtility.FromJson<ChestState>(json);
		if (state == null) return;

		var saveMgr = FindAnyObjectByType<SaveGameManager>();
		if (saveMgr == null || saveMgr.itemDatabase == null) return;

		container.RestoreState(state.slots, saveMgr.itemDatabase);
	}
}

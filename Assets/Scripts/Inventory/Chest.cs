using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable, ISaveableComponent
{
	private const int chestSlotCount = 21;

	[Header("Sprites")]
	public Sprite normalSprite;
	public Sprite highlightedSprite;

	public string SaveKey => "Chest";

	private SpriteRenderer spriteRenderer;
	private bool isOpen = false;
	private InventoryContainer container;
	private InventorySlot[] chestSlots;

	public InventorySlot[] ChestSlots => chestSlots;

	private void EnsureContainer()
	{
		if (container == null)
			container = GameManager.Instance.InventoryManager.CreateContainer(chestSlots);
	}

	public float interactionRange = 2f;

	public static Chest CurrentOpenChest { get; private set; }

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();

		chestSlots = new InventorySlot[chestSlotCount];
		for (int i = 0; i < chestSlotCount; i++)
			chestSlots[i] = new InventorySlot();
	}

	private void Start()
	{
		if (spriteRenderer != null && normalSprite != null)
			spriteRenderer.sprite = normalSprite;

		EnsureContainer();
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

		if (isOpen)
		{
			CurrentOpenChest = this;
			GameManager.Instance.InventoryManager.InventoryLocked = true;
			GameManager.Instance.InventoryManager.ShowInventory(true, fromChest: true);
			GameManager.Instance.InventoryManager.OpenChestPanel(this);
		}
		else
		{
			if (CurrentOpenChest == this) CurrentOpenChest = null;
			GameManager.Instance.InventoryManager.CloseChestPanel(this);
			GameManager.Instance.InventoryManager.InventoryLocked = false;
			GameManager.Instance.InventoryManager.ShowInventory(false, fromChest: true);
			Tooltip.Instance?.HideTooltip();
		}
	}

	public bool TryStoreInventoryItem(InventoryItem sourceItem) { EnsureContainer(); return container.TryStoreInventoryItem(sourceItem); }

	public int AddItemPartial(Item item, int count) { EnsureContainer(); return container.AddItemPartial(item, count); }

	public bool AddItem(Item item, int count = 1) { EnsureContainer(); return container.AddItem(item, count); }

	public void RemoveItem(string itemName, int count) { EnsureContainer(); container.RemoveItem(itemName, count); }

	public bool HasAnyItems()
	{
		if (chestSlots == null) return false;
		foreach (var slot in chestSlots)
			if (slot != null && !slot.IsEmpty) return true;
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
		EnsureContainer();
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

		EnsureContainer();
		container.RestoreState(state.slots, saveMgr.itemDatabase);
	}
}

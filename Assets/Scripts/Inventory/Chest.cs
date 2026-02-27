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

	public float interactionRange = 2f;
	private Transform playerTransform;

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

		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null) playerTransform = player.transform;
	}

	private void Update()
	{
		if (isOpen && GameInput.CancelDown)
		{
			GameInput.ConsumeCancel();
			ToggleChest();
			return;
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

	private InventorySlot GetEmptyChestSlot()
	{
		for (int i = 0; i < chestSlots.Length; i++)
		{
			if (chestSlots[i] != null && chestSlots[i].GetComponentInChildren<InventoryItem>() == null)
				return chestSlots[i];
		}
		return null;
	}

	public bool TryStoreInventoryItem(InventoryItem sourceItem)
	{
		if (sourceItem == null || sourceItem.item == null) return false;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = GetEmptyChestSlot();
			if (empty == null) return false;

			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
			return true;
		}

		var item = sourceItem.item;

		if (item.stackable && sourceItem.count > 0)
		{
			int moved = AddItemPartial(item, sourceItem.count);
			if (moved <= 0) return false;

			sourceItem.count -= moved;
			sourceItem.RefreshCount();

			if (sourceItem.count <= 0)
				Destroy(sourceItem.gameObject);

			return true;
		}

		if (!item.stackable)
		{
			var empty = GetEmptyChestSlot();
			if (empty == null) return false;

			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
			return true;
		}

		return false;
	}

	private void SpawnNewItemInChest(Item item, InventorySlot slot, int count, int waterFill, bool isSaltWater, bool isWaterContainer)
	{
		if (slot == null || item == null) return;

		if (isWaterContainer && item is WaterContainerItem wc)
		{
			var prefab = GameManager.Instance.InventoryManager.waterContainerItemPrefab;
			if (prefab == null) return;

			var go = Instantiate(prefab, slot.transform);
			var wii = go.GetComponent<WaterContainerInventoryItem>();
			wii.InitialiseWaterContainer(wc);
			wii.currentFill = Mathf.Clamp(waterFill, 0, wc.maxFillCapacity);
			wii.isSaltWater = isSaltWater;
			wii.UpdateSprite();
			wii.count = 1;
			wii.RefreshCount();
			return;
		}

		var invPrefab = GameManager.Instance.InventoryManager.inventoryItemPrefab;
		var obj = Instantiate(invPrefab, slot.transform);
		var ii = obj.GetComponent<InventoryItem>();
		ii.InitialiseItem(item);
		ii.count = count;
		ii.RefreshCount();
	}

	public int AddItemPartial(Item item, int count)
	{
		if (item == null || count <= 0) return 0;

		int maxStackedItems = GameManager.Instance.InventoryManager.maxStackedItems;

		if (item is WaterContainerItem)
		{
			int added = 0;
			for (int i = 0; i < chestSlots.Length && added < count; i++)
			{
				InventoryItem slotItem = chestSlots[i].GetComponentInChildren<InventoryItem>();
				if (slotItem != null) continue;

				SpawnNewItemInChest(item, chestSlots[i], 1, 0, false, true);
				added += 1;
			}
			return added;
		}

		int remaining = count;

		if (item.stackable)
		{
			for (int i = 0; i < chestSlots.Length && remaining > 0; i++)
			{
				InventoryItem slotItem = chestSlots[i].GetComponentInChildren<InventoryItem>();
				if (slotItem != null && slotItem.item == item && slotItem.count < maxStackedItems)
				{
					int spaceLeft = maxStackedItems - slotItem.count;
					int amountToAdd = Mathf.Min(remaining, spaceLeft);
					slotItem.count += amountToAdd;
					slotItem.RefreshCount();
					remaining -= amountToAdd;
				}
			}
		}

		for (int i = 0; i < chestSlots.Length && remaining > 0; i++)
		{
			InventoryItem slotItem = chestSlots[i].GetComponentInChildren<InventoryItem>();
			if (slotItem == null)
			{
				int amountToPlace = item.stackable ? Mathf.Min(remaining, maxStackedItems) : 1;
				SpawnNewItemInChest(item, chestSlots[i], amountToPlace, 0, false, false);
				remaining -= amountToPlace;
			}
		}

		return count - remaining;
	}

	public bool AddItem(Item item, int count = 1)
	{
		return AddItemPartial(item, count) == count;
	}

	public void RemoveItem(string itemName, int count)
	{
		if (string.IsNullOrEmpty(itemName) || count <= 0) return;

		int remainingToRemove = count;
		for (int i = 0; i < chestSlots.Length && remainingToRemove > 0; i++)
		{
			InventoryItem slotItem = chestSlots[i].GetComponentInChildren<InventoryItem>();
			if (slotItem != null && slotItem.item != null && slotItem.item.name == itemName && slotItem.count > 0)
			{
				int amountToRemove = Mathf.Min(remainingToRemove, slotItem.count);
				slotItem.count -= amountToRemove;
				slotItem.RefreshCount();
				remainingToRemove -= amountToRemove;

				if (slotItem.count <= 0)
					Destroy(slotItem.gameObject);
			}
		}
	}

	public bool HasAnyItems()
	{
		if (chestSlots == null || chestSlots.Length == 0) return false;

		for (int i = 0; i < chestSlots.Length; i++)
		{
			if (chestSlots[i] == null) continue;
			if (chestSlots[i].GetComponentInChildren<InventoryItem>() != null) return true;
		}

		return false;
	}

	public void SetHighlighted(bool highlight)
	{
		if (spriteRenderer != null)
			spriteRenderer.sprite = highlight ? highlightedSprite : normalSprite;
	}

	public bool IsOpen()
	{
		return isOpen;
	}

	public void Interact()
	{
		ToggleChest();
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
		if (spriteRenderer == null || spriteRenderer.sprite == null) return false;

		Bounds spriteBounds = spriteRenderer.bounds;
		Vector2 spriteMin = spriteBounds.min;
		Vector2 spriteMax = spriteBounds.max;

		return mouseWorldPos.x >= spriteMin.x && mouseWorldPos.x <= spriteMax.x &&
			   mouseWorldPos.y >= spriteMin.y && mouseWorldPos.y <= spriteMax.y;
	}

	public SpriteRenderer GetSpriteRenderer()
	{
		return spriteRenderer;
	}

	[System.Serializable]
	private class ChestState
	{
		public List<InventorySlotData> slots = new List<InventorySlotData>();
	}

	public string CaptureStateJson()
	{
		var state = new ChestState();
		state.slots.Clear();

		for (int i = 0; i < chestSlots.Length; i++)
		{
			var invItem = chestSlots[i].GetComponentInChildren<InventoryItem>();
			if (invItem == null)
			{
				state.slots.Add(new InventorySlotData());
				continue;
			}

			var sd = new InventorySlotData
			{
				itemId = invItem.item != null ? invItem.item.name : null,
				count = invItem.count
			};

			var water = invItem as WaterContainerInventoryItem;
			if (water != null)
			{
				sd.isWaterContainer = true;
				sd.waterFill = water.currentFill;
				sd.isSaltWater = water.isSaltWater;
			}

			state.slots.Add(sd);
		}

		return JsonUtility.ToJson(state);
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var state = JsonUtility.FromJson<ChestState>(json);
		if (state == null) return;

		for (int i = 0; i < chestSlots.Length; i++)
		{
			for (int c = chestSlots[i].transform.childCount - 1; c >= 0; c--)
				Destroy(chestSlots[i].transform.GetChild(c).gameObject);
		}

		var saveMgr = FindAnyObjectByType<SaveGameManager>();
		if (saveMgr == null || saveMgr.itemDatabase == null) return;

		for (int i = 0; i < chestSlots.Length && i < state.slots.Count; i++)
		{
			var sd = state.slots[i];
			if (sd == null || string.IsNullOrEmpty(sd.itemId) || sd.count <= 0) continue;

			var item = saveMgr.itemDatabase.Get(sd.itemId);
			if (item == null) continue;

			if (sd.isWaterContainer && item is WaterContainerItem wc)
			{
				SpawnNewItemInChest(item, chestSlots[i], 1, sd.waterFill, sd.isSaltWater, true);
			}
			else
			{
				SpawnNewItemInChest(item, chestSlots[i], sd.count, 0, false, false);
			}
		}
	}
}

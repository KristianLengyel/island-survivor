using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootItem
{
	public Item item;
	public int minAmount;
	public int maxAmount;
}

public class InventoryManager : MonoBehaviour
{
	public int maxStackedItems = 20;
	public int ToolbarSlotCount = 6;
	public InventorySlot[] inventorySlots;
	public GameObject inventoryItemPrefab;
	public GameObject waterContainerItemPrefab;
	[SerializeField] private GameObject inventoryUI;
	[SerializeField] private GameObject craftingUI;
	[SerializeField] private UIManager uiManager;

	[Header("Starter Items")]
	public Item[] startItems;

	[Header("Loot Items")]
	public LootItem[] possibleLoot;

	private int selectedSlot = 0;
	private CraftingMenu craftingMenu;

	public bool InventoryLocked { get; set; }

	private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>(64);
	private bool countsDirty = true;

	private void Awake()
	{
		if (craftingUI != null)
		{
			craftingMenu = craftingUI.GetComponentInChildren<CraftingMenu>(true);
			if (craftingMenu == null)
				Debug.LogError("CraftingMenu component not found in children of craftingUI!");
		}
	}

	private void Start()
	{
		if (inventoryUI != null) inventoryUI.SetActive(false);
		if (craftingUI != null) craftingUI.SetActive(false);

		ChangeSelectedSlot(0);

		foreach (var item in startItems)
			AddItem(item);

		MarkCountsDirty();
		RebuildCountsIfDirty();
	}

	private void LateUpdate()
	{
		RebuildCountsIfDirty();
	}

	public void MarkCountsDirty()
	{
		countsDirty = true;
	}

	public void ShiftMoveBetweenToolbarAndInventory(InventoryItem sourceItem)
	{
		if (sourceItem == null || sourceItem.item == null) return;
		if (!IsInventoryOpen()) return;

		int fromIndex = GetSlotIndexOfItem(sourceItem);
		if (fromIndex < 0) return;

		bool fromToolbar = fromIndex < ToolbarSlotCount;

		int targetStart = fromToolbar ? ToolbarSlotCount : 0;
		int targetEnd = fromToolbar ? inventorySlots.Length - 1 : ToolbarSlotCount - 1;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = GetEmptySlotInRange(targetStart, targetEnd);
			if (empty == null) return;

			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
			MarkCountsDirty();
			return;
		}

		Item item = sourceItem.item;

		if (!item.stackable)
		{
			var empty = GetEmptySlotInRange(targetStart, targetEnd);
			if (empty == null) return;

			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
			MarkCountsDirty();
			return;
		}

		int moved = MoveStackableCountIntoRange(item, sourceItem.count, targetStart, targetEnd);
		if (moved <= 0) return;

		sourceItem.count -= moved;
		sourceItem.RefreshCount();

		if (sourceItem.count <= 0)
		{
			Destroy(sourceItem.gameObject);
			if (Tooltip.Instance != null) Tooltip.Instance.HideTooltip();
		}

		MarkCountsDirty();
	}

	private int GetSlotIndexOfItem(InventoryItem item)
	{
		if (inventorySlots == null) return -1;

		for (int i = 0; i < inventorySlots.Length; i++)
		{
			if (inventorySlots[i] == null) continue;

			var child = inventorySlots[i].GetComponentInChildren<InventoryItem>();
			if (child == item) return i;
		}
		return -1;
	}

	private InventorySlot GetEmptySlotInRange(int start, int end)
	{
		start = Mathf.Clamp(start, 0, inventorySlots.Length - 1);
		end = Mathf.Clamp(end, 0, inventorySlots.Length - 1);

		for (int i = start; i <= end; i++)
		{
			var slot = inventorySlots[i];
			if (slot != null && slot.transform.childCount == 0)
				return slot;
		}

		return null;
	}

	private int MoveStackableCountIntoRange(Item item, int count, int start, int end)
	{
		if (item == null || count <= 0) return 0;

		int remaining = count;
		int max = maxStackedItems;

		start = Mathf.Clamp(start, 0, inventorySlots.Length - 1);
		end = Mathf.Clamp(end, 0, inventorySlots.Length - 1);

		for (int i = start; i <= end && remaining > 0; i++)
		{
			var slot = inventorySlots[i];
			if (slot == null) continue;

			var slotItem = slot.GetComponentInChildren<InventoryItem>();
			if (slotItem == null) continue;

			if (slotItem.item == item && slotItem.count < max)
			{
				int space = max - slotItem.count;
				int add = Mathf.Min(space, remaining);
				slotItem.count += add;
				slotItem.RefreshCount();
				remaining -= add;
			}
		}

		for (int i = start; i <= end && remaining > 0; i++)
		{
			var slot = inventorySlots[i];
			if (slot == null) continue;
			if (slot.transform.childCount != 0) continue;

			int place = Mathf.Min(max, remaining);
			SpawnNewItem(item, slot, place);
			remaining -= place;
		}

		return count - remaining;
	}

	private void RebuildCountsIfDirty()
	{
		if (!countsDirty) return;
		countsDirty = false;

		itemCounts.Clear();

		if (inventorySlots == null) return;

		for (int i = 0; i < inventorySlots.Length; i++)
		{
			var slot = inventorySlots[i];
			if (slot == null) continue;

			var invItem = slot.GetComponentInChildren<InventoryItem>();
			if (invItem == null || invItem.item == null || invItem.count <= 0) continue;

			string key = invItem.item.name;
			if (string.IsNullOrEmpty(key)) continue;

			if (itemCounts.TryGetValue(key, out int existing))
				itemCounts[key] = existing + invItem.count;
			else
				itemCounts[key] = invItem.count;
		}
	}

	public int GetItemCount(string itemName)
	{
		if (string.IsNullOrEmpty(itemName)) return 0;
		RebuildCountsIfDirty();
		return itemCounts.TryGetValue(itemName, out int c) ? c : 0;
	}

	public bool HasItem(string itemName, int amount)
	{
		if (amount <= 0) return true;
		return GetItemCount(itemName) >= amount;
	}

	public bool IsInventoryOpen() => inventoryUI != null && inventoryUI.activeSelf;

	public void ShowInventory(bool show, bool fromChest = false)
	{
		if (!show && InventoryLocked) return;

		if (inventoryUI != null) inventoryUI.SetActive(show);

		if (craftingUI != null)
		{
			craftingUI.SetActive(show && !fromChest);
			if (!show && craftingMenu != null)
				craftingMenu.HideRecipeDetails();
		}

		if (!show && Tooltip.Instance != null)
			Tooltip.Instance.HideTooltip();
	}

	public void ChangeSelectedSlot(int newIndex)
	{
		if (selectedSlot >= 0 && selectedSlot < inventorySlots.Length)
			inventorySlots[selectedSlot].Deselect();

		inventorySlots[newIndex].Select();
		selectedSlot = newIndex;
	}

	public int SelectedSlotIndex
	{
		get => selectedSlot;
		private set
		{
			if (selectedSlot != value)
				ChangeSelectedSlot(value);
		}
	}

	public Item GetSelectedItem()
	{
		var slot = inventorySlots[selectedSlot];
		var itemInSlot = slot.GetComponentInChildren<InventoryItem>();
		return itemInSlot?.item;
	}

	public InventorySlot GetEmptySlot()
	{
		foreach (var slot in inventorySlots)
		{
			if (slot != null && slot.transform.childCount == 0)
				return slot;
		}
		return null;
	}

	public bool TryStoreInventoryItem(InventoryItem sourceItem, bool showNotification = false)
	{
		if (sourceItem == null || sourceItem.item == null) return false;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = GetEmptySlot();
			if (empty == null) return false;

			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;

			MarkCountsDirty();
			return true;
		}

		var item = sourceItem.item;

		if (item.stackable && sourceItem.count > 0)
		{
			int moved = AddItemPartial(item, sourceItem.count, showNotification);
			if (moved <= 0) return false;

			sourceItem.count -= moved;
			sourceItem.RefreshCount();

			if (sourceItem.count <= 0)
				Destroy(sourceItem.gameObject);

			MarkCountsDirty();
			return true;
		}

		if (!item.stackable)
		{
			var empty = GetEmptySlot();
			if (empty == null) return false;

			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;

			MarkCountsDirty();
			return true;
		}

		return false;
	}

	public int AddItemPartial(Item item, int count, bool showNotification = true)
	{
		if (item == null || count <= 0) return 0;

		if (item is WaterContainerItem)
		{
			int added = 0;
			for (int i = 0; i < inventorySlots.Length && added < count; i++)
			{
				var slotItem = inventorySlots[i].GetComponentInChildren<InventoryItem>();
				if (slotItem != null) continue;

				SpawnNewItem(item, inventorySlots[i], 1);
				added += 1;
			}

			if (showNotification && added > 0)
				ShowItemNotification(item, added);

			if (added > 0) MarkCountsDirty();
			return added;
		}

		int remaining = count;

		if (item.stackable)
		{
			for (int i = 0; i < inventorySlots.Length && remaining > 0; i++)
			{
				InventoryItem slotItem = inventorySlots[i].GetComponentInChildren<InventoryItem>();
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

		for (int i = 0; i < inventorySlots.Length && remaining > 0; i++)
		{
			InventoryItem slotItem = inventorySlots[i].GetComponentInChildren<InventoryItem>();
			if (slotItem == null)
			{
				int amountToPlace = item.stackable ? Mathf.Min(remaining, maxStackedItems) : 1;
				SpawnNewItem(item, inventorySlots[i], amountToPlace);
				remaining -= amountToPlace;
			}
		}

		int addedFinal = count - remaining;

		if (showNotification && addedFinal > 0)
			ShowItemNotification(item, addedFinal);

		if (addedFinal > 0) MarkCountsDirty();
		return addedFinal;
	}

	public bool AddItem(Item item, int count = 1, bool showNotification = true)
	{
		return AddItemPartial(item, count, showNotification) == count;
	}

	public void RemoveItem(string itemName, int count)
	{
		if (string.IsNullOrEmpty(itemName) || count <= 0) return;

		int remainingToRemove = count;
		foreach (var slot in inventorySlots)
		{
			if (remainingToRemove <= 0) break;

			InventoryItem slotItem = slot.GetComponentInChildren<InventoryItem>();
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

		if (remainingToRemove != count) MarkCountsDirty();
	}

	public InventoryItem GetInventoryItemByName(string itemName)
	{
		foreach (var slot in inventorySlots)
		{
			var slotItem = slot.GetComponentInChildren<InventoryItem>();
			if (slotItem != null && slotItem.item != null && slotItem.item.name == itemName)
				return slotItem;
		}
		return null;
	}

	public void UseResources(List<ResourceRequirement> resourceRequirements)
	{
		foreach (var req in resourceRequirements)
			RemoveItem(req.resource.name, req.amount);
	}

	public void AddResources(List<ResourceRequirement> resourceRequirements)
	{
		foreach (var req in resourceRequirements)
			AddItem(req.resource, req.amount);
	}

	public List<Item> GenerateLoot()
	{
		List<Item> loot = new List<Item>();
		int numberOfItems = Random.Range(4, 6);

		for (int i = 0; i < numberOfItems; i++)
		{
			LootItem randomLootItem = possibleLoot[Random.Range(0, possibleLoot.Length)];
			int amount = Random.Range(randomLootItem.minAmount, randomLootItem.maxAmount + 1);
			for (int j = 0; j < amount; j++)
				loot.Add(randomLootItem.item);
		}

		while (loot.Count > 6)
			loot.RemoveAt(Random.Range(0, loot.Count));

		return loot;
	}

	public void SpawnNewItem(Item item, InventorySlot slot, int count = 1)
	{
		if (item is WaterContainerItem waterItem)
		{
			if (waterContainerItemPrefab == null)
			{
				Debug.LogError("WaterContainerItemPrefab is not assigned in InventoryManager!");
				return;
			}

			GameObject newItemGo = Instantiate(waterContainerItemPrefab, slot.transform);
			WaterContainerInventoryItem inventoryItem = newItemGo.GetComponent<WaterContainerInventoryItem>();
			inventoryItem.InitialiseWaterContainer(waterItem);
			inventoryItem.currentFill = 0;
			inventoryItem.isSaltWater = false;
			inventoryItem.UpdateSprite();
			inventoryItem.count = 1;
			inventoryItem.RefreshCount();
		}
		else
		{
			GameObject newItemGo = Instantiate(inventoryItemPrefab, slot.transform);
			InventoryItem inventoryItem = newItemGo.GetComponent<InventoryItem>();
			inventoryItem.InitialiseItem(item);
			inventoryItem.count = count;
			inventoryItem.RefreshCount();
		}

		MarkCountsDirty();
	}

	public void SpawnNewWaterContainerInSlot(WaterContainerItem wc, InventorySlot slot, int fill, bool salt)
	{
		if (wc == null || slot == null) return;
		if (waterContainerItemPrefab == null) return;

		GameObject go = Instantiate(waterContainerItemPrefab, slot.transform);
		var wii = go.GetComponent<WaterContainerInventoryItem>();
		wii.InitialiseWaterContainer(wc);
		wii.currentFill = Mathf.Clamp(fill, 0, wc.maxFillCapacity);
		wii.isSaltWater = salt;
		wii.UpdateSprite();
		wii.count = 1;
		wii.RefreshCount();

		MarkCountsDirty();
	}

	private void ShowItemNotification(Item item, int count)
	{
		uiManager?.ShowItemNotification(item.image, item.name, count);
	}

	public bool IsAnyChestOpen()
	{
		return Chest.CurrentOpenChest != null && Chest.CurrentOpenChest.IsOpen();
	}

	public InventoryData CaptureInventoryState()
	{
		var data = new InventoryData();
		data.selectedSlotIndex = SelectedSlotIndex;

		data.slots.Clear();
		for (int i = 0; i < inventorySlots.Length; i++)
		{
			var slot = inventorySlots[i];
			var invItem = slot.GetComponentInChildren<InventoryItem>();

			if (invItem == null)
			{
				data.slots.Add(new InventorySlotData());
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

			data.slots.Add(sd);
		}

		return data;
	}

	public void RestoreInventoryState(InventoryData data, ItemDatabase itemDb)
	{
		if (data == null) return;

		for (int i = 0; i < inventorySlots.Length; i++)
		{
			var slot = inventorySlots[i];
			for (int c = slot.transform.childCount - 1; c >= 0; c--)
				Destroy(slot.transform.GetChild(c).gameObject);
		}

		for (int i = 0; i < inventorySlots.Length && i < data.slots.Count; i++)
		{
			var sd = data.slots[i];
			if (sd == null || string.IsNullOrEmpty(sd.itemId) || sd.count <= 0) continue;

			var item = itemDb.Get(sd.itemId);
			if (item == null) continue;

			if (sd.isWaterContainer && item is WaterContainerItem wc)
			{
				SpawnNewWaterContainerInSlot(wc, inventorySlots[i], Mathf.RoundToInt(sd.waterFill), sd.isSaltWater);
			}
			else
			{
				SpawnNewItem(item, inventorySlots[i], sd.count);
			}
		}

		SelectedSlotIndex = Mathf.Clamp(data.selectedSlotIndex, 0, inventorySlots.Length - 1);

		MarkCountsDirty();
		RebuildCountsIfDirty();
	}
}
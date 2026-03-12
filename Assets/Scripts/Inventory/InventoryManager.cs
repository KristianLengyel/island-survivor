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
	private InventoryContainer container;

	public bool InventoryLocked { get; set; }

	private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>(64);
	private bool countsDirty = true;

	private void Awake()
	{
		container = new InventoryContainer(inventorySlots, maxStackedItems, inventoryItemPrefab, waterContainerItemPrefab);

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

	/// <summary>Creates an InventoryContainer backed by the given slots, sharing this manager's prefabs and stack size.</summary>
	public InventoryContainer CreateContainer(InventorySlot[] slots)
	{
		return new InventoryContainer(slots, maxStackedItems, inventoryItemPrefab, waterContainerItemPrefab);
	}

	public void MarkCountsDirty() => countsDirty = true;

	public void ShiftMoveBetweenToolbarAndInventory(InventoryItem sourceItem)
	{
		if (sourceItem == null || sourceItem.item == null) return;
		if (!IsInventoryOpen()) return;

		int fromIndex = container.GetSlotIndexOfItem(sourceItem);
		if (fromIndex < 0) return;

		bool fromToolbar = fromIndex < ToolbarSlotCount;
		int targetStart = fromToolbar ? ToolbarSlotCount : 0;
		int targetEnd = fromToolbar ? inventorySlots.Length - 1 : ToolbarSlotCount - 1;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = container.GetEmptySlotInRange(targetStart, targetEnd);
			if (empty == null) return;
			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
			MarkCountsDirty();
			return;
		}

		Item item = sourceItem.item;

		if (!item.stackable)
		{
			var empty = container.GetEmptySlotInRange(targetStart, targetEnd);
			if (empty == null) return;
			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
			MarkCountsDirty();
			return;
		}

		int moved = container.MoveStackableCountIntoRange(item, sourceItem.count, targetStart, targetEnd);
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
		return inventorySlots[selectedSlot].GetComponentInChildren<InventoryItem>()?.item;
	}

	public InventorySlot GetEmptySlot() => container.GetEmptySlot();

	public bool TryStoreInventoryItem(InventoryItem sourceItem, bool showNotification = false)
	{
		bool stored = container.TryStoreInventoryItem(sourceItem);
		if (stored) MarkCountsDirty();
		return stored;
	}

	public int AddItemPartial(Item item, int count, bool showNotification = true)
	{
		int added = container.AddItemPartial(item, count);
		if (added > 0)
		{
			if (showNotification) ShowItemNotification(item, added);
			MarkCountsDirty();
		}
		return added;
	}

	public bool AddItem(Item item, int count = 1, bool showNotification = true)
	{
		return AddItemPartial(item, count, showNotification) == count;
	}

	public void RemoveItem(string itemName, int count)
	{
		container.RemoveItem(itemName, count);
		MarkCountsDirty();
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
		var loot = new List<Item>();
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
		container.SpawnItem(item, slot, count);
		MarkCountsDirty();
	}

	public void SpawnNewWaterContainerInSlot(WaterContainerItem wc, InventorySlot slot, int fill, bool salt)
	{
		container.SpawnWaterContainer(wc, slot, fill, salt);
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
		return new InventoryData
		{
			selectedSlotIndex = SelectedSlotIndex,
			slots = container.CaptureState()
		};
	}

	public void RestoreInventoryState(InventoryData data, ItemDatabase itemDb)
	{
		if (data == null) return;

		container.RestoreState(data.slots, itemDb);
		SelectedSlotIndex = Mathf.Clamp(data.selectedSlotIndex, 0, inventorySlots.Length - 1);

		MarkCountsDirty();
		RebuildCountsIfDirty();
	}
}

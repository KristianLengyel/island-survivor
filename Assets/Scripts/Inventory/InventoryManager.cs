using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
	[SerializeField] private int totalSlotCount = 27;

	public InventorySlot[] inventorySlots;

	[SerializeField] private UIDocument inventoryDocument;
	[SerializeField] private UIManager uiManager;
	[SerializeField] private CraftingMenu craftingMenu;
	[SerializeField] private CraftingManager craftingManager;

	[Header("Starter Items")]
	public Item[] startItems;

	[Header("Loot Items")]
	public LootItem[] possibleLoot;

	private int selectedSlot = 0;
	private InventoryContainer container;
	private bool inventoryOpen;

	public bool InventoryLocked { get; set; }

	private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>(64);
	private bool countsDirty = true;

	private VisualElement panelRoot;
	private VisualElement inventoryRoot;
	private VisualElement toolbarSlotContainer;
	private VisualElement invSlotContainer;
	private VisualElement chestPanelWrapper;
	private VisualElement chestSlotContainer;
	private VisualElement dragGhost;
	private InventorySlot dragSource;
	private int dragSplitCount;
	private readonly Dictionary<VisualElement, InventorySlot> elementToSlot = new Dictionary<VisualElement, InventorySlot>(64);

	private void Awake()
	{
		inventorySlots = new InventorySlot[totalSlotCount];
		for (int i = 0; i < totalSlotCount; i++)
			inventorySlots[i] = new InventorySlot();

		container = new InventoryContainer(inventorySlots, maxStackedItems);

		if (craftingManager == null)
			craftingManager = GameManager.Instance?.GetComponent<CraftingManager>();
	}

	private void Start()
	{
		SetupUI();
		ChangeSelectedSlot(0);

		foreach (var item in startItems)
			AddItem(item);

		MarkCountsDirty();
		RebuildCountsIfDirty();
	}

	private void SetupUI()
	{
		if (inventoryDocument == null) return;

		var root = inventoryDocument.rootVisualElement;
		panelRoot = root.Q<VisualElement>("panel-root");
		if (panelRoot == null) return;
		panelRoot.pickingMode = PickingMode.Ignore;

		inventoryRoot = panelRoot.Q<VisualElement>("inventory-root");
		toolbarSlotContainer = panelRoot.Q<VisualElement>("toolbar-slot-container");
		invSlotContainer = panelRoot.Q<VisualElement>("inv-slot-container");
		chestPanelWrapper = panelRoot.Q<VisualElement>("chest-panel-wrapper");
		chestSlotContainer = panelRoot.Q<VisualElement>("chest-slot-container");

		for (int i = 0; i < ToolbarSlotCount; i++)
		{
			var el = CreateSlotElement();
			if (i == ToolbarSlotCount - 1)
				el.style.marginRight = 0;
			toolbarSlotContainer?.Add(el);
			inventorySlots[i].Initialize(el);
			elementToSlot[el] = inventorySlots[i];
			RegisterSlotEvents(el, inventorySlots[i]);
		}

		int invCount = totalSlotCount - ToolbarSlotCount;
		VisualElement invRow = null;
		for (int i = ToolbarSlotCount; i < totalSlotCount; i++)
		{
			int invIndex = i - ToolbarSlotCount;
			if (invIndex % 7 == 0)
			{
				invRow = new VisualElement();
				invRow.style.flexDirection = FlexDirection.Row;
				invSlotContainer?.Add(invRow);
			}
			var el = CreateSlotElement();
			if (invIndex % 7 == 6) el.style.marginRight = 0;
			if (invIndex >= invCount - 7) el.style.marginBottom = 0;
			invRow?.Add(el);
			inventorySlots[i].Initialize(el);
			elementToSlot[el] = inventorySlots[i];
			RegisterSlotEvents(el, inventorySlots[i]);
		}

		if (inventoryRoot != null)
			inventoryRoot.style.display = DisplayStyle.None;

		if (chestPanelWrapper != null)
			chestPanelWrapper.style.display = DisplayStyle.None;

		panelRoot.RegisterCallback<PointerMoveEvent>(OnPanelPointerMove);
		panelRoot.RegisterCallback<PointerUpEvent>(OnPanelPointerUp);

		if (craftingMenu != null)
			craftingMenu.Initialize(panelRoot);

		if (craftingManager != null && craftingMenu != null)
			craftingManager.RegisterCraftingMenu(craftingMenu);

		if (Tooltip.Instance != null)
			Tooltip.Instance.Initialize(panelRoot);
	}

	private VisualElement CreateSlotElement()
	{
		var el = new VisualElement();
		el.AddToClassList("inv-slot");

		var icon = new VisualElement();
		icon.AddToClassList("inv-slot-icon");
		el.Add(icon);

		var count = new Label();
		count.AddToClassList("inv-slot-count");
		count.style.unityTextAlign = TextAnchor.LowerRight;
		count.pickingMode = PickingMode.Ignore;
		el.Add(count);

		return el;
	}

	private void RegisterSlotEvents(VisualElement el, InventorySlot slot)
	{
		el.RegisterCallback<PointerDownEvent>(evt =>
		{
			if (evt.button == 2)
			{
				if (!slot.IsEmpty && slot.CurrentItem.item != null && slot.CurrentItem.item.stackable && slot.CurrentItem.count > 1)
					BeginSplitDrag(evt, slot);
				return;
			}

			if (evt.button != 0) return;

			if (!slot.IsEmpty && (evt.modifiers & EventModifiers.Shift) != 0)
			{
				ShiftMoveBetweenToolbarAndInventory(slot.CurrentItem);
				return;
			}

			if (!slot.IsEmpty)
				BeginDrag(evt, slot);
		});

		el.RegisterCallback<PointerEnterEvent>(evt =>
		{
			if (dragGhost != null) return;
			if (!slot.IsEmpty && slot.CurrentItem.item != null)
				Tooltip.Instance?.ShowTooltip(slot.CurrentItem.item.name, slot.CurrentItem.item.type.ToString(), el);
		});

		el.RegisterCallback<PointerLeaveEvent>(evt =>
		{
			if (dragGhost == null)
				Tooltip.Instance?.HideTooltip();
		});
	}

	private void BeginDrag(PointerDownEvent evt, InventorySlot slot)
	{
		dragSource = slot;
		dragSplitCount = 0;
		dragGhost = new VisualElement();
		dragGhost.AddToClassList("inv-drag-ghost");
		dragGhost.pickingMode = PickingMode.Ignore;

		var sprite = slot.CurrentItem.GetSprite();
		if (sprite != null)
			dragGhost.style.backgroundImage = new StyleBackground(sprite);

		panelRoot.Add(dragGhost);
		MoveDragGhost(evt.position);
		evt.StopPropagation();
	}

	private void BeginSplitDrag(PointerDownEvent evt, InventorySlot slot)
	{
		dragSource = slot;
		dragSplitCount = Mathf.CeilToInt(slot.CurrentItem.count / 2f);

		dragGhost = new VisualElement();
		dragGhost.AddToClassList("inv-drag-ghost");
		dragGhost.pickingMode = PickingMode.Ignore;

		var sprite = slot.CurrentItem.GetSprite();
		if (sprite != null)
			dragGhost.style.backgroundImage = new StyleBackground(sprite);

		panelRoot.Add(dragGhost);
		MoveDragGhost(evt.position);
		evt.StopPropagation();
	}

	private void MoveDragGhost(Vector2 screenPos)
	{
		if (dragGhost == null || panelRoot == null) return;
		var rootBounds = panelRoot.worldBound;
		dragGhost.style.left = screenPos.x - rootBounds.x - 8;
		dragGhost.style.top = screenPos.y - rootBounds.y - 8;
	}

	private void OnPanelPointerMove(PointerMoveEvent evt)
	{
		if (dragGhost == null) return;
		MoveDragGhost(evt.position);
	}

	private void OnPanelPointerUp(PointerUpEvent evt)
	{
		if (dragGhost == null || dragSource == null) { EndDrag(); return; }

		InventorySlot target = GetSlotAtPosition(evt.position);

		if (dragSplitCount > 0)
			HandleSplitDrop(target);
		else if (target != null && target != dragSource)
			SwapSlots(dragSource, target);

		EndDrag();
	}

	private void HandleSplitDrop(InventorySlot target)
	{
		var sourceItem = dragSource?.CurrentItem;
		if (sourceItem == null || target == null || target == dragSource)
			return;

		int split = dragSplitCount;

		if (target.IsEmpty)
		{
			sourceItem.count -= split;
			sourceItem.RefreshCount();
			if (sourceItem.count <= 0)
				dragSource.ClearItem();

			var newItem = new InventoryItem();
			newItem.InitialiseItem(sourceItem.item);
			newItem.count = split;
			target.SetItem(newItem);
			MarkCountsDirty();
		}
		else if (target.CurrentItem != null && target.CurrentItem.item == sourceItem.item)
		{
			int space = maxStackedItems - target.CurrentItem.count;
			int toAdd = Mathf.Min(space, split);
			if (toAdd <= 0) return;

			target.CurrentItem.count += toAdd;
			target.CurrentItem.RefreshCount();

			sourceItem.count -= toAdd;
			sourceItem.RefreshCount();
			if (sourceItem.count <= 0)
				dragSource.ClearItem();

			MarkCountsDirty();
		}
	}

	private void EndDrag()
	{
		dragGhost?.RemoveFromHierarchy();
		dragGhost = null;
		dragSource = null;
		dragSplitCount = 0;
		Tooltip.Instance?.HideTooltip();
	}

	private InventorySlot GetSlotAtPosition(Vector2 screenPos)
	{
		foreach (var kvp in elementToSlot)
		{
			if (kvp.Key.worldBound.Contains(screenPos))
				return kvp.Value;
		}
		return null;
	}

	private void SwapSlots(InventorySlot a, InventorySlot b)
	{
		var itemA = a.CurrentItem;
		var itemB = b.CurrentItem;

		if (itemA != null && itemB != null && itemA.item == itemB.item && itemA.item.stackable)
		{
			int space = maxStackedItems - itemB.count;
			int toAdd = Mathf.Min(space, itemA.count);
			if (toAdd > 0)
			{
				itemB.count += toAdd;
				itemB.RefreshCount();
				itemA.count -= toAdd;
				if (itemA.count <= 0)
					a.ClearItem();
				else
					itemA.RefreshCount();
				MarkCountsDirty();
				return;
			}
		}

		a.ClearItem();
		b.ClearItem();
		if (itemA != null) b.SetItem(itemA);
		if (itemB != null) a.SetItem(itemB);
		MarkCountsDirty();
	}

	public void OpenChestPanel(Chest chest)
	{
		if (chest == null || chestPanelWrapper == null || chestSlotContainer == null) return;

		chestSlotContainer.Clear();

		var chestSlots = chest.ChestSlots;
		int chestCount = chestSlots.Length;
		VisualElement chestRow = null;
		for (int i = 0; i < chestCount; i++)
		{
			if (i % 7 == 0)
			{
				chestRow = new VisualElement();
				chestRow.style.flexDirection = FlexDirection.Row;
				chestSlotContainer.Add(chestRow);
			}
			var el = CreateSlotElement();
			if (i % 7 == 6) el.style.marginRight = 0;
			if (i >= chestCount - 7) el.style.marginBottom = 0;
			chestRow?.Add(el);
			chestSlots[i].IsChestSlot = true;
			chestSlots[i].Initialize(el);
			elementToSlot[el] = chestSlots[i];
			RegisterSlotEvents(el, chestSlots[i]);
		}

		chestPanelWrapper.style.display = DisplayStyle.Flex;
	}

	public void CloseChestPanel(Chest chest)
	{
		if (chestPanelWrapper == null || chestSlotContainer == null) return;

		if (chest != null)
		{
			foreach (var slot in chest.ChestSlots)
			{
				if (slot.Element != null)
				{
					elementToSlot.Remove(slot.Element);
					slot.Uninitialize();
				}
			}
		}

		chestSlotContainer.Clear();
		chestPanelWrapper.style.display = DisplayStyle.None;
	}

	private void LateUpdate()
	{
		RebuildCountsIfDirty();
	}

	public InventoryContainer CreateContainer(InventorySlot[] slots)
		=> new InventoryContainer(slots, maxStackedItems);

	public void MarkCountsDirty() => countsDirty = true;

	public void ShiftMoveBetweenToolbarAndInventory(InventoryItem sourceItem)
	{
		if (sourceItem == null || sourceItem.item == null) return;
		if (!IsInventoryOpen()) return;

		if (sourceItem.slot != null && sourceItem.slot.IsChestSlot)
		{
			ShiftMoveToPlayerRange(sourceItem, ToolbarSlotCount, inventorySlots.Length - 1);
			if (sourceItem.slot != null && !sourceItem.slot.IsEmpty)
				ShiftMoveToPlayerRange(sourceItem, 0, ToolbarSlotCount - 1);
			return;
		}

		if (IsAnyChestOpen())
		{
			var chest = Chest.CurrentOpenChest;
			if (chest != null)
			{
				chest.TryStoreInventoryItem(sourceItem);
				MarkCountsDirty();
			}
			return;
		}

		int fromIndex = container.GetSlotIndexOfItem(sourceItem);
		if (fromIndex < 0) return;

		bool fromToolbar = fromIndex < ToolbarSlotCount;
		int targetStart = fromToolbar ? ToolbarSlotCount : 0;
		int targetEnd = fromToolbar ? inventorySlots.Length - 1 : ToolbarSlotCount - 1;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = container.GetEmptySlotInRange(targetStart, targetEnd);
			if (empty == null) return;
			sourceItem.slot?.ClearItem();
			empty.SetItem(sourceItem);
			MarkCountsDirty();
			return;
		}

		Item item = sourceItem.item;

		if (!item.stackable)
		{
			var empty = container.GetEmptySlotInRange(targetStart, targetEnd);
			if (empty == null) return;
			sourceItem.slot?.ClearItem();
			empty.SetItem(sourceItem);
			MarkCountsDirty();
			return;
		}

		int moved = container.MoveStackableCountIntoRange(item, sourceItem.count, targetStart, targetEnd);
		if (moved <= 0) return;

		sourceItem.count -= moved;
		sourceItem.RefreshCount();

		if (sourceItem.count <= 0)
		{
			sourceItem.slot?.ClearItem();
			Tooltip.Instance?.HideTooltip();
		}

		MarkCountsDirty();
	}

	private void ShiftMoveToPlayerRange(InventoryItem sourceItem, int start, int end)
	{
		if (sourceItem.slot == null || sourceItem.slot.IsEmpty) return;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = container.GetEmptySlotInRange(start, end);
			if (empty == null) return;
			sourceItem.slot.ClearItem();
			empty.SetItem(sourceItem);
			MarkCountsDirty();
			return;
		}

		Item item = sourceItem.item;

		if (!item.stackable)
		{
			var empty = container.GetEmptySlotInRange(start, end);
			if (empty == null) return;
			sourceItem.slot.ClearItem();
			empty.SetItem(sourceItem);
			MarkCountsDirty();
			return;
		}

		int moved = container.MoveStackableCountIntoRange(item, sourceItem.count, start, end);
		if (moved <= 0) return;

		sourceItem.count -= moved;
		sourceItem.RefreshCount();

		if (sourceItem.count <= 0)
		{
			sourceItem.slot?.ClearItem();
			Tooltip.Instance?.HideTooltip();
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

			var invItem = slot.CurrentItem;
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

	public bool IsInventoryOpen() => inventoryOpen;

	public void ShowInventory(bool show, bool fromChest = false)
	{
		if (!show && InventoryLocked) return;

		inventoryOpen = show;

		if (inventoryRoot != null)
			inventoryRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

		if (craftingMenu != null)
		{
			craftingMenu.SetVisible(show && !fromChest);
			if (!show)
				craftingMenu.HideRecipeDetails();
		}

		if (!show)
			Tooltip.Instance?.HideTooltip();
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

	public Item GetSelectedItem() => inventorySlots[selectedSlot].CurrentItem?.item;

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
			var slotItem = slot.CurrentItem;
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

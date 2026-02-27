using UnityEngine;

public class InventoryInputHandler : MonoBehaviour
{
	private InventoryManager inventoryManager;
	private const int ToolbarSlotCount = 6;

	private void Start()
	{
		inventoryManager = GameManager.Instance.InventoryManager;
	}

	private void Update()
	{
		if (GameInput.InventoryDown)
		{
			if (!inventoryManager.InventoryLocked)
			{
				MenuCoordinator.Instance.Toggle("Inventory");
			}
		}

		if (!inventoryManager.IsInventoryOpen())
		{
			int slotDown = GameInput.ToolbarSlotDown;
			if (slotDown >= 0) inventoryManager.ChangeSelectedSlot(slotDown);

			float scroll = GameInput.Scroll;
			if (scroll != 0)
			{
				int slotIncrement = scroll > 0 ? -1 : 1;
				int currentSlot = inventoryManager.SelectedSlotIndex;
				int newSlot = (currentSlot + slotIncrement + ToolbarSlotCount) % ToolbarSlotCount;
				inventoryManager.ChangeSelectedSlot(newSlot);
			}
		}

		if (GameInput.StackIncreaseDown)
		{
			IncreaseSelectedItemCount(10);
		}

		if (GameInput.StackDecreaseDown)
		{
			DecreaseSelectedItemCount(1);
		}
	}

	private void IncreaseSelectedItemCount(int amount)
	{
		if (inventoryManager.SelectedSlotIndex < 0 || inventoryManager.SelectedSlotIndex >= ToolbarSlotCount)
		{
			return;
		}

		var slot = inventoryManager.inventorySlots[inventoryManager.SelectedSlotIndex];
		var slotItem = slot.GetComponentInChildren<InventoryItem>();
		if (slotItem == null || !slotItem.item.stackable) return;

		int maxStackedItems = inventoryManager.maxStackedItems;
		int remainingAmount = amount;

		int spaceLeft = maxStackedItems - slotItem.count;
		int amountToAddToCurrent = Mathf.Min(remainingAmount, spaceLeft);
		slotItem.count += amountToAddToCurrent;
		slotItem.RefreshCount();
		remainingAmount -= amountToAddToCurrent;

		if (remainingAmount > 0)
		{
			for (int i = 0; i < inventoryManager.inventorySlots.Length && remainingAmount > 0; i++)
			{
				if (i == inventoryManager.SelectedSlotIndex) continue;

				var otherSlot = inventoryManager.inventorySlots[i];
				var otherItem = otherSlot.GetComponentInChildren<InventoryItem>();
				if (otherItem != null && otherItem.item == slotItem.item && otherItem.count < maxStackedItems)
				{
					int spaceInOther = maxStackedItems - otherItem.count;
					int amountToAdd = Mathf.Min(remainingAmount, spaceInOther);
					otherItem.count += amountToAdd;
					otherItem.RefreshCount();
					remainingAmount -= amountToAdd;
				}
			}

			while (remainingAmount > 0)
			{
				InventorySlot emptySlot = inventoryManager.GetEmptySlot();
				if (emptySlot == null) return;

				int amountToAdd = Mathf.Min(remainingAmount, maxStackedItems);
				inventoryManager.SpawnNewItem(slotItem.item, emptySlot, amountToAdd);
				remainingAmount -= amountToAdd;
			}
		}
	}

	private void DecreaseSelectedItemCount(int amount)
	{
		if (inventoryManager.SelectedSlotIndex < 0 || inventoryManager.SelectedSlotIndex >= ToolbarSlotCount)
		{
			return;
		}

		var slot = inventoryManager.inventorySlots[inventoryManager.SelectedSlotIndex];
		var slotItem = slot.GetComponentInChildren<InventoryItem>();
		if (slotItem != null && slotItem.item.stackable && slotItem.count > amount)
		{
			slotItem.count -= amount;
			slotItem.RefreshCount();
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

public class InventoryContainer
{
	private readonly InventorySlot[] slots;
	private readonly int maxStackSize;

	public int SlotCount => slots.Length;
	public int MaxStackSize => maxStackSize;

	public InventoryContainer(InventorySlot[] slots, int maxStackSize)
	{
		this.slots = slots;
		this.maxStackSize = maxStackSize;
	}

	public InventorySlot GetEmptySlot()
	{
		foreach (var slot in slots)
			if (slot != null && slot.IsEmpty)
				return slot;
		return null;
	}

	public InventorySlot GetEmptySlotInRange(int start, int end)
	{
		start = Mathf.Clamp(start, 0, slots.Length - 1);
		end = Mathf.Clamp(end, 0, slots.Length - 1);
		for (int i = start; i <= end; i++)
			if (slots[i] != null && slots[i].IsEmpty)
				return slots[i];
		return null;
	}

	public int GetSlotIndexOfItem(InventoryItem item)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] == null) continue;
			if (slots[i].CurrentItem == item)
				return i;
		}
		return -1;
	}

	public int AddItemPartial(Item item, int count)
	{
		if (item == null || count <= 0) return 0;

		if (item is WaterContainerItem)
		{
			int added = 0;
			for (int i = 0; i < slots.Length && added < count; i++)
			{
				if (!slots[i].IsEmpty) continue;
				SpawnItem(item, slots[i], 1);
				added++;
			}
			return added;
		}

		int remaining = count;

		if (item.stackable)
		{
			for (int i = 0; i < slots.Length && remaining > 0; i++)
			{
				var slotItem = slots[i].CurrentItem;
				if (slotItem != null && slotItem.item == item && slotItem.count < maxStackSize)
				{
					int space = maxStackSize - slotItem.count;
					int add = Mathf.Min(remaining, space);
					slotItem.count += add;
					slotItem.RefreshCount();
					remaining -= add;
				}
			}
		}

		for (int i = 0; i < slots.Length && remaining > 0; i++)
		{
			if (!slots[i].IsEmpty) continue;
			int place = item.stackable ? Mathf.Min(remaining, maxStackSize) : 1;
			SpawnItem(item, slots[i], place);
			remaining -= place;
		}

		return count - remaining;
	}

	public bool AddItem(Item item, int count = 1) => AddItemPartial(item, count) == count;

	public void RemoveItem(string itemName, int count)
	{
		if (string.IsNullOrEmpty(itemName) || count <= 0) return;

		int remaining = count;
		foreach (var slot in slots)
		{
			if (remaining <= 0) break;
			var slotItem = slot.CurrentItem;
			if (slotItem == null || slotItem.item == null || slotItem.item.name != itemName || slotItem.count <= 0) continue;

			int remove = Mathf.Min(remaining, slotItem.count);
			slotItem.count -= remove;
			remaining -= remove;

			if (slotItem.count <= 0)
				slot.ClearItem();
			else
				slotItem.RefreshCount();
		}
	}

	public bool TryStoreInventoryItem(InventoryItem sourceItem)
	{
		if (sourceItem == null || sourceItem.item == null) return false;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = GetEmptySlot();
			if (empty == null) return false;
			sourceItem.slot?.ClearItem();
			empty.SetItem(sourceItem);
			return true;
		}

		var item = sourceItem.item;

		if (item.stackable && sourceItem.count > 0)
		{
			int moved = AddItemPartial(item, sourceItem.count);
			if (moved <= 0) return false;

			sourceItem.count -= moved;
			if (sourceItem.count <= 0)
				sourceItem.slot?.ClearItem();
			else
				sourceItem.RefreshCount();

			return true;
		}

		if (!item.stackable)
		{
			var empty = GetEmptySlot();
			if (empty == null) return false;
			sourceItem.slot?.ClearItem();
			empty.SetItem(sourceItem);
			return true;
		}

		return false;
	}

	public int MoveStackableCountIntoRange(Item item, int count, int start, int end)
	{
		if (item == null || count <= 0) return 0;

		int remaining = count;
		start = Mathf.Clamp(start, 0, slots.Length - 1);
		end = Mathf.Clamp(end, 0, slots.Length - 1);

		for (int i = start; i <= end && remaining > 0; i++)
		{
			var slotItem = slots[i].CurrentItem;
			if (slotItem != null && slotItem.item == item && slotItem.count < maxStackSize)
			{
				int space = maxStackSize - slotItem.count;
				int add = Mathf.Min(space, remaining);
				slotItem.count += add;
				slotItem.RefreshCount();
				remaining -= add;
			}
		}

		for (int i = start; i <= end && remaining > 0; i++)
		{
			if (!slots[i].IsEmpty) continue;
			int place = Mathf.Min(maxStackSize, remaining);
			SpawnItem(item, slots[i], place);
			remaining -= place;
		}

		return count - remaining;
	}

	public void SpawnItem(Item item, InventorySlot slot, int count)
	{
		if (item == null || slot == null) return;

		if (item is WaterContainerItem wc)
		{
			var wcItem = new WaterContainerInventoryItem();
			wcItem.InitialiseWaterContainer(wc);
			wcItem.count = 1;
			slot.SetItem(wcItem);
		}
		else
		{
			var invItem = new InventoryItem();
			invItem.InitialiseItem(item);
			invItem.count = count;
			slot.SetItem(invItem);
		}
	}

	public void SpawnWaterContainer(WaterContainerItem wc, InventorySlot slot, int fill, bool isSaltWater)
	{
		if (wc == null || slot == null) return;
		var wcItem = new WaterContainerInventoryItem();
		wcItem.InitialiseWaterContainer(wc);
		wcItem.currentFill = Mathf.Clamp(fill, 0, wc.maxFillCapacity);
		wcItem.isSaltWater = isSaltWater;
		wcItem.count = 1;
		slot.SetItem(wcItem);
	}

	public List<InventorySlotData> CaptureState()
	{
		var result = new List<InventorySlotData>(slots.Length);
		for (int i = 0; i < slots.Length; i++)
		{
			var invItem = slots[i].CurrentItem;
			if (invItem == null)
			{
				result.Add(new InventorySlotData());
				continue;
			}

			var sd = new InventorySlotData
			{
				itemId = invItem.item != null ? invItem.item.name : null,
				count = invItem.count
			};

			if (invItem is WaterContainerInventoryItem water)
			{
				sd.isWaterContainer = true;
				sd.waterFill = water.currentFill;
				sd.isSaltWater = water.isSaltWater;
			}

			result.Add(sd);
		}
		return result;
	}

	public void RestoreState(List<InventorySlotData> slotData, ItemDatabase itemDb)
	{
		foreach (var slot in slots)
			slot.ClearItem();

		if (slotData == null) return;

		for (int i = 0; i < slots.Length && i < slotData.Count; i++)
		{
			var sd = slotData[i];
			if (sd == null || string.IsNullOrEmpty(sd.itemId) || sd.count <= 0) continue;

			var item = itemDb.Get(sd.itemId);
			if (item == null) continue;

			if (sd.isWaterContainer && item is WaterContainerItem wc)
				SpawnWaterContainer(wc, slots[i], Mathf.RoundToInt(sd.waterFill), sd.isSaltWater);
			else
				SpawnItem(item, slots[i], sd.count);
		}
	}
}

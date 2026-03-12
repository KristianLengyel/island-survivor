using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared inventory logic for any collection of InventorySlots.
/// Used by InventoryManager (player) and Chest to eliminate duplicated stacking/spawning/serialization code.
/// </summary>
public class InventoryContainer
{
	private readonly InventorySlot[] slots;
	private readonly int maxStackSize;
	private readonly GameObject itemPrefab;
	private readonly GameObject waterContainerPrefab;

	public int SlotCount => slots.Length;
	public int MaxStackSize => maxStackSize;

	public InventoryContainer(InventorySlot[] slots, int maxStackSize, GameObject itemPrefab, GameObject waterContainerPrefab)
	{
		this.slots = slots;
		this.maxStackSize = maxStackSize;
		this.itemPrefab = itemPrefab;
		this.waterContainerPrefab = waterContainerPrefab;
	}

	public InventorySlot GetEmptySlot()
	{
		foreach (var slot in slots)
			if (slot != null && slot.transform.childCount == 0)
				return slot;
		return null;
	}

	public InventorySlot GetEmptySlotInRange(int start, int end)
	{
		start = Mathf.Clamp(start, 0, slots.Length - 1);
		end = Mathf.Clamp(end, 0, slots.Length - 1);
		for (int i = start; i <= end; i++)
			if (slots[i] != null && slots[i].transform.childCount == 0)
				return slots[i];
		return null;
	}

	public int GetSlotIndexOfItem(InventoryItem item)
	{
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] == null) continue;
			if (slots[i].GetComponentInChildren<InventoryItem>() == item)
				return i;
		}
		return -1;
	}

	// Returns the number of items actually added.
	public int AddItemPartial(Item item, int count)
	{
		if (item == null || count <= 0) return 0;

		if (item is WaterContainerItem)
		{
			int added = 0;
			for (int i = 0; i < slots.Length && added < count; i++)
			{
				if (slots[i].GetComponentInChildren<InventoryItem>() != null) continue;
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
				var slotItem = slots[i].GetComponentInChildren<InventoryItem>();
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
			if (slots[i].GetComponentInChildren<InventoryItem>() != null) continue;
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
			var slotItem = slot.GetComponentInChildren<InventoryItem>();
			if (slotItem == null || slotItem.item == null || slotItem.item.name != itemName || slotItem.count <= 0) continue;

			int remove = Mathf.Min(remaining, slotItem.count);
			slotItem.count -= remove;
			slotItem.RefreshCount();
			remaining -= remove;

			if (slotItem.count <= 0)
				Object.Destroy(slotItem.gameObject);
		}
	}

	public bool TryStoreInventoryItem(InventoryItem sourceItem)
	{
		if (sourceItem == null || sourceItem.item == null) return false;

		if (sourceItem is WaterContainerInventoryItem)
		{
			var empty = GetEmptySlot();
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
				Object.Destroy(sourceItem.gameObject);

			return true;
		}

		if (!item.stackable)
		{
			var empty = GetEmptySlot();
			if (empty == null) return false;
			sourceItem.transform.SetParent(empty.transform, false);
			sourceItem.transform.localPosition = Vector3.zero;
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
			var slotItem = slots[i].GetComponentInChildren<InventoryItem>();
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
			if (slots[i].transform.childCount != 0) continue;
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
			if (waterContainerPrefab == null) return;
			var go = Object.Instantiate(waterContainerPrefab, slot.transform);
			var wii = go.GetComponent<WaterContainerInventoryItem>();
			wii.InitialiseWaterContainer(wc);
			wii.currentFill = 0;
			wii.isSaltWater = false;
			wii.UpdateSprite();
			wii.count = 1;
			wii.RefreshCount();
		}
		else
		{
			if (itemPrefab == null) return;
			var go = Object.Instantiate(itemPrefab, slot.transform);
			var ii = go.GetComponent<InventoryItem>();
			ii.InitialiseItem(item);
			ii.count = count;
			ii.RefreshCount();
		}
	}

	public void SpawnWaterContainer(WaterContainerItem wc, InventorySlot slot, int fill, bool isSaltWater)
	{
		if (wc == null || slot == null || waterContainerPrefab == null) return;
		var go = Object.Instantiate(waterContainerPrefab, slot.transform);
		var wii = go.GetComponent<WaterContainerInventoryItem>();
		wii.InitialiseWaterContainer(wc);
		wii.currentFill = Mathf.Clamp(fill, 0, wc.maxFillCapacity);
		wii.isSaltWater = isSaltWater;
		wii.UpdateSprite();
		wii.count = 1;
		wii.RefreshCount();
	}

	public List<InventorySlotData> CaptureState()
	{
		var result = new List<InventorySlotData>(slots.Length);
		for (int i = 0; i < slots.Length; i++)
		{
			var invItem = slots[i].GetComponentInChildren<InventoryItem>();
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
			for (int c = slot.transform.childCount - 1; c >= 0; c--)
				Object.Destroy(slot.transform.GetChild(c).gameObject);

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

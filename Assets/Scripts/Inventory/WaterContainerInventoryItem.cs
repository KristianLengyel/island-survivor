using UnityEngine;

public class WaterContainerInventoryItem : InventoryItem
{
	[HideInInspector] public int currentFill = 0;
	[HideInInspector] public bool isSaltWater = false;

	public new WaterContainerItem item => base.item as WaterContainerItem;

	public void InitialiseWaterContainer(WaterContainerItem newItem)
	{
		base.InitialiseItem(newItem);
		currentFill = 0;
		isSaltWater = false;
		UpdateSprite();
		RefreshCount();
	}

	public bool Fill(int amount, bool withSaltWater = false)
	{
		if (item.maxFillCapacity <= 0 || amount <= 0) return false;

		if (currentFill > 0 && isSaltWater != withSaltWater)
		{
			return false;
		}

		int newFill = Mathf.Clamp(currentFill + amount, 0, item.maxFillCapacity);
		if (newFill == currentFill) return false;

		currentFill = newFill;
		isSaltWater = withSaltWater;
		UpdateSprite();
		RefreshCount();
		return true;
	}

	public bool Drink()
	{
		if (item.maxFillCapacity > 0 && currentFill > 0)
		{
			currentFill--;
			UpdateSprite();
			RefreshCount();
			return true;
		}
		return false;
	}

	public void UpdateSprite()
	{
		if (item.maxFillCapacity > 0 && item.fillSprites != null && item.fillSprites.Length > 0)
		{
			int spriteIndex;
			if (item.maxFillCapacity == 1 || item.maxFillCapacity == 3)
			{
				spriteIndex = currentFill;
			}
			else
			{
				spriteIndex = Mathf.RoundToInt((float)currentFill / item.maxFillCapacity * (item.fillSprites.Length - 1));
			}
			spriteIndex = Mathf.Clamp(spriteIndex, 0, item.fillSprites.Length - 1);
			image.sprite = isSaltWater ? item.saltFillSprites[spriteIndex] : item.fillSprites[spriteIndex];
		}
		else
		{
			image.sprite = item.image;
		}
	}

	protected override void SplitStack(InventorySlot targetSlot)
	{
		if (count <= 1) return;
		if (targetSlot.transform.childCount > 0) return;

		int halfCount = count / 2;
		InventoryManager inventoryManager = GameManager.Instance.InventoryManager;

		count -= halfCount;
		RefreshCount();

		inventoryManager.SpawnNewItem(item, targetSlot, halfCount);

		WaterContainerInventoryItem newItem = targetSlot.GetComponentInChildren<WaterContainerInventoryItem>();
		if (newItem != null)
		{
			newItem.currentFill = 0;
			newItem.isSaltWater = false;
			newItem.UpdateSprite();
			newItem.RefreshCount();
		}
	}
}

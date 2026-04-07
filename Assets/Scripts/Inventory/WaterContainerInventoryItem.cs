using UnityEngine;

public class WaterContainerInventoryItem : InventoryItem
{
	public int currentFill = 0;
	public bool isSaltWater = false;

	public new WaterContainerItem item => base.item as WaterContainerItem;

	public void InitialiseWaterContainer(WaterContainerItem newItem)
	{
		base.InitialiseItem(newItem);
		currentFill = 0;
		isSaltWater = false;
		RefreshCount();
	}

	public bool Fill(int amount, bool withSaltWater = false)
	{
		if (item == null || item.maxFillCapacity <= 0 || amount <= 0) return false;
		if (currentFill > 0 && isSaltWater != withSaltWater) return false;

		int newFill = Mathf.Clamp(currentFill + amount, 0, item.maxFillCapacity);
		if (newFill == currentFill) return false;

		currentFill = newFill;
		isSaltWater = withSaltWater;
		RefreshCount();
		return true;
	}

	public bool Drink()
	{
		if (item != null && item.maxFillCapacity > 0 && currentFill > 0)
		{
			currentFill--;
			RefreshCount();
			return true;
		}
		return false;
	}

	public override Sprite GetSprite()
	{
		if (item == null || item.maxFillCapacity <= 0 || item.fillSprites == null || item.fillSprites.Length == 0)
			return base.item?.image;

		int spriteIndex;
		if (item.maxFillCapacity == 1 || item.maxFillCapacity == 3)
			spriteIndex = currentFill;
		else
			spriteIndex = Mathf.RoundToInt((float)currentFill / item.maxFillCapacity * (item.fillSprites.Length - 1));

		spriteIndex = Mathf.Clamp(spriteIndex, 0, item.fillSprites.Length - 1);
		return isSaltWater ? item.saltFillSprites[spriteIndex] : item.fillSprites[spriteIndex];
	}
}

using UnityEngine;

public class InventoryItem
{
	public Item item;
	public int count;
	public InventorySlot slot;

	public virtual void InitialiseItem(Item newItem)
	{
		item = newItem;
		RefreshCount();
	}

	public void RefreshCount()
	{
		slot?.RefreshVisual();
	}

	public virtual Sprite GetSprite()
	{
		return item?.image;
	}
}

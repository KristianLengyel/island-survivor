using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
	public Image image;
	public Sprite slotImage, selectedSlotImage, highlightedSlotImage;

	private bool isSelected = false;

	private void Awake()
	{
		Deselect();
	}

	public void Deselect()
	{
		isSelected = false;
		image.sprite = slotImage;
	}

	public void Select()
	{
		isSelected = true;
		image.sprite = selectedSlotImage;
	}

	public void OnDrop(PointerEventData eventData)
	{
		InventoryItem droppedItem = eventData.pointerDrag.GetComponent<InventoryItem>();
		if (droppedItem == null) return;

		if (transform.childCount == 0)
		{
			droppedItem.parentAfterDrag = transform;
		}
		else
		{
			InventoryItem currentItem = GetComponentInChildren<InventoryItem>();
			if (currentItem != null && currentItem.item == droppedItem.item && currentItem.item.stackable)
			{
				int totalAmount = currentItem.count + droppedItem.count;
				int remainingAmount = totalAmount - GameManager.Instance.InventoryManager.maxStackedItems;

				if (remainingAmount > 0)
				{
					currentItem.count = GameManager.Instance.InventoryManager.maxStackedItems;
					droppedItem.count = remainingAmount;
					droppedItem.RefreshCount();
					currentItem.RefreshCount();
				}
				else
				{
					currentItem.count = totalAmount;
					droppedItem.count = 0;
					droppedItem.RefreshCount();
					Destroy(droppedItem.gameObject);
					currentItem.RefreshCount();
				}
			}
			else
			{
				Transform currentParent = currentItem.transform.parent;
				currentItem.transform.SetParent(droppedItem.parentAfterDrag);
				droppedItem.parentAfterDrag = currentParent;
			}
		}

		GameManager.Instance.InventoryManager.MarkCountsDirty();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!isSelected)
		{
			image.sprite = highlightedSlotImage;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!isSelected)
		{
			image.sprite = slotImage;
		}
	}
}

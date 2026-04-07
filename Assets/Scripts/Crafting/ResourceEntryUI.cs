using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceEntryUI : MonoBehaviour
{
	[SerializeField] private Image resourceIcon;
	[SerializeField] private TMP_Text resourceName;
	[SerializeField] private TMP_Text resourceCount;

	private InventoryManager inventoryManager;

	public void Setup(Item resource, int amount, InventoryManager inventoryManager)
	{
		this.inventoryManager = inventoryManager;

		if (resource != null)
		{
			resourceIcon.sprite = resource.image;
			resourceName.text = resource.name;
			resourceCount.text = amount.ToString();

			int availableCount = GetAvailableCount(resource);
			bool hasEnough = availableCount >= amount;
			Color textColor = hasEnough ? Color.white : Color.red;
			resourceName.color = Color.white;
			resourceCount.color = textColor;
		}
	}

	private int GetAvailableCount(Item resource)
	{
		int totalAvailable = 0;
		foreach (var slot in inventoryManager.inventorySlots)
		{
			InventoryItem item = slot.CurrentItem;
			if (item != null && item.item != null && item.item.name == resource.name)
				totalAvailable += item.count;
		}
		return totalAvailable;
	}
}
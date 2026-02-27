using System.Collections.Generic;
using UnityEngine;

public class HookCollision : MonoBehaviour
{
	private Transform hookTransform;
	public int hookedItemCount = 0;
	public int maxHookedItems = 6;

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("collectable") && hookedItemCount < maxHookedItems)
		{
			other.transform.SetParent(transform);
			other.transform.localPosition = Vector3.zero;
			other.transform.localRotation = Quaternion.identity;
			SpawnableMovement spawnableMovement = other.GetComponent<SpawnableMovement>();
			if (spawnableMovement != null)
			{
				spawnableMovement.StartFollowingHook(transform);
			}

			ClickableObject clickableObject = other.GetComponent<ClickableObject>();
			if (clickableObject != null)
			{
				clickableObject.SetHooked(true);
				hookedItemCount++;
			}

			hookTransform = transform;
		}
	}

	public void CollectItems()
	{
		if (hookTransform != null)
		{
			List<Transform> itemsToRemove = new List<Transform>();

			foreach (Transform itemTransform in hookTransform)
			{
				ClickableObject clickableObject = itemTransform.GetComponent<ClickableObject>();
				if (clickableObject != null)
				{
					if (clickableObject.item.name == "Barrel")
					{
						List<Item> loot = GameManager.Instance.InventoryManager.GenerateLoot();
						foreach (Item lootItem in loot)
						{
							GameManager.Instance.InventoryManager.AddItem(lootItem);
						}
					}
					else
					{
						bool canAdd = GameManager.Instance.InventoryManager.AddItem(clickableObject.item);
						if (canAdd)
						{
							itemsToRemove.Add(itemTransform);
						}
					}
				}
			}

			foreach (Transform itemTransform in itemsToRemove)
			{
				Destroy(itemTransform.gameObject);
				hookedItemCount--;
			}
		}
	}

	public bool HasItems()
	{
		return hookedItemCount > 0;
	}
}

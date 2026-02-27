using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CatchingNet : MonoBehaviour, IInteractable
{
	public int MaxCaughtItems = 10;
	private List<GameObject> caughtItems = new List<GameObject>();

	public Sprite normalSprite;
	public Sprite highlightedSprite;

	public float interactionRange = 2f;

	private SpriteRenderer spriteRenderer;
	private InventoryManager inventoryManager;
	private bool isHighlighted;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (spriteRenderer != null) spriteRenderer.sprite = normalSprite;
	}

	private void Start()
	{
		inventoryManager = GameManager.Instance?.InventoryManager;
		if (inventoryManager == null) enabled = false;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("collectable") && caughtItems.Count < MaxCaughtItems && !caughtItems.Contains(other.gameObject))
		{
			caughtItems.Add(other.gameObject);
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
			}
		}
	}

	public void CollectItems()
	{
		for (int i = caughtItems.Count - 1; i >= 0; i--)
		{
			GameObject item = caughtItems[i];
			if (item != null)
			{
				ClickableObject clickableObject = item.GetComponent<ClickableObject>();
				if (clickableObject != null)
				{
					if (clickableObject.item.name == "Barrel")
					{
						List<Item> loot = inventoryManager.GenerateLoot();
						foreach (Item lootItem in loot)
						{
							inventoryManager.AddItem(lootItem);
						}
						caughtItems.RemoveAt(i);
						Destroy(item);
					}
					else if (inventoryManager.AddItem(clickableObject.item))
					{
						caughtItems.RemoveAt(i);
						Destroy(item);
					}
				}
			}
			else
			{
				caughtItems.RemoveAt(i);
			}
		}
	}

	public void SetHighlighted(bool highlight)
	{
		if (isHighlighted != highlight)
		{
			isHighlighted = highlight;
			if (spriteRenderer != null)
			{
				spriteRenderer.sprite = highlight ? highlightedSprite : normalSprite;
			}
		}
	}

	public void Interact()
	{
		CollectItems();
	}

	public float GetInteractionRange()
	{
		return interactionRange;
	}

	public Transform GetTransform()
	{
		return transform;
	}

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		if (spriteRenderer == null || spriteRenderer.sprite == null) return false;

		Bounds spriteBounds = spriteRenderer.bounds;
		Vector2 spriteMin = spriteBounds.min;
		Vector2 spriteMax = spriteBounds.max;

		return mouseWorldPos.x >= spriteMin.x && mouseWorldPos.x <= spriteMax.x &&
			   mouseWorldPos.y >= spriteMin.y && mouseWorldPos.y <= spriteMax.y;
	}
}
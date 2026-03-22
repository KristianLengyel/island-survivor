using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CatchingNet : MonoBehaviour, IInteractable, ISaveableComponent
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

			other.enabled = false;

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
		return InteractableUtil.IsMouseOverBounds(spriteRenderer, mouseWorldPos);
	}

	// --- ISaveableComponent ---

	public string SaveKey => "CatchingNet";

	[System.Serializable]
	private class NetState
	{
		public List<string> caughtItemIds = new List<string>();
	}

	public string CaptureStateJson()
	{
		var state = new NetState();
		foreach (var go in caughtItems)
		{
			if (go == null) continue;
			var clickable = go.GetComponent<ClickableObject>();
			if (clickable != null && clickable.item != null)
				state.caughtItemIds.Add(clickable.item.name);
		}
		return JsonUtility.ToJson(state);
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var state = JsonUtility.FromJson<NetState>(json);
		if (state == null || state.caughtItemIds == null || state.caughtItemIds.Count == 0) return;

		var saveMgr = FindAnyObjectByType<SaveGameManager>();
		if (saveMgr == null || saveMgr.itemDatabase == null) return;

		// Caught items are transient world GameObjects (collectable prefabs) whose Item.prefab
		// is not set. Rather than trying to re-spawn them visually, add them straight to inventory.
		var inventory = GameManager.Instance?.InventoryManager;
		if (inventory == null) return;

		foreach (var itemId in state.caughtItemIds)
		{
			var item = saveMgr.itemDatabase.Get(itemId);
			if (item != null) inventory.AddItem(item, 1, showNotification: false);
		}
	}
}
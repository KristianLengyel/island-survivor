using UnityEngine;

[RequireComponent(typeof(ClickableObject))]
public class CollectablePickup : MonoBehaviour, IInteractable
{
	[SerializeField] private float interactionRange = 2f;
	[SerializeField] private bool useLootTable;

	private ClickableObject _clickable;
	private Highlightable _highlightable;
	private SpriteRenderer _spriteRenderer;
	private InventoryManager _inventory;

	private void Awake()
	{
		_clickable = GetComponent<ClickableObject>();
		_highlightable = GetComponent<Highlightable>();
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void CacheInventory()
	{
		if (_inventory == null && GameManager.Instance != null)
			_inventory = GameManager.Instance.InventoryManager;
	}

	public void SetHighlighted(bool highlight)
	{
		if (_clickable != null && _clickable.isCaught) return;
		if (_highlightable != null)
			_highlightable.SetHighlight(highlight);
	}

	public void Interact()
	{
		if (_clickable != null && _clickable.isCaught) return;

		CacheInventory();
		if (_inventory == null) return;

		if (useLootTable)
		{
			var loot = _inventory.GenerateLoot();
			if (loot != null)
			{
				foreach (var lootItem in loot)
					_inventory.AddItem(lootItem);
			}
		}
		else
		{
			if (_clickable == null || _clickable.item == null) return;
			_inventory.AddItemPartial(_clickable.item, 1, true);
		}

		Destroy(gameObject);
	}

	public float GetInteractionRange() => interactionRange;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		if (_spriteRenderer == null) return false;
		var b = _spriteRenderer.bounds;
		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}
}

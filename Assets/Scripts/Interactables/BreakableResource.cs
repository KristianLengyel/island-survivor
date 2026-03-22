using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BreakableResource : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Interaction")]
	[SerializeField] private float interactionRange = 2f;
	[SerializeField] private SpriteRenderer outlineRenderer;

	[Header("Mouse Hit Sprite")]
	[SerializeField] private SpriteRenderer hitSpriteRenderer;

	[Header("Tool Requirement")]
	[SerializeField] private Item requiredTool;

	[Header("Break Rules")]
	[SerializeField] private int maxHealth = 5;
	[SerializeField] private float cooldown = 0.25f;
	[SerializeField] private float staminaCostPerHit = 0f;

	[Header("Visual Stages (optional)")]
	[SerializeField] private Sprite[] breakStageSprites;

	[Header("Drops (go to inventory)")]
	[SerializeField] private ResourceDrop[] drops;
	[SerializeField] private bool showInventoryNotification = true;

	private Collider2D col;
	private Highlightable _highlightable;

	private int currentHealth;
	private bool isBroken;
	private float nextAllowedTime;

	private InventoryManager inventory;
	private PlayerStats playerStats;
	private PlayerController playerController;

	public string SaveKey => "BreakableResource";

	private void Awake()
	{
		col = GetComponent<Collider2D>();
		_highlightable = GetComponent<Highlightable>();
		ResolveRenderers();

		currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);

		CacheRefs();
		RefreshStageSprite();
	}

	private void OnEnable()
	{
		CacheRefs();

		if (isBroken)
		{
			Destroy(gameObject);
			return;
		}

		RefreshStageSprite();
	}

	private void CacheRefs()
	{
		if (inventory == null) inventory = GameManager.Instance != null ? GameManager.Instance.InventoryManager : null;
		if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
		if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
	}

	private void ResolveRenderers()
	{
		GameObject _;
		InteractableUtil.ResolveOutlineRenderer(transform, ref outlineRenderer, out _);

		if (hitSpriteRenderer == null)
		{
			var sr = GetComponent<SpriteRenderer>();
			if (sr != null && sr != outlineRenderer) hitSpriteRenderer = sr;
		}

		if (hitSpriteRenderer == null)
		{
			var all = GetComponentsInChildren<SpriteRenderer>(true);
			for (int i = 0; i < all.Length; i++)
			{
				var r = all[i];
				if (r == null) continue;
				if (outlineRenderer != null && r == outlineRenderer) continue;
				hitSpriteRenderer = r;
				break;
			}
		}
	}

	// ----------------- IInteractable -----------------

	public void SetHighlighted(bool highlight)
	{
		if (_highlightable != null)
		{
			_highlightable.SetHighlight(highlight && !isBroken && HasRequiredTool());
			return;
		}

		if (outlineRenderer == null) return;
		if (!outlineRenderer.gameObject.activeSelf) outlineRenderer.gameObject.SetActive(true);

		if (!highlight)
		{
			outlineRenderer.enabled = false;
			return;
		}

		outlineRenderer.enabled = !isBroken && HasRequiredTool();
	}

	public void Interact()
	{
		if (isBroken) return;
		if (Time.time < nextAllowedTime) return;

		CacheRefs();

		if (!HasRequiredTool()) return;

		if (playerStats != null && staminaCostPerHit > 0f)
		{
			if (!playerStats.SpendStamina(staminaCostPerHit)) return;
		}

		if (playerController != null)
		{
			Vector2 toResource = (Vector2)(transform.position - playerController.transform.position);
			if (toResource.sqrMagnitude > 0.001f)
				playerController.SetFacingDirection(toResource.normalized);
		}

		nextAllowedTime = Time.time + Mathf.Max(0f, cooldown);

		ApplyHit(1);
	}

	public float GetInteractionRange() => interactionRange;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		return InteractableUtil.IsMouseOverBounds(hitSpriteRenderer, mouseWorldPos);
	}

	// ----------------- Logic -----------------

	private void ApplyHit(int damage)
	{
		if (isBroken) return;

		int d = Mathf.Max(0, damage);
		currentHealth = Mathf.Max(0, currentHealth - d);

		RefreshStageSprite();

		if (AudioManager.instance != null)
			AudioManager.instance.PlaySound("WoodChop", Random.Range(0.9f, 1.1f));

		if (currentHealth <= 0)
			BreakNow();
	}

	private void BreakNow()
	{
		if (isBroken) return;

		isBroken = true;

		if (_highlightable != null)
			_highlightable.SetHighlight(false);
		else if (outlineRenderer != null)
			outlineRenderer.enabled = false;

		if (col != null)
			col.enabled = false;

		AddDropsToInventory();

		Destroy(gameObject);
	}

	private void AddDropsToInventory()
	{
		if (inventory == null) return;
		if (drops == null || drops.Length == 0) return;

		for (int i = 0; i < drops.Length; i++)
		{
			var d = drops[i];
			if (d == null || d.item == null) continue;

			int amount = d.RollAmount();
			if (amount <= 0) continue;

			inventory.AddItemPartial(d.item, amount, showInventoryNotification);
		}
	}

	private void RefreshStageSprite()
	{
		if (hitSpriteRenderer == null) return;
		if (breakStageSprites == null || breakStageSprites.Length == 0) return;

		if (isBroken)
		{
			hitSpriteRenderer.sprite = breakStageSprites[breakStageSprites.Length - 1];
			return;
		}

		int mh = Mathf.Max(1, maxHealth);
		currentHealth = Mathf.Clamp(currentHealth, 0, mh);

		float pct = Mathf.Clamp01((float)currentHealth / mh);

		int last = breakStageSprites.Length - 1;
		int index = Mathf.Clamp(Mathf.RoundToInt((1f - pct) * last), 0, last);

		hitSpriteRenderer.sprite = breakStageSprites[index];
	}

	private bool HasRequiredTool()
	{
		if (requiredTool == null) return true;

		if (inventory == null) inventory = GameManager.Instance != null ? GameManager.Instance.InventoryManager : null;
		if (inventory == null) return false;

		var selected = inventory.GetSelectedItem();
		return selected != null && selected == requiredTool;
	}

	// ----------------- Save System -----------------

	[System.Serializable]
	private class BreakableResourceState
	{
		public int hp;
		public bool broken;
	}

	public string CaptureStateJson()
	{
		return JsonUtility.ToJson(new BreakableResourceState
		{
			hp = currentHealth,
			broken = isBroken
		});
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var st = JsonUtility.FromJson<BreakableResourceState>(json);
		if (st == null) return;

		int mh = Mathf.Max(1, maxHealth);

		currentHealth = Mathf.Clamp(st.hp, 0, mh);
		isBroken = st.broken;

		if (_highlightable != null)
			_highlightable.SetHighlight(false);

		if (outlineRenderer != null)
		{
			if (!outlineRenderer.gameObject.activeSelf) outlineRenderer.gameObject.SetActive(true);
			outlineRenderer.enabled = false;
		}

		if (isBroken)
		{
			Destroy(gameObject);
			return;
		}

		if (col != null) col.enabled = true;
		if (hitSpriteRenderer != null) hitSpriteRenderer.enabled = true;

		RefreshStageSprite();
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (maxHealth < 1) maxHealth = 1;
		if (interactionRange < 0f) interactionRange = 0f;
		if (cooldown < 0f) cooldown = 0f;
		if (staminaCostPerHit < 0f) staminaCostPerHit = 0f;

		if (!Application.isPlaying)
		{
			if (currentHealth <= 0) currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
			ResolveRenderers();
			RefreshStageSprite();
		}
	}
#endif
}

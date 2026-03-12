using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PalmTree : MonoBehaviour, IInteractable, ISaveableComponent
{
	[Header("Interaction")]
	[SerializeField] private float interactionRange = 2f;
	[SerializeField] private SpriteRenderer outlineRenderer;

	[Header("Mouse Hit Sprite")]
	[SerializeField] private SpriteRenderer hitSpriteRenderer;

	[Header("Tool Requirement")]
	[SerializeField] private Item requiredTool;

	[Header("Axe Rules")]
	[SerializeField] private float swingWindup = 0.12f;
	[SerializeField] private float cooldown = 0.35f;
	[SerializeField] private float staminaCostPerHit = 8f;

	[Header("Tree State")]
	[SerializeField] private int maxHealth = 8;
	[SerializeField] private Sprite[] chopStageSprites;

	[Header("Fall")]
	[SerializeField] private bool disableOnFell = true;
	[SerializeField] private float fallHideDelay = 0.05f;

	[Header("Feedback")]
	[SerializeField] private float shakeDuration = 0.12f;
	[SerializeField] private float shakeStrength = 0.05f;

	[Header("Drops (go to inventory)")]
	[SerializeField] private Item dropItem;
	[SerializeField] private int dropCount = 0;
	[SerializeField] private bool showInventoryNotification = true;

	private Collider2D col;

	private int currentHealth;
	private bool isFelled;
	private bool isSwinging;
	private float nextAllowedTime;

	private Vector3 baseLocalPos;

	private InventoryManager inventory;
	private PlayerStats playerStats;
	private PlayerCarryController carryController;

	private Coroutine swingCoroutine;
	private Coroutine fallCoroutine;

	public string SaveKey => "PalmTree";

	private void Awake()
	{
		col = GetComponent<Collider2D>();
		ResolveRenderers();

		if (outlineRenderer != null)
		{
			if (!outlineRenderer.gameObject.activeSelf) outlineRenderer.gameObject.SetActive(true);
			outlineRenderer.enabled = false;
		}

		currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
		baseLocalPos = transform.localPosition;

		CacheRefs();
		RefreshStageSprite();
	}

	private void OnEnable()
	{
		baseLocalPos = transform.localPosition;
		CacheRefs();

		if (isFelled)
			ApplyFelledStateVisualOnly();
		else
			RefreshStageSprite();
	}

	private void OnDisable()
	{
		if (swingCoroutine != null) { StopCoroutine(swingCoroutine); swingCoroutine = null; }
		if (fallCoroutine != null) { StopCoroutine(fallCoroutine); fallCoroutine = null; }
		isSwinging = false;
		transform.localPosition = baseLocalPos;
	}

	private void CacheRefs()
	{
		if (inventory == null) inventory = GameManager.Instance != null ? GameManager.Instance.InventoryManager : null;
		if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
		if (carryController == null) carryController = FindAnyObjectByType<PlayerCarryController>();
	}

	private void ResolveRenderers()
	{
		if (outlineRenderer == null)
		{
			var t = transform.Find("Outline");
			if (t != null) outlineRenderer = t.GetComponent<SpriteRenderer>();
		}

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
				if (r.sprite == null && chopStageSprites != null && chopStageSprites.Length > 0) continue;
				hitSpriteRenderer = r;
				break;
			}
		}
	}

	public void SetHighlighted(bool highlight)
	{
		if (outlineRenderer == null) return;
		if (!outlineRenderer.gameObject.activeSelf) outlineRenderer.gameObject.SetActive(true);

		if (!highlight)
		{
			outlineRenderer.enabled = false;
			return;
		}

		outlineRenderer.enabled = !isFelled && HasRequiredTool();
	}

	public void Interact()
	{
		if (isFelled) return;
		if (isSwinging) return;
		if (Time.time < nextAllowedTime) return;

		CacheRefs();

		if (!HasRequiredTool()) return;

		if (playerStats != null && staminaCostPerHit > 0f)
		{
			if (!playerStats.SpendStamina(staminaCostPerHit)) return;
		}

		if (carryController == null) return;
		if (!carryController.TryChop()) return;

		nextAllowedTime = Time.time + Mathf.Max(0f, cooldown);

		if (swingCoroutine != null) StopCoroutine(swingCoroutine);
		swingCoroutine = StartCoroutine(SwingRoutine());
	}

	public float GetInteractionRange() => interactionRange;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		if (hitSpriteRenderer == null || hitSpriteRenderer.sprite == null) return false;
		var b = hitSpriteRenderer.bounds;
		return mouseWorldPos.x >= b.min.x && mouseWorldPos.x <= b.max.x &&
			   mouseWorldPos.y >= b.min.y && mouseWorldPos.y <= b.max.y;
	}

	private IEnumerator SwingRoutine()
	{
		isSwinging = true;

		if (shakeDuration > 0f && shakeStrength > 0f)
			yield return ShakeRoutine(shakeDuration, shakeStrength);

		if (swingWindup > 0f)
			yield return new WaitForSeconds(swingWindup);

		ApplyHit(1);

		isSwinging = false;
		swingCoroutine = null;
	}

	private IEnumerator ShakeRoutine(float duration, float strength)
	{
		float t = 0f;
		while (t < duration)
		{
			t += Time.deltaTime;
			float ox = Random.Range(-strength, strength);
			float oy = Random.Range(-strength, strength);
			transform.localPosition = baseLocalPos + new Vector3(ox, oy, 0f);
			yield return null;
		}

		transform.localPosition = baseLocalPos;
	}

	private void ApplyHit(int damage)
	{
		if (isFelled) return;

		int d = Mathf.Max(0, damage);
		currentHealth = Mathf.Max(0, currentHealth - d);

		RefreshStageSprite();

		if (AudioManager.instance != null)
			AudioManager.instance.PlaySound("WoodChop", Random.Range(0.9f, 1.1f));

		if (currentHealth <= 0)
			Fall();
	}

	private void Fall()
	{
		if (isFelled) return;

		isFelled = true;

		if (outlineRenderer != null)
			outlineRenderer.enabled = false;

		if (dropItem != null && dropCount > 0)
			AddDropsToInventory(dropItem, dropCount);

		Destroy(gameObject);
	}

	private void ApplyFelledStateVisualOnly()
	{
		if (col != null) col.enabled = false;

		if (chopStageSprites != null && chopStageSprites.Length > 0 && hitSpriteRenderer != null)
			hitSpriteRenderer.sprite = chopStageSprites[chopStageSprites.Length - 1];
	}

	private IEnumerator FallDisableRoutine()
	{
		yield return new WaitForSeconds(Mathf.Max(0f, fallHideDelay));

		if (hitSpriteRenderer != null) hitSpriteRenderer.enabled = false;

		gameObject.SetActive(false);
	}

	private void AddDropsToInventory(Item item, int count)
	{
		if (item == null || count <= 0) return;

		if (inventory == null) inventory = GameManager.Instance != null ? GameManager.Instance.InventoryManager : null;
		if (inventory == null) return;

		inventory.AddItemPartial(item, count, showInventoryNotification);
	}

	private void RefreshStageSprite()
	{
		if (hitSpriteRenderer == null) return;
		if (chopStageSprites == null || chopStageSprites.Length == 0) return;

		if (isFelled)
		{
			hitSpriteRenderer.sprite = chopStageSprites[chopStageSprites.Length - 1];
			return;
		}

		int mh = Mathf.Max(1, maxHealth);
		currentHealth = Mathf.Clamp(currentHealth, 0, mh);

		float pct = Mathf.Clamp01((float)currentHealth / mh);

		int last = chopStageSprites.Length - 1;
		int index = Mathf.Clamp(Mathf.RoundToInt((1f - pct) * last), 0, last);

		hitSpriteRenderer.sprite = chopStageSprites[index];
	}

	private bool HasRequiredTool()
	{
		if (requiredTool == null) return true;

		if (inventory == null) inventory = GameManager.Instance != null ? GameManager.Instance.InventoryManager : null;
		if (inventory == null) return false;

		var selected = inventory.GetSelectedItem();
		return selected != null && selected == requiredTool;
	}

	[System.Serializable]
	private class PalmTreeState
	{
		public int hp;
		public bool felled;
	}

	public string CaptureStateJson()
	{
		return JsonUtility.ToJson(new PalmTreeState
		{
			hp = currentHealth,
			felled = isFelled
		});
	}

	public void RestoreStateJson(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		var st = JsonUtility.FromJson<PalmTreeState>(json);
		if (st == null) return;

		int mh = Mathf.Max(1, maxHealth);

		currentHealth = Mathf.Clamp(st.hp, 0, mh);
		isFelled = st.felled;

		if (outlineRenderer != null)
		{
			if (!outlineRenderer.gameObject.activeSelf) outlineRenderer.gameObject.SetActive(true);
			outlineRenderer.enabled = false;
		}

		if (isFelled)
		{
			ApplyFelledStateVisualOnly();

			if (disableOnFell)
			{
				if (col != null) col.enabled = false;
				if (hitSpriteRenderer != null) hitSpriteRenderer.enabled = false;
				gameObject.SetActive(false);
			}

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
		if (swingWindup < 0f) swingWindup = 0f;
		if (shakeDuration < 0f) shakeDuration = 0f;
		if (shakeStrength < 0f) shakeStrength = 0f;
		if (fallHideDelay < 0f) fallHideDelay = 0f;
		if (dropCount < 0) dropCount = 0;

		if (!Application.isPlaying)
		{
			if (currentHealth <= 0) currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
			ResolveRenderers();
			RefreshStageSprite();
		}
	}
#endif
}

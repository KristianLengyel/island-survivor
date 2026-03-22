using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerCarryController : MonoBehaviour
{
	private Animator animator;
	private Camera mainCamera;

	public bool IsHolding { get; private set; }
	public bool IsBusy { get; private set; }

	[SerializeField] private Transform holdPoint;
	[SerializeField] private SpriteRenderer heldSpriteRenderer;
	[SerializeField] private int heldSortingOrderOffset = 20;
	[SerializeField] private int behindPlayerSortingOrder = 4;
	[SerializeField] private float pickupScanRadius = 2f;
	[SerializeField] private int putDownRange = 2;

	[Header("Placement")]
	[SerializeField] private Tilemap buildingTilemap;
	[SerializeField] private Tilemap waterTilemap;

	[SerializeField] private Sprite debugHeldSprite;

	private static readonly Color GhostValid   = new Color(0f, 1f,    0f,    0.45f);
	private static readonly Color GhostInvalid = new Color(1f, 0.15f, 0.15f, 0.45f);

	private Sprite pendingHeldSprite;
	private SpriteRenderer playerSpriteRenderer;
	private SpriteRenderer ghostRenderer;

	private Carryable pendingCarryable;
	private Carryable currentCarryable;

	private Vector3Int currentGhostCell;
	private bool isGhostValid;
	private Vector3 pendingPutDownPos;

	private readonly List<Collider2D> scanBuffer   = new List<Collider2D>(16);
	private readonly List<Collider2D> overlapBuffer = new List<Collider2D>(8);

	private void Awake()
	{
		animator             = GetComponent<Animator>();
		playerSpriteRenderer = GetComponent<SpriteRenderer>();
		mainCamera           = Camera.main;

		ResolveHoldRefsOrDisable();
		ApplySorting();
		SetHeldSpriteInternal(null, false);
		CreateGhostRenderer();
	}

	private void OnDestroy()
	{
		if (ghostRenderer != null)
			Destroy(ghostRenderer.gameObject);
	}

	private void CreateGhostRenderer()
	{
		var go = new GameObject("CarryGhost");
		ghostRenderer = go.AddComponent<SpriteRenderer>();
		if (playerSpriteRenderer != null)
			ghostRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
		ghostRenderer.sortingOrder = 8;
		ghostRenderer.enabled = false;
	}

	private void ResolveHoldRefsOrDisable()
	{
		if (holdPoint == null)
		{
			var t = transform.Find("HoldPoint");
			if (t != null) holdPoint = t;
		}

		if (heldSpriteRenderer == null && holdPoint != null)
		{
			var t = holdPoint.Find("HeldSprite");
			if (t != null) heldSpriteRenderer = t.GetComponent<SpriteRenderer>();
		}

		if (holdPoint == null || heldSpriteRenderer == null)
			enabled = false;
	}

	private void ApplySorting()
	{
		if (heldSpriteRenderer == null || playerSpriteRenderer == null) return;

		heldSpriteRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
		heldSpriteRenderer.sortingOrder   = playerSpriteRenderer.sortingOrder + heldSortingOrderOffset;
	}

	public void Tick()
	{
		UpdateSortOrder();

		if (!IsHolding || IsBusy || buildingTilemap == null)
		{
			SetGhostVisible(false);
			return;
		}

		if (!mainCamera) mainCamera = Camera.main;
		if (!mainCamera) return;

		Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(GameInput.MouseScreen);
		currentGhostCell   = buildingTilemap.WorldToCell(mouseWorld);
		Vector3 cellCenter = buildingTilemap.GetCellCenterWorld(currentGhostCell);

		isGhostValid = IsCellValid(currentGhostCell);

		ghostRenderer.transform.position = cellCenter;
		ghostRenderer.sprite             = currentCarryable != null ? currentCarryable.PlacementSprite : null;
		ghostRenderer.color              = isGhostValid ? GhostValid : GhostInvalid;
		SetGhostVisible(ghostRenderer.sprite != null);
	}

	private void UpdateSortOrder()
	{
		if (heldSpriteRenderer == null || playerSpriteRenderer == null || !IsHolding) return;

		float lastMoveY = animator != null ? animator.GetFloat("lastMoveY") : 0f;
		bool facingUp   = lastMoveY > 0.5f;
		heldSpriteRenderer.sortingOrder = facingUp
			? behindPlayerSortingOrder
			: playerSpriteRenderer.sortingOrder + heldSortingOrderOffset;
	}

	private bool IsCellValid(Vector3Int cell)
	{
		if (buildingTilemap == null) return false;

		Vector3Int playerCell = buildingTilemap.WorldToCell(transform.position);
		if (Mathf.Abs(cell.x - playerCell.x) > putDownRange ||
			Mathf.Abs(cell.y - playerCell.y) > putDownRange)
			return false;

		bool hasFloor = buildingTilemap.GetTile(cell) != null;
		if (!hasFloor && waterTilemap != null && waterTilemap.GetTile(cell) != null)
			return false;

		Vector3 cellCenter = buildingTilemap.GetCellCenterWorld(cell);
		Physics2D.OverlapCircle(cellCenter, 0.3f, ContactFilter2D.noFilter, overlapBuffer);
		for (int i = 0; i < overlapBuffer.Count; i++)
		{
			var col = overlapBuffer[i];
			if (col == null) continue;
			var carryable = col.GetComponentInParent<Carryable>();
			if (carryable != null && !carryable.IsBeingCarried) return false;
			if (col.CompareTag("PlaceableObject")) return false;
		}

		return true;
	}

	private void SetGhostVisible(bool visible)
	{
		if (ghostRenderer != null)
			ghostRenderer.enabled = visible;
	}

	public void SetHeldSprite(Sprite sprite)
	{
		pendingHeldSprite = sprite;
		if (IsHolding && !IsBusy)
			SetHeldSpriteInternal(pendingHeldSprite, true);
	}

	public void ClearHeldSprite()
	{
		pendingHeldSprite = null;
		SetHeldSpriteInternal(null, false);
	}

	private void SetHeldSpriteInternal(Sprite sprite, bool enabledState)
	{
		if (heldSpriteRenderer == null) return;
		heldSpriteRenderer.sprite  = sprite;
		heldSpriteRenderer.enabled = enabledState && sprite != null;
	}

	public bool TryPickUp()
	{
		if (IsBusy || IsHolding || animator == null) return false;

		Carryable found = FindNearestCarryable();
		if (found == null) return false;

		pendingCarryable  = found;
		pendingHeldSprite = found.HeldSprite;

		IsBusy = true;
		animator.SetBool("isBusy", true);
		animator.ResetTrigger("putdown");
		animator.SetTrigger("pickup");
		return true;
	}

	private Carryable FindNearestCarryable()
	{
		Physics2D.OverlapCircle(transform.position, pickupScanRadius, ContactFilter2D.noFilter, scanBuffer);

		Carryable best      = null;
		float     bestDistSq = float.MaxValue;

		for (int i = 0; i < scanBuffer.Count; i++)
		{
			var col = scanBuffer[i];
			if (col == null) continue;

			var carryable = col.GetComponentInParent<Carryable>();
			if (carryable == null || carryable.IsBeingCarried) continue;

			float distSq = ((Vector2)carryable.transform.position - (Vector2)transform.position).sqrMagnitude;
			if (distSq > carryable.PickupRange * carryable.PickupRange) continue;

			if (distSq < bestDistSq)
			{
				bestDistSq = distSq;
				best = carryable;
			}
		}

		return best;
	}

	public bool TryPutDown()
	{
		if (IsBusy || !IsHolding || animator == null) return false;
		if (!isGhostValid) return false;

		pendingPutDownPos = buildingTilemap != null
			? buildingTilemap.GetCellCenterWorld(currentGhostCell)
			: GetFacingPutDownPosition();

		IsBusy = true;
		animator.SetBool("isBusy", true);
		animator.ResetTrigger("pickup");
		animator.SetTrigger("putdown");
		return true;
	}

	public bool TryChop()
	{
		if (IsBusy || animator == null) return false;

		IsBusy = true;
		animator.SetBool("isBusy", true);
		animator.SetTrigger("chop");
		return true;
	}

	public void OnPickupAnimationFinished()
	{
		IsHolding = true;
		IsBusy    = false;

		if (pendingCarryable != null)
		{
			currentCarryable = pendingCarryable;
			pendingCarryable = null;
			currentCarryable.OnPickedUp();
		}

		if (pendingHeldSprite == null && debugHeldSprite != null)
			pendingHeldSprite = debugHeldSprite;

		SetHeldSpriteInternal(pendingHeldSprite, true);

		if (animator != null)
		{
			animator.SetBool("isHolding", true);
			animator.SetBool("isBusy",    false);
		}
	}

	public void OnPutdownAnimationFinished()
	{
		IsHolding = false;
		IsBusy    = false;

		if (currentCarryable != null)
		{
			currentCarryable.OnPutDown(pendingPutDownPos);
			currentCarryable = null;
		}

		SetGhostVisible(false);
		SetHeldSpriteInternal(null, false);
		pendingHeldSprite = null;

		if (animator != null)
		{
			animator.SetBool("isHolding", false);
			animator.SetBool("isBusy",    false);
		}
	}

	public void OnChopAnimationFinished()
	{
		IsBusy = false;
		if (animator != null)
			animator.SetBool("isBusy", false);
	}

	private Vector3 GetFacingPutDownPosition()
	{
		if (animator == null) return transform.position;

		float lx     = animator.GetFloat("lastMoveX");
		float ly     = animator.GetFloat("lastMoveY");
		Vector2 facing = new Vector2(lx, ly);

		if (facing.sqrMagnitude < 0.01f) facing = Vector2.down;
		return transform.position + (Vector3)(facing.normalized * 0.6f);
	}
}

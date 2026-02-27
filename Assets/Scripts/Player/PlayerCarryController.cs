using UnityEngine;

public class PlayerCarryController : MonoBehaviour
{
	private Animator animator;

	public bool IsHolding { get; private set; }
	public bool IsBusy { get; private set; }

	[SerializeField] private Transform holdPoint;
	[SerializeField] private SpriteRenderer heldSpriteRenderer;

	[SerializeField] private int heldSortingOrderOffset = 20;
	[SerializeField] private Sprite debugHeldSprite;

	private Sprite pendingHeldSprite;
	private SpriteRenderer playerSpriteRenderer;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		playerSpriteRenderer = GetComponent<SpriteRenderer>();

		ResolveHoldRefsOrDisable();
		ApplySorting();
		SetHeldSpriteInternal(null, false);
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
		{
			enabled = false;
		}
	}

	private void ApplySorting()
	{
		if (heldSpriteRenderer == null || playerSpriteRenderer == null) return;

		heldSpriteRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
		heldSpriteRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + heldSortingOrderOffset;
	}

	public void SetHeldSprite(Sprite sprite)
	{
		pendingHeldSprite = sprite;
		if (IsHolding && !IsBusy)
		{
			SetHeldSpriteInternal(pendingHeldSprite, true);
		}
	}

	public void ClearHeldSprite()
	{
		pendingHeldSprite = null;
		SetHeldSpriteInternal(null, false);
	}

	private void SetHeldSpriteInternal(Sprite sprite, bool enabledState)
	{
		if (heldSpriteRenderer == null) return;

		heldSpriteRenderer.sprite = sprite;
		heldSpriteRenderer.enabled = enabledState && sprite != null;
	}

	public bool TryPickUp()
	{
		if (IsBusy || IsHolding || animator == null) return false;

		IsBusy = true;
		animator.SetBool("isBusy", true);
		animator.ResetTrigger("putdown");
		animator.SetTrigger("pickup");
		return true;
	}

	public bool TryPutDown()
	{
		if (IsBusy || !IsHolding || animator == null) return false;

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
		IsBusy = false;

		if (pendingHeldSprite == null && debugHeldSprite != null)
		{
			pendingHeldSprite = debugHeldSprite;
		}

		SetHeldSpriteInternal(pendingHeldSprite, true);

		if (animator != null)
		{
			animator.SetBool("isHolding", true);
			animator.SetBool("isBusy", false);
		}
	}

	public void OnPutdownAnimationFinished()
	{
		IsHolding = false;
		IsBusy = false;

		SetHeldSpriteInternal(null, false);
		pendingHeldSprite = null;

		if (animator != null)
		{
			animator.SetBool("isHolding", false);
			animator.SetBool("isBusy", false);
		}
	}

	public void OnChopAnimationFinished()
	{
		IsBusy = false;

		if (animator != null)
		{
			animator.SetBool("isBusy", false);
		}
	}
}

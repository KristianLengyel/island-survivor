using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
	private Rigidbody2D rb;
	private Camera mainCamera;

	private IInteractable currentlyHighlightedInteractable;

	private readonly Collider2D[] overlapBuffer = new Collider2D[16];
	private ContactFilter2D overlapFilter;

	private Vector3 lastMouseScreenPos;

	public void Initialize(Rigidbody2D rb, Camera mainCamera)
	{
		this.rb = rb;
		this.mainCamera = mainCamera;

		overlapFilter = new ContactFilter2D
		{
			useTriggers = true,
			useLayerMask = false,
			useDepth = false
		};
	}

	public void PrimeMousePosition()
	{
		lastMouseScreenPos = GameInput.MouseScreen;
	}

	public void Tick()
	{
		if (!mainCamera) return;

		Vector3 mousePos = GameInput.MouseScreen;
		if (float.IsInfinity(mousePos.x) || float.IsNaN(mousePos.x)) return;

		bool mouseMoved = mousePos != lastMouseScreenPos;
		bool pressedE = GameInput.InteractDown;
		if (!mouseMoved && !pressedE) return;

		lastMouseScreenPos = mousePos;

		if (currentlyHighlightedInteractable != null)
		{
			if (currentlyHighlightedInteractable as Object == null || !currentlyHighlightedInteractable.GetTransform().gameObject.activeInHierarchy)
			{
				currentlyHighlightedInteractable = null;
			}
		}

		Vector3 mouseWorldPos3 = mainCamera.ScreenToWorldPoint(mousePos);
		Vector2 mouseWorldPos = new Vector2(mouseWorldPos3.x, mouseWorldPos3.y);

		int hitCount = Physics2D.OverlapPoint(mouseWorldPos, overlapFilter, overlapBuffer);
		if (hitCount <= 0)
		{
			ClearHighlight();
			return;
		}

		IInteractable best = null;
		int bestPriority = int.MaxValue;
		float bestDistSq = float.MaxValue;

		Vector2 playerPos = rb ? rb.position : (Vector2)transform.position;

		for (int i = 0; i < hitCount; i++)
		{
			var col = overlapBuffer[i];
			if (!col) continue;

			if (col.TryGetComponent<Chest>(out var chest))
			{
				if (!chest.IsMouseOverSprite(mouseWorldPos)) continue;

				Vector2 tPos = chest.GetTransform().position;
				float distSq = (tPos - playerPos).sqrMagnitude;
				float range = chest.GetInteractionRange();
				if (distSq > range * range) continue;

				int priority = 0;
				if (priority < bestPriority || (priority == bestPriority && distSq < bestDistSq))
				{
					best = chest;
					bestPriority = priority;
					bestDistSq = distSq;
				}
				continue;
			}

			if (col.TryGetComponent<Planter>(out var planter))
			{
				if (!planter.IsMouseOverSprite(mouseWorldPos)) continue;

				Vector2 tPos = planter.GetTransform().position;
				float distSq = (tPos - playerPos).sqrMagnitude;
				float range = planter.GetInteractionRange();
				if (distSq > range * range) continue;

				int priority = 1;
				if (priority < bestPriority || (priority == bestPriority && distSq < bestDistSq))
				{
					best = planter;
					bestPriority = priority;
					bestDistSq = distSq;
				}
				continue;
			}

			var interactable = col.GetComponent<IInteractable>();
			if (interactable == null) continue;

			var t = interactable.GetTransform();
			if (t == null) continue;

			Vector2 tPos2 = t.position;
			float distSq2 = (tPos2 - playerPos).sqrMagnitude;
			float range2 = interactable.GetInteractionRange();
			if (distSq2 > range2 * range2) continue;

			int priorityOther = 2;
			if (priorityOther < bestPriority || (priorityOther == bestPriority && distSq2 < bestDistSq))
			{
				best = interactable;
				bestPriority = priorityOther;
				bestDistSq = distSq2;
			}
		}

		if (best != null)
		{
			if (best != currentlyHighlightedInteractable)
			{
				currentlyHighlightedInteractable?.SetHighlighted(false);
				best.SetHighlighted(true);
				currentlyHighlightedInteractable = best;
			}

			if (pressedE)
			{
				best.Interact();
			}
		}
		else
		{
			ClearHighlight();
		}
	}

	public void ClearHighlight()
	{
		if (currentlyHighlightedInteractable != null)
		{
			currentlyHighlightedInteractable.SetHighlighted(false);
			currentlyHighlightedInteractable = null;
		}
	}
}

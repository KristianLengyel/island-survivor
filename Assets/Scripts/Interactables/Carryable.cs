using UnityEngine;

public class Carryable : MonoBehaviour
{
	[SerializeField] private Sprite heldSprite;
	[SerializeField] private float pickupRange = 2f;

	private SpriteRenderer mainRenderer;
	private SpriteRenderer[] renderers;
	private Collider2D[] colliders;

	public Sprite HeldSprite => heldSprite;
	public Sprite PlacementSprite => mainRenderer != null ? mainRenderer.sprite : null;
	public float PickupRange => pickupRange;
	public bool IsBeingCarried { get; private set; }

	private void Awake()
	{
		mainRenderer = GetComponent<SpriteRenderer>();
		renderers = GetComponentsInChildren<SpriteRenderer>(true);
		colliders = GetComponentsInChildren<Collider2D>(true);
	}

	public void OnPickedUp()
	{
		IsBeingCarried = true;
		for (int i = 0; i < renderers.Length; i++)
			renderers[i].enabled = false;
		for (int i = 0; i < colliders.Length; i++)
			colliders[i].enabled = false;
	}

	public void OnPutDown(Vector3 worldPosition)
	{
		IsBeingCarried = false;
		transform.position = worldPosition;
		for (int i = 0; i < renderers.Length; i++)
			renderers[i].enabled = true;
		for (int i = 0; i < colliders.Length; i++)
			colliders[i].enabled = true;
	}
}

using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraMovement : MonoBehaviour
{
	public Transform player;
	public Tilemap tilemap;
	public PlayerController playerController;

	private Vector3 minBounds;
	private Vector3 maxBounds;
	private float halfHeight;
	private float halfWidth;
	private Transform hookTransform;
	private bool isTransitioning = false;
	private bool boundsReady = false;

	private void Start()
	{
		Camera camera = GetComponent<Camera>();
		halfHeight = camera.orthographicSize;
		halfWidth = halfHeight * camera.aspect;
	}

	private void LateUpdate()
	{
		if (!boundsReady || isTransitioning) return;

		Vector3 desiredPosition = hookTransform != null
			? Vector3.Lerp(player.position, hookTransform.position, 0.5f)
			: player.position;

		float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
		float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
		transform.position = new Vector3(clampedX, clampedY, transform.position.z);
	}

	public void SetBoundsFromMapData(MapDataV3 data, Tilemap referenceTilemap)
	{
		int size = data.size;
		Tilemap tm = referenceTilemap != null ? referenceTilemap : tilemap;

		if (tm != null)
		{
			minBounds = tm.CellToWorld(new Vector3Int(1, 1, 0));
			maxBounds = tm.CellToWorld(new Vector3Int(size - 1, size - 1, 0));
		}
		else
		{
			float worldMin = -(size * 0.5f) + 1f;
			float worldMax = (size * 0.5f) - 1f;
			minBounds = new Vector3(worldMin, worldMin, 0f);
			maxBounds = new Vector3(worldMax, worldMax, 0f);
		}

		playerController.SetBounds(minBounds, maxBounds);
		boundsReady = true;
	}

	public void SetHookTransform(Transform hook)
	{
		hookTransform = hook;
	}

	public void ClearHookTransform()
	{
		if (hookTransform != null)
			StartCoroutine(SmoothTransitionToPlayer());
	}

	private IEnumerator SmoothTransitionToPlayer()
	{
		isTransitioning = true;
		float duration = 0.25f;
		float elapsed = 0f;
		Vector3 startPos = transform.position;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float clampedX = Mathf.Clamp(player.position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
			float clampedY = Mathf.Clamp(player.position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
			transform.position = Vector3.Lerp(startPos, new Vector3(clampedX, clampedY, transform.position.z), t);
			yield return null;
		}

		hookTransform = null;
		isTransitioning = false;
	}
}
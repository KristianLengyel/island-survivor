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

	private void Start()
	{
		Camera camera = GetComponent<Camera>();
		halfHeight = camera.orthographicSize;
		halfWidth = halfHeight * camera.aspect;

		UpdateTilemapBounds();
	}

	private void LateUpdate()
	{
		if (isTransitioning)
		{
			return;
		}

		Vector3 desiredPosition;

		if (hookTransform != null)
		{
			Vector3 hookPosition = hookTransform.position;
			desiredPosition = Vector3.Lerp(player.position, hookPosition, 0.5f);
		}
		else
		{
			desiredPosition = player.position;
		}

		float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
		float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

		transform.position = new Vector3(clampedX, clampedY, transform.position.z);
	}

	public void UpdateTilemapBounds()
	{
		tilemap.CompressBounds();

		BoundsInt cellBounds = tilemap.cellBounds;

		cellBounds.xMin += 1;
		cellBounds.yMin += 1;
		cellBounds.xMax -= 1;
		cellBounds.yMax -= 1;

		Vector3Int minCell = cellBounds.min;
		Vector3Int maxCell = cellBounds.max;

		minBounds = tilemap.CellToWorld(minCell);
		maxBounds = tilemap.CellToWorld(new Vector3Int(maxCell.x, maxCell.y, minCell.z));

		playerController.SetBounds(minBounds, maxBounds);
	}

	public void SetHookTransform(Transform hook)
	{
		hookTransform = hook;
	}

	public void ClearHookTransform()
	{
		if (hookTransform != null)
		{
			StartCoroutine(SmoothTransitionToPlayer());
		}
	}

	private IEnumerator SmoothTransitionToPlayer()
	{
		isTransitioning = true;
		float transitionDuration = 0.25f;
		float elapsedTime = 0f;
		Vector3 initialPosition = transform.position;

		while (elapsedTime < transitionDuration)
		{
			elapsedTime += Time.deltaTime;
			float t = Mathf.Clamp01(elapsedTime / transitionDuration);

			Vector3 targetPosition = player.position;
			float clampedX = Mathf.Clamp(targetPosition.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
			float clampedY = Mathf.Clamp(targetPosition.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

			transform.position = Vector3.Lerp(initialPosition, new Vector3(clampedX, clampedY, transform.position.z), t);

			yield return null;
		}

		hookTransform = null;
		isTransitioning = false;
	}
}

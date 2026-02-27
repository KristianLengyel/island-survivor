using UnityEngine;

public class PlayerMovementMotor : MonoBehaviour
{
	private Rigidbody2D rb;
	private PlayerTileDetector tileDetector;
	private CameraMovement cameraMovement;

	private float landSpeed;
	private float waterSpeed;
	private float smoothing;

	private Vector2 smoothVelocity;

	private Vector3 minBounds;
	private Vector3 maxBounds;

	private Transform hookTransform;
	private float maxRange = 7f;
	private MonoBehaviour activeTool;

	public float LastStepSoundTime { get; set; } = -1f;

	public void Initialize(
		Rigidbody2D rb,
		PlayerTileDetector tileDetector,
		float landSpeed,
		float waterSpeed,
		float smoothing,
		CameraMovement cameraMovement
	)
	{
		this.rb = rb;
		this.tileDetector = tileDetector;
		this.landSpeed = landSpeed;
		this.waterSpeed = waterSpeed;
		this.smoothing = smoothing;
		this.cameraMovement = cameraMovement;
	}

	public void FixedTick(Vector2 movementInput)
	{
		if (!rb) return;

		float currentSpeed = tileDetector != null && tileDetector.IsInWater() ? waterSpeed : landSpeed;
		Vector2 targetVelocity = movementInput * currentSpeed;

		rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref smoothVelocity, smoothing);

		Vector2 clampedPosition = rb.position;
		clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBounds.x, maxBounds.x);
		clampedPosition.y = Mathf.Clamp(clampedPosition.y, minBounds.y, maxBounds.y);

		if (hookTransform != null)
		{
			Vector2 hookPos = hookTransform.position;
			Vector2 diff = clampedPosition - hookPos;
			float distSq = diff.sqrMagnitude;
			float maxRangeSq = maxRange * maxRange;

			if (distSq > maxRangeSq)
			{
				Vector2 dir = diff.normalized;
				clampedPosition = hookPos + dir * maxRange;
			}
		}

		rb.position = clampedPosition;
	}

	public void SetBounds(Vector3 minBounds, Vector3 maxBounds)
	{
		this.minBounds = minBounds;
		this.maxBounds = maxBounds;
	}

	public void SetHookTransform(Transform hook, float toolMaxRange, MonoBehaviour tool)
	{
		hookTransform = hook;
		maxRange = toolMaxRange;
		activeTool = tool;
		if (cameraMovement) cameraMovement.SetHookTransform(hook);
	}

	public void ClearHookTransform(MonoBehaviour tool)
	{
		if (activeTool == tool)
		{
			hookTransform = null;
			activeTool = null;
			if (cameraMovement) cameraMovement.ClearHookTransform();
		}
	}
}

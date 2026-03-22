using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class FishController : MonoBehaviour
{
	public float maxSpeed = 2f;
	public float fleeSpeed = 4f;
	public float acceleration = 2f;
	public float waypointRadius = 1f;
	public float roamRadius = 5f;
	public float fleeRadius = 3f;
	public float idleMinDuration = 0.5f;
	public float idleMaxDuration = 2f;

	public int maxTileCheckAttempts = 10;

	private Tilemap waterTilemap;
	private TileBase waterTile;
	private Transform player;

	private Rigidbody2D rb;
	private Vector2 currentWaypoint;
	private bool hasWaypoint;
	private bool facingRight = false;
	private bool initialized;
	private bool isIdle;
	private float idleTimer;
	private float activeSpeed;

	public void Init(Tilemap map, TileBase tile, Transform playerTransform)
	{
		waterTilemap = map;
		waterTile = tile;
		player = playerTransform;
		activeSpeed = maxSpeed;
		initialized = true;
	}

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.rotation = 0f;
	}

	void Update()
	{
		if (!initialized) return;

		bool playerNear = player != null && Vector2.Distance(transform.position, player.position) < fleeRadius;

		if (playerNear)
		{
			isIdle = false;
			activeSpeed = fleeSpeed;
			float fleeDist = hasWaypoint ? Vector2.Distance(transform.position, currentWaypoint) : 0f;
			if (!hasWaypoint || fleeDist < waypointRadius)
				PickFleeWaypoint();
			return;
		}

		activeSpeed = maxSpeed;

		if (isIdle)
		{
			idleTimer -= Time.deltaTime;
			if (idleTimer <= 0f)
			{
				isIdle = false;
				PickNewWaypointInWater();
			}
			return;
		}

		float dist = hasWaypoint ? Vector2.Distance(transform.position, currentWaypoint) : 0f;
		if (!hasWaypoint || dist < waypointRadius)
		{
			hasWaypoint = false;
			isIdle = true;
			idleTimer = Random.Range(idleMinDuration, idleMaxDuration);
		}
	}

	void FixedUpdate()
	{
		if (!initialized) return;

		if (isIdle || !hasWaypoint)
		{
			rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
			return;
		}

		Vector2 dir = (currentWaypoint - (Vector2)transform.position).normalized;
		Vector2 desiredVel = dir * activeSpeed;
		Vector2 steering = desiredVel - rb.linearVelocity;
		float maxSteer = acceleration * Time.fixedDeltaTime;
		if (steering.magnitude > maxSteer)
			steering = steering.normalized * maxSteer;

		rb.linearVelocity += steering;

		if (Mathf.Abs(rb.linearVelocity.x) > 0.01f)
		{
			if (rb.linearVelocity.x < 0f && facingRight) Flip();
			else if (rb.linearVelocity.x > 0f && !facingRight) Flip();
		}
	}

	void PickNewWaypointInWater()
	{
		Vector2 fishPos = transform.position;

		for (int i = 0; i < maxTileCheckAttempts; i++)
		{
			Vector2 offset = Random.insideUnitCircle * roamRadius;
			Vector2 possiblePos = fishPos + offset;

			Vector3Int cellPos = waterTilemap.WorldToCell(possiblePos);
			TileBase tile = waterTilemap.GetTile(cellPos);

			if (tile != null && tile == waterTile)
			{
				currentWaypoint = possiblePos;
				hasWaypoint = true;
				return;
			}
		}

		hasWaypoint = false;
	}

	void PickFleeWaypoint()
	{
		if (player == null) return;

		Vector2 fishPos = transform.position;
		Vector2 awayDir = ((Vector2)transform.position - (Vector2)player.position).normalized;

		for (int i = 0; i < maxTileCheckAttempts; i++)
		{
			float angle = Random.Range(-60f, 60f);
			Vector2 jitteredDir = Quaternion.Euler(0f, 0f, angle) * awayDir;
			Vector2 possiblePos = fishPos + jitteredDir * roamRadius;

			Vector3Int cellPos = waterTilemap.WorldToCell(possiblePos);
			TileBase tile = waterTilemap.GetTile(cellPos);

			if (tile != null && tile == waterTile)
			{
				currentWaypoint = possiblePos;
				hasWaypoint = true;
				return;
			}
		}

		PickNewWaypointInWater();
	}

	void Flip()
	{
		facingRight = !facingRight;
		Vector3 localScale = transform.localScale;
		localScale.x *= -1f;
		transform.localScale = localScale;
	}
}

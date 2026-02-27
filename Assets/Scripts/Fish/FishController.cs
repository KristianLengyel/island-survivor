using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class FishController : MonoBehaviour
{
	public float maxSpeed = 2f;
	public float acceleration = 2f;
	public float waypointRadius = 1f;
	public float roamRadius = 5f;

	private Tilemap waterTilemap;
	private TileBase waterTile;

	public int maxTileCheckAttempts = 10;

	Rigidbody2D rb;
	Vector2 currentWaypoint;
	bool hasWaypoint;
	bool facingRight = true;

	public void Init(Tilemap map, TileBase tile)
	{
		waterTilemap = map;
		waterTile = tile;
	}

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		rb.rotation = 0f;
	}

	void Update()
	{
		float dist = Vector2.Distance(transform.position, currentWaypoint);
		if (!hasWaypoint || dist < waypointRadius)
		{
			PickNewWaypointInWater();
		}
	}

	void FixedUpdate()
	{
		Vector2 dir = (currentWaypoint - (Vector2)transform.position).normalized;
		Vector2 desiredVel = dir * maxSpeed;
		Vector2 steering = desiredVel - rb.linearVelocity;
		float maxSteer = acceleration * Time.fixedDeltaTime;
		if (steering.magnitude > maxSteer)
			steering = steering.normalized * maxSteer;

		rb.linearVelocity += steering;

		if (Mathf.Abs(rb.linearVelocity.x) > 0.01f)
		{
			if (rb.linearVelocity.x < 0f && !facingRight)
			{
				Flip();
			}
			else if (rb.linearVelocity.x > 0f && facingRight)
			{
				Flip();
			}
		}
	}

	void PickNewWaypointInWater()
	{
		bool found = false;
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
				found = true;
				break;
			}
		}

		if (!found)
		{
			hasWaypoint = false;
		}
	}

	void Flip()
	{
		facingRight = !facingRight;
		Vector3 localScale = transform.localScale;
		localScale.x *= -1f;
		transform.localScale = localScale;
	}
}

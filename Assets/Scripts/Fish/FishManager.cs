using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FishManager : MonoBehaviour
{
	[Header("Player & Map References")]
	public Transform player;
	public Tilemap waterTilemap;
	public TileBase waterTile;
	public MapGeneratorV3 mapGenerator;

	[Header("Fish Settings")]
	public GameObject fishPrefab;
	public int fishCount = 10;
	public float spawnRadius = 10f;
	public float maxDistanceFromPlayer = 15f;
	public int maxSpawnAttempts = 100;

	[Header("Hierarchy Management")]
	public Transform fishParent;

	[Header("Gizmos")]
	public bool showGizmos = true;

	private List<GameObject> spawnedFish = new List<GameObject>();
	private bool ready;

	IEnumerator Start()
	{
		if (mapGenerator == null)
			mapGenerator = FindAnyObjectByType<MapGeneratorV3>();

		if (mapGenerator != null)
		{
			while (mapGenerator.IsGenerating)
				yield return null;
		}

		Vector3Int playerCell = waterTilemap.WorldToCell(player.position);
		while (!waterTilemap.HasTile(playerCell))
		{
			yield return null;
			playerCell = waterTilemap.WorldToCell(player.position);
		}

		if (fishParent == null)
		{
			GameObject parentObj = new GameObject("FishParent");
			fishParent = parentObj.transform;
		}

		SpawnFishAroundPlayer();
		ready = true;
	}

	void Update()
	{
		if (!ready) return;
		if (player == null) return;

		for (int i = 0; i < spawnedFish.Count; i++)
		{
			if (!spawnedFish[i])
			{
				spawnedFish.RemoveAt(i);
				i--;
				SpawnOneFishNearPlayer();
				continue;
			}

			float dist = Vector2.Distance(player.position, spawnedFish[i].transform.position);
			if (dist > maxDistanceFromPlayer)
			{
				Destroy(spawnedFish[i]);
				spawnedFish.RemoveAt(i);
				i--;
				SpawnOneFishNearPlayer();
			}
		}
	}

	void SpawnFishAroundPlayer()
	{
		int spawned = 0;
		int attempts = 0;

		while (spawned < fishCount && attempts < maxSpawnAttempts)
		{
			attempts++;
			Vector3Int cell = GetRandomCellNearPlayer();
			if (IsWaterTile(cell))
			{
				Vector3 worldPos = waterTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
				GameObject fishGO = Instantiate(fishPrefab, worldPos, Quaternion.identity, fishParent);

				FishController fishCtrl = fishGO.GetComponent<FishController>();
				fishCtrl.Init(waterTilemap, waterTile, player);

				spawnedFish.Add(fishGO);
				spawned++;
			}
		}
	}

	void SpawnOneFishNearPlayer()
	{
		int attempts = 0;
		while (attempts < maxSpawnAttempts)
		{
			attempts++;
			Vector3Int cell = GetRandomCellNearPlayer();
			if (IsWaterTile(cell))
			{
				Vector3 worldPos = waterTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);
				GameObject fishGO = Instantiate(fishPrefab, worldPos, Quaternion.identity, fishParent);

				FishController fishCtrl = fishGO.GetComponent<FishController>();
				fishCtrl.Init(waterTilemap, waterTile, player);

				spawnedFish.Add(fishGO);
				break;
			}
		}
	}

	Vector3Int GetRandomCellNearPlayer()
	{
		Vector3Int playerCell = waterTilemap.WorldToCell(player.position);
		int offsetX = Random.Range(-Mathf.RoundToInt(spawnRadius), Mathf.RoundToInt(spawnRadius) + 1);
		int offsetY = Random.Range(-Mathf.RoundToInt(spawnRadius), Mathf.RoundToInt(spawnRadius) + 1);
		Vector3Int result = new Vector3Int(playerCell.x + offsetX, playerCell.y + offsetY, 0);

		BoundsInt bounds = waterTilemap.cellBounds;
		if (result.x < bounds.xMin) result.x = bounds.xMin;
		if (result.x >= bounds.xMax) result.x = bounds.xMax - 1;
		if (result.y < bounds.yMin) result.y = bounds.yMin;
		if (result.y >= bounds.yMax) result.y = bounds.yMax - 1;

		return result;
	}

	bool IsWaterTile(Vector3Int cellPos)
	{
		TileBase tile = waterTilemap.GetTile(cellPos);
		return (tile != null && tile == waterTile);
	}

	void OnDrawGizmos()
	{
		if (!showGizmos)
			return;

		if (player != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(player.position, spawnRadius);

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(player.position, maxDistanceFromPlayer);
		}
	}
}

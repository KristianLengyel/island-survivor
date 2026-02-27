using UnityEngine;
using UnityEngine.Tilemaps;

public class HammerTool : MonoBehaviour, IPlayerTool
{
	[Header("Tilemaps")]
	public Tilemap buildingTilemap;
	public Tilemap tempTilemap;
	public Tilemap progressTilemap;
	public Tilemap waterTilemap;
	public Tilemap betweenOceanFloorWaterTilemap;
	public Tilemap objectsTilemap;

	[Header("Tiles")]
	public TileBase waterTile;
	public TileBase indicatorTile;

	[Header("Hold Progress Tiles")]
	[SerializeField] private TileBase[] holdProgressTiles;

	[Header("Walkable Marker Tile")]
	[SerializeField] private TileBase walkableMarkerTile;

	[Header("Items")]
	public Item hammerItem;

	[Header("Settings")]
	private const float OverlapRadius = 0.5f;
	private const int PlacementRange = 2;
	[SerializeField] private float holdDuration = 0.5f;

	private bool canPlaceTile = true;
	private bool canRemoveTile = true;

	private float rightClickHoldTime = 0f;
	private Vector3Int lastGridPos;
	private bool isHoldingRightClick = false;

	private Camera mainCamera;

	private Vector3Int? lastTempIndicatorCell;
	private Vector3Int? lastProgressCell;
	private int lastProgressSpriteIndex = -1;

	private readonly Collider2D[] overlapBuffer = new Collider2D[16];
	private ContactFilter2D overlapFilter;

	private bool isActive;

	private void Start()
	{
		mainCamera = Camera.main;
		SetupOverlapFilter();
		enabled = false;
	}

	public bool CanHandle(Item selectedItem)
	{
		return selectedItem != null && selectedItem == hammerItem;
	}

	public void OnSelected(Item selectedItem)
	{
		isActive = true;
		canPlaceTile = true;
		canRemoveTile = true;
		isHoldingRightClick = false;
		rightClickHoldTime = 0f;
		lastProgressSpriteIndex = -1;
		ClearAllIndicators();
		enabled = true;
	}

	public void OnDeselected()
	{
		isActive = false;
		enabled = false;
		isHoldingRightClick = false;
		rightClickHoldTime = 0f;
		ClearAllIndicators();
	}

	public void Tick()
	{
		if (!isActive) return;

		if (GameManager.Instance == null)
		{
			ClearAllIndicators();
			return;
		}

		if (!mainCamera)
		{
			mainCamera = Camera.main;
			if (!mainCamera)
			{
				ClearAllIndicators();
				return;
			}
		}

		Vector3 mousePos = GameInput.MouseScreen;
		if (mousePos.x < 0 || mousePos.y < 0 || mousePos.x > Screen.width || mousePos.y > Screen.height)
		{
			ClearAllIndicators();
			return;
		}

		var inventoryManager = GameManager.Instance.InventoryManager;
		var buildingManager = GameManager.Instance.BuildingManager;
		var buildingMenuManager = GameManager.Instance.BuildingMenuManager;

		if (inventoryManager.IsInventoryOpen() || buildingMenuManager.IsBuildingMenuOpen)
		{
			ClearAllIndicators();
			return;
		}

		Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);
		Vector3Int gridPos = buildingTilemap.WorldToCell(mouseWorldPos);

		if (!IsWithinRange(gridPos))
		{
			ClearAllIndicators();
			return;
		}

		ShowIndicator(gridPos);

		if (GameInput.LmbDown && canPlaceTile)
		{
			TileBase tileToPlace = buildingManager.GetSelectedTile();
			if (tileToPlace != null && buildingManager.HasEnoughResources(tileToPlace))
			{
				if (buildingTilemap.GetTile(gridPos) == null && !IsPlaceableObjectAt(gridPos))
				{
					PlaceTile(gridPos, tileToPlace);
					buildingManager.UseResources(tileToPlace);
					canPlaceTile = false;
				}
			}
		}
		else if (GameInput.LmbUp)
		{
			canPlaceTile = true;
		}

		if (GameInput.RmbDown && canRemoveTile)
		{
			if (TryRemovePlaceableObject(gridPos))
			{
				canRemoveTile = false;
			}
			else
			{
				isHoldingRightClick = true;
				rightClickHoldTime = 0f;
				lastGridPos = gridPos;
				lastProgressSpriteIndex = -1;
			}
		}
		else if (GameInput.RmbHeld && isHoldingRightClick && gridPos == lastGridPos)
		{
			rightClickHoldTime += Time.deltaTime;
			if (rightClickHoldTime >= holdDuration && canRemoveTile)
			{
				RemoveTile(gridPos);
				canRemoveTile = false;
				isHoldingRightClick = false;
				ClearAllIndicators();
			}
		}
		else if (GameInput.RmbUp)
		{
			isHoldingRightClick = false;
			rightClickHoldTime = 0f;
			canRemoveTile = true;
			ClearProgressIndicator();
		}

		if (isHoldingRightClick && gridPos != lastGridPos)
		{
			isHoldingRightClick = false;
			rightClickHoldTime = 0f;
			lastGridPos = gridPos;
			lastProgressSpriteIndex = -1;
			ClearProgressIndicator();
		}
	}

	public void FixedTick() { }

	private void SetupOverlapFilter()
	{
		overlapFilter = default;
		overlapFilter.useTriggers = true;
		overlapFilter.useLayerMask = false;
		overlapFilter.useDepth = false;
	}

	private void PlaceTile(Vector3Int gridPos, TileBase tileToPlace)
	{
		buildingTilemap.SetTile(gridPos, tileToPlace);
		GameManager.Instance.AudioManager.PlaySound("PlaceSound");

		Vector3Int below = new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z);
		if (waterTilemap.GetTile(below) == waterTile)
		{
			TileBase pillarTile = GameManager.Instance.BuildingManager.GetPillarTileForFloor(tileToPlace);
			if (pillarTile != null)
			{
				betweenOceanFloorWaterTilemap.SetTile(below, pillarTile);
			}
		}
	}

	private bool TryRemovePlaceableObject(Vector3Int gridPos)
	{
		var inventoryManager = GameManager.Instance.InventoryManager;
		Vector3 cellCenter = buildingTilemap.GetCellCenterWorld(gridPos);

		int hitCount = Physics2D.OverlapCircle(cellCenter, OverlapRadius, overlapFilter, overlapBuffer);
		for (int i = 0; i < hitCount; i++)
		{
			var col = overlapBuffer[i];
			if (!col) continue;

			if (!col.CompareTag("PlaceableObject")) continue;

			GameObject placedObj = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;

			var chest = placedObj.GetComponent<Chest>() ?? placedObj.GetComponentInParent<Chest>();
			if (chest != null && chest.HasAnyItems())
			{
				return true;
			}

			ItemHolder itemHolder = placedObj.GetComponent<ItemHolder>();
			if (itemHolder != null)
			{
				inventoryManager.AddItem(itemHolder.item, 1);
				Destroy(placedObj);

				if (itemHolder.item.type == ItemType.PlaceableObjectWalkableOver)
				{
					objectsTilemap.SetTile(gridPos, null);
				}

				GameManager.Instance.AudioManager.PlaySound("PlaceSound");
				return true;
			}
		}

		return false;
	}

	private void RemoveTile(Vector3Int gridPos)
	{
		var inventoryManager = GameManager.Instance.InventoryManager;
		var buildingManager = GameManager.Instance.BuildingManager;

		TileBase tile = buildingTilemap.GetTile(gridPos);
		if (tile != null)
		{
			buildingTilemap.SetTile(gridPos, null);
			GameManager.Instance.AudioManager.PlaySound("PlaceSound");

			Vector3Int below = new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z);
			TileBase currentPillar = betweenOceanFloorWaterTilemap.GetTile(below);
			TileBase expectedPillar = buildingManager.GetPillarTileForFloor(tile);
			if (currentPillar != null && currentPillar == expectedPillar)
			{
				betweenOceanFloorWaterTilemap.SetTile(below, null);
			}

			var returnedResources = buildingManager.GetResourcesReturnedOnDestroy(tile);
			if (returnedResources != null)
			{
				inventoryManager.AddResources(returnedResources);
			}
		}
	}

	private bool IsPlaceableObjectAt(Vector3Int gridPos)
	{
		Vector3 cellCenter = buildingTilemap.GetCellCenterWorld(gridPos);

		int hitCount = Physics2D.OverlapCircle(cellCenter, OverlapRadius, overlapFilter, overlapBuffer);
		for (int i = 0; i < hitCount; i++)
		{
			var c = overlapBuffer[i];
			if (!c) continue;
			if (c.CompareTag("PlaceableObject")) return true;
		}

		return false;
	}

	private bool CanRemoveAtPosition(Vector3Int gridPos)
	{
		return buildingTilemap.GetTile(gridPos) != null || IsPlaceableObjectAt(gridPos);
	}

	private void ShowIndicator(Vector3Int gridPos)
	{
		SetTempIndicator(gridPos);

		bool shouldShowProgress =
			isHoldingRightClick &&
			rightClickHoldTime > 0f &&
			rightClickHoldTime < holdDuration &&
			gridPos == lastGridPos &&
			CanRemoveAtPosition(gridPos);

		if (shouldShowProgress && holdProgressTiles != null && holdProgressTiles.Length > 0)
		{
			float progress = Mathf.Clamp01(rightClickHoldTime / holdDuration);
			int tileIndex = Mathf.FloorToInt(progress * (holdProgressTiles.Length - 1));
			tileIndex = Mathf.Clamp(tileIndex, 0, holdProgressTiles.Length - 1);

			if (lastProgressCell != gridPos)
			{
				ClearProgressIndicator();
				lastProgressCell = gridPos;
				lastProgressSpriteIndex = -1;
			}

			if (tileIndex != lastProgressSpriteIndex)
			{
				progressTilemap.SetTile(gridPos, holdProgressTiles[tileIndex]);
				lastProgressSpriteIndex = tileIndex;
			}
		}
		else
		{
			ClearProgressIndicator();
		}
	}

	private void SetTempIndicator(Vector3Int cell)
	{
		if (lastTempIndicatorCell.HasValue && lastTempIndicatorCell.Value != cell)
		{
			tempTilemap.SetTile(lastTempIndicatorCell.Value, null);
		}

		if (!lastTempIndicatorCell.HasValue || lastTempIndicatorCell.Value != cell)
		{
			tempTilemap.SetTile(cell, indicatorTile);
			lastTempIndicatorCell = cell;
		}
	}

	private void ClearTempIndicator()
	{
		if (lastTempIndicatorCell.HasValue)
		{
			tempTilemap.SetTile(lastTempIndicatorCell.Value, null);
			lastTempIndicatorCell = null;
		}
	}

	private void ClearProgressIndicator()
	{
		if (lastProgressCell.HasValue)
		{
			progressTilemap.SetTile(lastProgressCell.Value, null);
			lastProgressCell = null;
			lastProgressSpriteIndex = -1;
		}
	}

	private void ClearAllIndicators()
	{
		ClearTempIndicator();
		ClearProgressIndicator();
	}

	private bool IsWithinRange(Vector3Int gridPos)
	{
		Vector3Int playerPos = buildingTilemap.WorldToCell(transform.position);
		return Mathf.Abs(gridPos.x - playerPos.x) <= PlacementRange && Mathf.Abs(gridPos.y - playerPos.y) <= PlacementRange;
	}
}

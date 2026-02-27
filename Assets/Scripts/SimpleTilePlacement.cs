using UnityEngine;
using UnityEngine.Tilemaps;

public class SimpleTilePlacement : MonoBehaviour
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

	[Header("Hold Indicator Tiles (pre-baked assets)")]
	[SerializeField] private TileBase[] holdProgressTiles;

	[Header("Walkable Marker Tile (pre-baked asset)")]
	[SerializeField] private TileBase walkableMarkerTile;

	[Header("Items")]
	public Item hammerItem;
	public Item catchingNetItem;

	[Header("Settings")]
	private const float OverlapRadius = 0.5f;
	private const int PlacementRange = 2;
	[SerializeField] private float holdDuration = 0.5f;
	[SerializeField] private GameObject placeableContainerPrefab;
	[SerializeField] private float objectOffsetY = 0.25f;

	private bool canPlaceTile = true;
	private bool canRemoveTile = true;
	private Item lastSelectedItem;
	private float rightClickHoldTime = 0f;
	private Vector3Int lastGridPos;
	private bool isHoldingRightClick = false;
	private Transform placeableContainer;

	private Camera mainCamera;

	private Vector3Int? lastTempIndicatorCell;
	private Vector3Int? lastProgressCell;
	private int lastProgressSpriteIndex = -1;

	private readonly Collider2D[] overlapBuffer = new Collider2D[16];
	private ContactFilter2D overlapFilter;

	private void Start()
	{
		mainCamera = Camera.main;
		SetupPlaceableContainer();
		SetupOverlapFilter();
	}

	private void SetupOverlapFilter()
	{
		overlapFilter = default;
		overlapFilter.useTriggers = true;
		overlapFilter.useLayerMask = false;
		overlapFilter.useDepth = false;
	}

	private void SetupPlaceableContainer()
	{
		GameObject existingContainer = GameObject.Find("PlaceableObjectsContainer");
		if (existingContainer != null)
		{
			placeableContainer = existingContainer.transform;
		}
		else
		{
			if (placeableContainerPrefab != null)
			{
				placeableContainer = Instantiate(placeableContainerPrefab, Vector3.zero, Quaternion.identity).transform;
				placeableContainer.name = "PlaceableObjectsContainer";
			}
			else
			{
				placeableContainer = new GameObject("PlaceableObjectsContainer").transform;
			}
		}
	}

	private void Update()
	{
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

		Item selectedItem = inventoryManager.GetSelectedItem();
		if (selectedItem != lastSelectedItem)
		{
			ClearAllIndicators();
			lastSelectedItem = selectedItem;
		}

		if (selectedItem == null)
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

		if (selectedItem.type == ItemType.PlaceableObject || selectedItem.type == ItemType.PlaceableObjectWalkableOver)
		{
			HandlePlaceableObject(selectedItem, gridPos);
		}
		else if (selectedItem == hammerItem)
		{
			HandleHammerItem(buildingManager, gridPos);
		}
		else
		{
			ClearAllIndicators();
		}
	}

	private void HandlePlaceableObject(Item selectedItem, Vector3Int gridPos)
	{
		ShowIndicator(gridPos);

		if (GameInput.LmbDown && canPlaceTile)
		{
			if (CanPlaceAtPosition(selectedItem, gridPos) &&
				!IsPlaceableObjectAt(gridPos) &&
				(selectedItem != catchingNetItem || IsNextToFloorTile(gridPos)))
			{
				PlacePrefab(gridPos, selectedItem.prefab, selectedItem.type == ItemType.PlaceableObjectWalkableOver);
				canPlaceTile = false;
				GameManager.Instance.InventoryManager.RemoveItem(selectedItem.name, 1);
			}
		}
		else if (GameInput.LmbUp)
		{
			canPlaceTile = true;
		}
	}

	private void HandleHammerItem(BuildingManager buildingManager, Vector3Int gridPos)
	{
		ShowIndicator(gridPos);

		if (Input.GetMouseButtonDown(0) && canPlaceTile)
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
		else if (Input.GetMouseButtonUp(0))
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

	private void PlacePrefab(Vector3Int gridPos, GameObject prefab, bool walkable)
	{
		Vector3 cellCenter = buildingTilemap.GetCellCenterWorld(gridPos);
		Vector3 offsetPosition = cellCenter + new Vector3(0f, buildingTilemap.cellSize.y * objectOffsetY, 0f);

		GameObject placedObject = Instantiate(prefab, walkable ? cellCenter : offsetPosition, Quaternion.identity, placeableContainer);
		placedObject.tag = "PlaceableObject";

		GameManager.Instance.AudioManager.PlaySound("PlaceSound");

		if (walkable && walkableMarkerTile != null)
		{
			objectsTilemap.SetTile(gridPos, walkableMarkerTile);
		}

		ClearAllIndicators();
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

			if (col.CompareTag("PlaceableObject"))
			{
				GameObject placedObj = col.gameObject;
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

	private bool CanPlaceAtPosition(Item item, Vector3Int gridPos)
	{
		TileBase buildingTile = buildingTilemap.GetTile(gridPos);
		TileBase waterTileAtPos = waterTilemap.GetTile(gridPos);

		switch (item.placementCondition)
		{
			case PlacementCondition.Anywhere:
				return true;

			case PlacementCondition.WaterOnly:
				return buildingTile == null && waterTileAtPos == waterTile;

			case PlacementCondition.FloorOnly:
				return buildingTile != null || waterTileAtPos == null;

			default:
				return false;
		}
	}

	private bool IsNextToFloorTile(Vector3Int gridPos)
	{
		Vector3Int neighborPos;

		neighborPos = new Vector3Int(gridPos.x, gridPos.y + 1, gridPos.z);
		if (buildingTilemap.GetTile(neighborPos) != null) return true;

		neighborPos = new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z);
		if (buildingTilemap.GetTile(neighborPos) != null) return true;

		neighborPos = new Vector3Int(gridPos.x - 1, gridPos.y, gridPos.z);
		if (buildingTilemap.GetTile(neighborPos) != null) return true;

		neighborPos = new Vector3Int(gridPos.x + 1, gridPos.y, gridPos.z);
		if (buildingTilemap.GetTile(neighborPos) != null) return true;

		return false;
	}

	private bool CanRemoveAtPosition(Vector3Int gridPos)
	{
		return buildingTilemap.GetTile(gridPos) != null || IsPlaceableObjectAt(gridPos);
	}

	private void ShowIndicator(Vector3Int gridPos)
	{
		Item selectedItem = GameManager.Instance.InventoryManager.GetSelectedItem();

		bool shouldShowIndicator =
			selectedItem != null &&
			CanPlaceAtPosition(selectedItem, gridPos) &&
			(selectedItem != catchingNetItem || IsNextToFloorTile(gridPos));

		if (shouldShowIndicator)
		{
			SetTempIndicator(gridPos);
		}
		else
		{
			ClearTempIndicator();
		}

		bool shouldShowProgress =
			isHoldingRightClick &&
			rightClickHoldTime > 0f &&
			rightClickHoldTime < holdDuration &&
			lastSelectedItem == hammerItem &&
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

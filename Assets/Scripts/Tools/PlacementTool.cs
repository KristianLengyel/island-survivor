using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementTool : MonoBehaviour, IPlayerTool
{
	[Header("Tilemaps")]
	public Tilemap buildingTilemap;
	public Tilemap tempTilemap;
	public Tilemap waterTilemap;
	public Tilemap objectsTilemap;

	[Header("Tiles")]
	public TileBase waterTile;
	public TileBase indicatorTile;

	[Header("Walkable Marker Tile")]
	[SerializeField] private TileBase walkableMarkerTile;

	[Header("Items")]
	public Item catchingNetItem;

	[Header("Settings")]
	private const float OverlapRadius = 0.5f;
	private const int PlacementRange = 2;
	[SerializeField] private GameObject placeableContainerPrefab;
	[SerializeField] private float objectOffsetY = 0.25f;

	private bool canPlace = true;
	private Item selectedItem;
	private Transform placeableContainer;
	private Camera mainCamera;

	private Vector3Int? lastTempIndicatorCell;

	private readonly Collider2D[] overlapBuffer = new Collider2D[16];
	private ContactFilter2D overlapFilter;

	private void Start()
	{
		mainCamera = Camera.main;
		SetupPlaceableContainer();
		SetupOverlapFilter();
		enabled = false;
	}

	public bool CanHandle(Item selectedItem)
	{
		if (selectedItem == null) return false;
		return selectedItem.type == ItemType.PlaceableObject || selectedItem.type == ItemType.PlaceableObjectWalkableOver;
	}

	public void OnSelected(Item selectedItem)
	{
		this.selectedItem = selectedItem;
		ClearAllIndicators();
		canPlace = true;
		enabled = true;
	}

	public void OnDeselected()
	{
		enabled = false;
		this.selectedItem = null;
		ClearAllIndicators();
	}

	public void Tick()
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
		var buildingMenuManager = GameManager.Instance.BuildingMenuManager;

		if (inventoryManager.IsInventoryOpen() || buildingMenuManager.IsBuildingMenuOpen)
		{
			ClearAllIndicators();
			return;
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

		ShowIndicator(gridPos);

		if (GameInput.LmbDown && canPlace)
		{
			if (CanPlaceAtPosition(selectedItem, gridPos) &&
				!IsPlaceableObjectAt(gridPos) &&
				(selectedItem != catchingNetItem || IsNextToFloorTile(gridPos)))
			{
				PlacePrefab(gridPos, selectedItem.prefab, selectedItem.type == ItemType.PlaceableObjectWalkableOver);
				canPlace = false;
				GameManager.Instance.InventoryManager.RemoveItem(selectedItem.name, 1);
			}
		}
		else if (GameInput.LmbUp)
		{
			canPlace = true;
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
		Vector3Int n;

		n = new Vector3Int(gridPos.x, gridPos.y + 1, gridPos.z);
		if (buildingTilemap.GetTile(n) != null) return true;

		n = new Vector3Int(gridPos.x, gridPos.y - 1, gridPos.z);
		if (buildingTilemap.GetTile(n) != null) return true;

		n = new Vector3Int(gridPos.x - 1, gridPos.y, gridPos.z);
		if (buildingTilemap.GetTile(n) != null) return true;

		n = new Vector3Int(gridPos.x + 1, gridPos.y, gridPos.z);
		if (buildingTilemap.GetTile(n) != null) return true;

		return false;
	}

	private void ShowIndicator(Vector3Int gridPos)
	{
		bool shouldShow =
			selectedItem != null &&
			CanPlaceAtPosition(selectedItem, gridPos) &&
			(selectedItem != catchingNetItem || IsNextToFloorTile(gridPos));

		if (shouldShow)
		{
			SetTempIndicator(gridPos);
		}
		else
		{
			ClearTempIndicator();
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

	private void ClearAllIndicators()
	{
		ClearTempIndicator();
	}

	private bool IsWithinRange(Vector3Int gridPos)
	{
		Vector3Int playerPos = buildingTilemap.WorldToCell(transform.position);
		return Mathf.Abs(gridPos.x - playerPos.x) <= PlacementRange && Mathf.Abs(gridPos.y - playerPos.y) <= PlacementRange;
	}
}

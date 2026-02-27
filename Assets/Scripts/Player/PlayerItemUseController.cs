using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerItemUseController : MonoBehaviour
{
	private Rigidbody2D rb;
	private Camera mainCamera;
	private PlayerTileDetector tileDetector;
	private PlayerStats playerStats;

	private float maxFillRange;
	private Tilemap fillIndicatorTilemap;
	private TileBase fillIndicatorTile;
	private GameObject fillTextPrefab;
	private Canvas uiCanvas;
	private float textOffsetY;

	private Vector3Int lastIndicatorPos = Vector3Int.zero;
	private bool indicatorActive;
	private GameObject fillTextInstance;
	private TextMeshProUGUI fillTextTMP;

	public void Initialize(
		Rigidbody2D rb,
		Camera mainCamera,
		PlayerTileDetector tileDetector,
		PlayerStats playerStats,
		float maxFillRange,
		Tilemap fillIndicatorTilemap,
		TileBase fillIndicatorTile,
		GameObject fillTextPrefab,
		Canvas uiCanvas,
		float textOffsetY
	)
	{
		this.rb = rb;
		this.mainCamera = mainCamera;
		this.tileDetector = tileDetector;
		this.playerStats = playerStats;
		this.maxFillRange = maxFillRange;
		this.fillIndicatorTilemap = fillIndicatorTilemap;
		this.fillIndicatorTile = fillIndicatorTile;
		this.fillTextPrefab = fillTextPrefab;
		this.uiCanvas = uiCanvas;
		this.textOffsetY = textOffsetY;
	}

	public void Tick()
	{
		var inventoryManager = GameManager.Instance.InventoryManager;
		if (!inventoryManager)
		{
			ClearIndicator();
			return;
		}

		Item selectedItem = inventoryManager.GetSelectedItem();
		if (selectedItem == null)
		{
			ClearIndicator();
			return;
		}

		var slot = inventoryManager.inventorySlots[inventoryManager.SelectedSlotIndex];
		var baseSlotItem = slot.GetComponentInChildren<InventoryItem>();
		var slotItem = baseSlotItem as WaterContainerInventoryItem;

		if (GameInput.RmbDown)
		{
			if (selectedItem.type == ItemType.Consumable && selectedItem.actionType == ActionType.Consume)
			{
				if (baseSlotItem != null && playerStats != null && playerStats.ConsumeFood(selectedItem.name))
				{
					inventoryManager.RemoveItem(selectedItem.name, 1);
					GameManager.Instance.AudioManager.PlaySound("EatSound");
				}
			}
			else if (slotItem != null && playerStats != null && slotItem.Drink())
			{
				float thirstChange = slotItem.isSaltWater ? -(playerStats.maxThirst / 3f) : (playerStats.maxThirst / 3f);
				playerStats.AddThirst(thirstChange);
			}
		}

		if (slotItem != null && mainCamera != null && fillIndicatorTilemap != null)
		{
			Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(GameInput.MouseScreen);
			Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
			Vector3Int gridPos = fillIndicatorTilemap.WorldToCell(mouseWorldPos);

			Vector2 playerPos = rb ? rb.position : (Vector2)transform.position;
			Vector2 toMouse = (Vector2)mouseWorldPos - playerPos;
			float distSq = toMouse.sqrMagnitude;
			float maxFillRangeSq = maxFillRange * maxFillRange;

			if (distSq <= maxFillRangeSq && tileDetector != null && tileDetector.IsTileWater(mousePos2D))
			{
				if (indicatorActive && gridPos != lastIndicatorPos)
				{
					fillIndicatorTilemap.SetTile(lastIndicatorPos, null);
				}

				if (!indicatorActive || gridPos != lastIndicatorPos)
				{
					fillIndicatorTilemap.SetTile(gridPos, fillIndicatorTile);
					lastIndicatorPos = gridPos;
					indicatorActive = true;
				}

				if (fillTextInstance == null && fillTextPrefab != null && uiCanvas != null)
				{
					fillTextInstance = Instantiate(fillTextPrefab, Vector3.zero, Quaternion.identity, uiCanvas.transform);
					fillTextTMP = fillTextInstance.GetComponent<TextMeshProUGUI>();
					if (fillTextTMP != null) fillTextTMP.text = "Fill";
				}

				if (fillTextInstance != null)
				{
					Vector3 tileWorldPos = fillIndicatorTilemap.CellToWorld(gridPos);
					tileWorldPos.y += textOffsetY;
					Vector3 screenPos = mainCamera.WorldToScreenPoint(tileWorldPos);
					fillTextInstance.transform.position = screenPos;
				}

				if (GameInput.InteractDown && slotItem.currentFill < slotItem.item.maxFillCapacity)
				{
					int fillsToAdd = slotItem.item.maxFillCapacity - slotItem.currentFill;
					slotItem.Fill(fillsToAdd, true);
					AudioManager.instance.PlaySound("WaterFill");
				}
			}
			else
			{
				ClearIndicator();
			}
		}
		else
		{
			ClearIndicator();
		}
	}

	public void ClearIndicator()
	{
		if (indicatorActive && fillIndicatorTilemap != null)
		{
			fillIndicatorTilemap.SetTile(lastIndicatorPos, null);
			indicatorActive = false;
		}

		if (fillTextInstance != null)
		{
			Destroy(fillTextInstance);
			fillTextInstance = null;
			fillTextTMP = null;
		}
	}
}

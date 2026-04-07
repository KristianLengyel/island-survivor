using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WaterPump : MonoBehaviour, IInteractable, ISaveableComponent, IPipeConnectable
{
	[Header("Sprites")]
	[SerializeField] private Sprite activeSprite;
	[SerializeField] private Sprite inactiveSprite;

	[Header("Outline")]
	[SerializeField] private SpriteRenderer outlineRenderer;

	[Header("Settings")]
	[SerializeField] private float pumpInterval = 10f;

	public float interactionRange = 2f;

	private SpriteRenderer spriteRenderer;
	private bool isFloorAdjacent;
	private Vector2Int gridPos;
	private float pumpTimer;
	private bool hasUnit;

	public Vector2Int GridPosition => gridPos;
	public bool IsPump => true;
	public bool IsActive => isFloorAdjacent;
	public WaterType OutputWaterType => WaterType.SaltWater;

	public bool IsColorSource => isFloorAdjacent;
	public bool IsWaterTypeCompatible(WaterType type) => type == WaterType.SaltWater;
	public bool CanReceiveWater(WaterType type) => false;
	public void ReceiveWaterUnit(WaterType type) { }
	public bool CanConsumeWater(WaterType type) => type == WaterType.SaltWater && isFloorAdjacent && hasUnit;
	public void ConsumeWaterUnit() { hasUnit = false; }

	public string SaveKey => "WaterPump";

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		if (PipeNetwork.Instance == null)
		{
			Debug.LogError("[WaterPump] PipeNetwork.Instance is null!");
			return;
		}

		gridPos = PipeNetwork.Instance.WorldToCell(transform.position);
		isFloorAdjacent = CheckFloorAdjacency();
		ApplyVisualState();
		PipeNetwork.Instance.Register(this);
	}

	private void OnDisable()
	{
		if (PipeNetwork.Instance != null)
			PipeNetwork.Instance.Deregister(this);
	}

	private void Update()
	{
		if (!isFloorAdjacent) return;

		pumpTimer += Time.deltaTime;
		if (pumpTimer < pumpInterval) return;
		pumpTimer -= pumpInterval;

		if (!PipeNetwork.Instance.TryPushWaterUnit(gridPos, WaterType.SaltWater, this))
			hasUnit = true;
	}

	private bool CheckFloorAdjacency()
	{
		var buildingTilemap = PipeNetwork.Instance.BuildingTilemap;
		if (buildingTilemap == null) return true;

		var cell = new Vector3Int(gridPos.x, gridPos.y, 0);
		return buildingTilemap.HasTile(cell + Vector3Int.up)
			|| buildingTilemap.HasTile(cell + Vector3Int.down)
			|| buildingTilemap.HasTile(cell + Vector3Int.left)
			|| buildingTilemap.HasTile(cell + Vector3Int.right);
	}

	private void ApplyVisualState()
	{
		if (spriteRenderer == null) return;
		spriteRenderer.sprite = isFloorAdjacent ? activeSprite : inactiveSprite;
	}

	public void Interact() { }

	public void SetHighlighted(bool highlight)
	{
		if (outlineRenderer != null)
			outlineRenderer.enabled = highlight;
	}

	public float GetInteractionRange() => interactionRange;
	public Transform GetTransform() => transform;

	public bool IsMouseOverSprite(Vector2 mouseWorldPos)
	{
		return InteractableUtil.IsMouseOverBounds(spriteRenderer, mouseWorldPos);
	}

	public string CaptureStateJson() => "{}";
	public void RestoreStateJson(string json) { }
}

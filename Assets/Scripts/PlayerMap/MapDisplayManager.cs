using UnityEngine;
using UnityEngine.UIElements;

public class MapDisplayManager : MonoBehaviour
{
	[Header("UI")]
	[SerializeField] private UIDocument uiDocument;
	[SerializeField] private Sprite playerMarkerSprite;

	[Header("Exploration Settings")]
	public float revealRadius = 20f;
	public float updateInterval = 1f;

	[Header("Zoom Settings")]
	public float minZoom = 1f;
	public float maxZoom = 5f;
	public float zoomSpeed = 0.5f;

	[Header("Player Reference")]
	public Transform playerTransform;

	private Texture2D mapTexture;
	private bool[,] visitedTiles;
	private MapManager mapManager;

	private VisualElement mapRoot;
	private VisualElement mapViewport;
	private VisualElement mapCanvas;
	private VisualElement playerMarkerEl;

	[SerializeField] private bool isMapOpen = false;

	private bool isFullMapRevealed = false;
	private int worldSize;
	private float lastUpdateTime;

	private float zoomLevel = 1f;
	private Vector2 panOffset = Vector2.zero;
	private bool isDragging = false;
	private Vector2 lastPointerPos;
	private int dragPointerId = -1;

	private const float viewportSize = 196f;

	private void Start()
	{
		TryInit();
	}

	private bool TryInit()
	{
		if (mapTexture != null) return true;

		if (uiDocument == null) return false;

		mapManager = MapManager.Instance;
		if (mapManager == null || (worldSize = mapManager.worldSize) <= 0) return false;

		var root = uiDocument.rootVisualElement;
		mapRoot = root.Q<VisualElement>("map-root");
		mapViewport = root.Q<VisualElement>("map-viewport");
		mapCanvas = root.Q<VisualElement>("map-canvas");
		playerMarkerEl = root.Q<VisualElement>("map-player-marker");

		if (mapRoot == null || mapViewport == null || mapCanvas == null || playerMarkerEl == null) return false;

		visitedTiles = new bool[worldSize, worldSize];
		mapTexture = new Texture2D(worldSize, worldSize, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};

		Color[] initialColors = new Color[worldSize * worldSize];
		for (int i = 0; i < initialColors.Length; i++)
			initialColors[i] = new Color(0f, 0f, 0f, 0f);

		mapTexture.SetPixels(initialColors);
		mapTexture.Apply();

		mapCanvas.style.backgroundImage = new StyleBackground(mapTexture);

		if (playerMarkerSprite != null)
			playerMarkerEl.style.backgroundImage = new StyleBackground(playerMarkerSprite);

		zoomLevel = minZoom;
		panOffset = Vector2.zero;
		ApplyZoomPan();

		mapViewport.RegisterCallback<WheelEvent>(OnWheel);
		mapViewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
		mapViewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
		mapViewport.RegisterCallback<PointerUpEvent>(OnPointerUp);

		isMapOpen = false;
		mapRoot.style.display = DisplayStyle.None;

		EnsurePlayer();
		enabled = true;
		return true;
	}

	private void OnWheel(WheelEvent evt)
	{
		float delta = -evt.delta.y * zoomSpeed * 0.1f;
		float newZoom = Mathf.Clamp(zoomLevel + delta, minZoom, maxZoom);
		if (Mathf.Approximately(newZoom, zoomLevel)) return;

		Vector2 mouseInViewport = evt.localMousePosition;
		float oldCanvasSize = viewportSize * zoomLevel;
		float newCanvasSize = viewportSize * newZoom;

		float normX = (-panOffset.x + mouseInViewport.x) / oldCanvasSize;
		float normY = (-panOffset.y + mouseInViewport.y) / oldCanvasSize;

		panOffset.x = -(normX * newCanvasSize - mouseInViewport.x);
		panOffset.y = -(normY * newCanvasSize - mouseInViewport.y);

		zoomLevel = newZoom;
		ClampPan();
		ApplyZoomPan();
		evt.StopPropagation();
	}

	private void OnPointerDown(PointerDownEvent evt)
	{
		if (evt.button != 0) return;
		isDragging = true;
		lastPointerPos = evt.position;
		dragPointerId = evt.pointerId;
		mapViewport.CapturePointer(evt.pointerId);
		evt.StopPropagation();
	}

	private void OnPointerMove(PointerMoveEvent evt)
	{
		if (!isDragging) return;
		Vector2 delta = (Vector2)evt.position - lastPointerPos;
		lastPointerPos = evt.position;
		panOffset += delta;
		ClampPan();
		ApplyZoomPan();
	}

	private void OnPointerUp(PointerUpEvent evt)
	{
		if (!isDragging) return;
		isDragging = false;
		mapViewport.ReleasePointer(evt.pointerId);
		dragPointerId = -1;
	}

	private void ClampPan()
	{
		float canvasSize = viewportSize * zoomLevel;
		float maxPan = canvasSize - viewportSize;
		panOffset.x = Mathf.Clamp(panOffset.x, -maxPan, 0f);
		panOffset.y = Mathf.Clamp(panOffset.y, -maxPan, 0f);
	}

	private void ApplyZoomPan()
	{
		if (mapCanvas == null) return;
		float canvasSize = viewportSize * zoomLevel;
		mapCanvas.style.width = canvasSize;
		mapCanvas.style.height = canvasSize;
		mapCanvas.style.left = panOffset.x;
		mapCanvas.style.top = panOffset.y;
	}

	private void Update()
	{
		if (mapTexture == null && !TryInit()) return;

		if (GameInput.RevealMapDown)
		{
			isFullMapRevealed = !isFullMapRevealed;
			if (isFullMapRevealed)
				RevealFullMap();
			else
				ResetMap();
		}

		if (!EnsurePlayer()) return;

		if (Time.time - lastUpdateTime >= updateInterval)
		{
			UpdateVisitedTiles();
			lastUpdateTime = Time.time;
		}

		if (isMapOpen)
		{
			Vector2Int playerTilePos = WorldToTilePosition(playerTransform.position);
			float canvasSize = viewportSize * zoomLevel;

			float normalizedX = (float)playerTilePos.x / worldSize;
			float normalizedY = (float)playerTilePos.y / worldSize;

			float markerW = playerMarkerEl.layout.width;
			float markerH = playerMarkerEl.layout.height;

			playerMarkerEl.style.left = normalizedX * canvasSize - markerW * 0.5f;
			playerMarkerEl.style.top = (1f - normalizedY) * canvasSize - markerH * 0.5f;
			playerMarkerEl.style.rotate = new Rotate(Angle.Degrees(-playerTransform.eulerAngles.z));
		}
	}

	public void OpenMap()
	{
		isMapOpen = true;
		if (mapRoot != null)
			mapRoot.style.display = DisplayStyle.Flex;
	}

	public void CloseMap()
	{
		isMapOpen = false;
		if (isDragging && dragPointerId >= 0 && mapViewport != null)
			mapViewport.ReleasePointer(dragPointerId);
		isDragging = false;
		dragPointerId = -1;
		if (mapRoot != null)
			mapRoot.style.display = DisplayStyle.None;
	}

	public void ToggleMap()
	{
		if (isMapOpen) CloseMap();
		else OpenMap();
	}

	public bool IsMapOpen() => isMapOpen;

	private bool EnsurePlayer()
	{
		if (playerTransform != null) return true;

		GameObject go = GameObject.FindGameObjectWithTag("Player");
		if (go != null)
		{
			playerTransform = go.transform;
			return true;
		}

		var playerController = FindAnyObjectByType<PlayerController>();
		if (playerController != null)
		{
			playerTransform = playerController.transform;
			return true;
		}

		return false;
	}

	private void UpdateVisitedTiles()
	{
		if (playerTransform == null) return;

		Vector2Int playerTilePos = WorldToTilePosition(playerTransform.position);
		int radiusCeil = Mathf.CeilToInt(revealRadius);

		int minX = Mathf.Max(playerTilePos.x - radiusCeil, 0);
		int maxX = Mathf.Min(playerTilePos.x + radiusCeil, worldSize - 1);
		int minY = Mathf.Max(playerTilePos.y - radiusCeil, 0);
		int maxY = Mathf.Min(playerTilePos.y + radiusCeil, worldSize - 1);

		bool updated = false;
		for (int x = minX; x <= maxX; x++)
		{
			for (int y = minY; y <= maxY; y++)
			{
				if (!visitedTiles[x, y] && Vector2.Distance(new Vector2(x, y), playerTilePos) <= revealRadius)
				{
					visitedTiles[x, y] = true;
					Color tileColor = mapManager.GetTileColor(x, y);
					tileColor.a = 1f;
					mapTexture.SetPixel(x, y, tileColor);
					updated = true;
				}
			}
		}

		if (updated)
		{
			mapTexture.Apply();
			mapCanvas.style.backgroundImage = new StyleBackground(mapTexture);
		}
	}

	private Vector2Int WorldToTilePosition(Vector3 worldPos)
	{
		int tx = Mathf.Clamp(Mathf.FloorToInt(worldPos.x + (worldSize / 2f)), 0, worldSize - 1);
		int ty = Mathf.Clamp(Mathf.FloorToInt(worldPos.y + (worldSize / 2f)), 0, worldSize - 1);
		return new Vector2Int(tx, ty);
	}

	private void RevealFullMap()
	{
		for (int x = 0; x < worldSize; x++)
		{
			for (int y = 0; y < worldSize; y++)
			{
				visitedTiles[x, y] = true;
				Color tileColor = mapManager.GetTileColor(x, y);
				tileColor.a = 1f;
				mapTexture.SetPixel(x, y, tileColor);
			}
		}
		mapTexture.Apply();
		mapCanvas.style.backgroundImage = new StyleBackground(mapTexture);
	}

	private void ResetMap()
	{
		for (int x = 0; x < worldSize; x++)
		{
			for (int y = 0; y < worldSize; y++)
			{
				visitedTiles[x, y] = false;
				mapTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
			}
		}
		mapTexture.Apply();
		mapCanvas.style.backgroundImage = new StyleBackground(mapTexture);
	}

	public void ResetForNewMap()
	{
		if (mapTexture != null)
		{
			Destroy(mapTexture);
			mapTexture = null;
		}
		visitedTiles = null;
		worldSize = 0;
		isFullMapRevealed = false;
		zoomLevel = minZoom;
		panOffset = Vector2.zero;
	}

	public PlayerMapData CapturePlayerMapState()
	{
		var data = new PlayerMapData();
		data.worldSize = worldSize;

		int total = worldSize * worldSize;
		int bytes = (total + 7) / 8;
		var packed = new byte[bytes];

		int bit = 0;
		for (int y = 0; y < worldSize; y++)
		{
			for (int x = 0; x < worldSize; x++)
			{
				if (visitedTiles[x, y])
					packed[bit >> 3] |= (byte)(1 << (bit & 7));
				bit++;
			}
		}

		data.visited = packed;
		return data;
	}

	public void RestorePlayerMapState(PlayerMapData data)
	{
		if (data == null || data.visited == null) return;
		if (mapTexture == null && !TryInit()) return;
		if (mapTexture == null) return;
		if (data.worldSize != worldSize) return;

		int bit = 0;
		for (int y = 0; y < worldSize; y++)
		{
			for (int x = 0; x < worldSize; x++)
			{
				bool v = (data.visited[bit >> 3] & (1 << (bit & 7))) != 0;
				visitedTiles[x, y] = v;

				if (v)
				{
					Color tileColor = mapManager.GetTileColor(x, y);
					tileColor.a = 1f;
					mapTexture.SetPixel(x, y, tileColor);
				}
				else
				{
					mapTexture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
				}

				bit++;
			}
		}

		mapTexture.Apply();
		mapCanvas.style.backgroundImage = new StyleBackground(mapTexture);
	}
}

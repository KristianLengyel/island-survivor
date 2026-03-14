using UnityEngine;
using UnityEngine.UI;

public class MapDisplayManager : MonoBehaviour
{
	[Header("UI Components")]
	public GameObject mapUI;
	public RawImage mapRawImage;
	public Image playerMarker;

	[Header("Exploration Settings")]
	public float revealRadius = 20f;
	public float updateInterval = 1f;

	[Header("Player Reference")]
	public Transform playerTransform;

	private Texture2D mapTexture;
	private bool[,] visitedTiles;
	private MapManager mapManager;

	[SerializeField] private bool isMapOpen = false;

	private bool isFullMapRevealed = false;
	private int worldSize;
	private float lastUpdateTime;

	private void Start()
	{
		TryInit();
	}

	private bool TryInit()
	{
		if (mapTexture != null) return true; // already initialized

		if (mapUI == null || mapRawImage == null || playerMarker == null) return false;

		mapManager = MapManager.Instance;
		if (mapManager == null || (worldSize = mapManager.worldSize) <= 0) return false;

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
		mapRawImage.texture = mapTexture;

		mapRawImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
		mapRawImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		mapRawImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		mapRawImage.rectTransform.anchoredPosition = Vector2.zero;

		playerMarker.transform.SetParent(mapRawImage.transform, false);
		playerMarker.rectTransform.anchoredPosition = Vector2.zero;

		isMapOpen = false;
		mapUI.SetActive(false);

		EnsurePlayer();
		enabled = true;
		return true;
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

		if (!EnsurePlayer())
			return;

		if (Time.time - lastUpdateTime >= updateInterval)
		{
			UpdateVisitedTiles();
			lastUpdateTime = Time.time;
		}

		if (isMapOpen)
		{
			Vector2Int playerTilePos = WorldToTilePosition(playerTransform.position);
			RectTransform mapRect = mapRawImage.rectTransform;
			float mapWidth = mapRect.rect.width;
			float mapHeight = mapRect.rect.height;

			float normalizedX = (float)playerTilePos.x / worldSize;
			float normalizedY = (float)playerTilePos.y / worldSize;

			float uiX = (normalizedX - 0.5f) * mapWidth;
			float uiY = (normalizedY - 0.5f) * mapHeight;

			playerMarker.rectTransform.anchoredPosition = new Vector2(uiX, uiY);
			playerMarker.rectTransform.rotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y);
		}
	}

	public void OpenMap()
	{
		isMapOpen = true;
		if (mapUI != null) mapUI.SetActive(true);
	}

	public void CloseMap()
	{
		isMapOpen = false;
		if (mapUI != null) mapUI.SetActive(false);
	}

	public void ToggleMap()
	{
		if (isMapOpen) CloseMap();
		else OpenMap();
	}

	public bool IsMapOpen()
	{
		return isMapOpen;
	}

	private bool EnsurePlayer()
	{
		if (playerTransform != null)
			return true;

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
			mapTexture.Apply();
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
		if (mapTexture == null && !TryInit()) return; // retry init if MapManager is ready now
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
	}
}

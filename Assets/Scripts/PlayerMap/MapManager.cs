using UnityEngine;

public class MapManager : MonoBehaviour
{
	public static MapManager Instance;
	public int worldSize;
	public Color[,] tileColors;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public void InitializeWorld(int size)
	{
		worldSize = size;
		tileColors = new Color[size, size];
		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				tileColors[x, y] = Color.black;
			}
		}
	}

	public Color GetTileColor(int x, int y)
	{
		if (x < 0 || x >= worldSize || y < 0 || y >= worldSize)
			return Color.black;

		return tileColors[x, y];
	}
}

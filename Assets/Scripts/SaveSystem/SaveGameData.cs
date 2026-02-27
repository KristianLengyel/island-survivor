using System;
using System.Collections.Generic;

[Serializable]
public class SaveGameData
{
	public int version = 1;

	public PlayerData player = new PlayerData();
	public PlayerStatsData playerStats = new PlayerStatsData();

	public WeatherData weather = new WeatherData();
	public DayNightData dayNight = new DayNightData();
	public PlayerMapData playerMap = new PlayerMapData();

	public InventoryData inventory = new InventoryData();

	public List<TilemapLayerData> tilemaps = new List<TilemapLayerData>();
	public List<WorldObjectData> worldObjects = new List<WorldObjectData>();
}

[Serializable]
public class PlayerData
{
	public float px, py, pz;
}

[Serializable]
public class PlayerStatsData
{
	public float thirst = 100f;
	public float hunger = 100f;
}

[Serializable]
public class WeatherData
{
	public bool rainActive;
	public bool fogActive;
}

[Serializable]
public class DayNightData
{
	public float cycleTimer;
	public int dayCount;
	public bool isRainyDay;
}

[Serializable]
public class PlayerMapData
{
	public int worldSize;
	public byte[] visited;
}

[Serializable]
public class InventorySlotData
{
	public string itemId;
	public int count;

	public bool isWaterContainer;
	public int waterFill;
	public bool isSaltWater;
}

[Serializable]
public class InventoryData
{
	public int selectedSlotIndex;
	public List<InventorySlotData> slots = new List<InventorySlotData>();
}

[Serializable]
public class TileData
{
	public int x, y, z;
	public string tileId;
}

[Serializable]
public class TilemapLayerData
{
	public string layerId;
	public List<TileData> tiles = new List<TileData>();
}

[System.Serializable]
public class ComponentStateData
{
	public string key;
	public string json;
}

[Serializable]
public class WorldObjectData
{
	public string uniqueId;
	public string prefabItemId;

	public float px, py, pz;
	public float rx, ry, rz, rw;

	public List<ComponentStateData> components = new List<ComponentStateData>();
}

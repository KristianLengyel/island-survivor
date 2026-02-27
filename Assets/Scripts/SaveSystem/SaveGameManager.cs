using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SaveGameManager : MonoBehaviour
{
	[Header("Databases")]
	public ItemDatabase itemDatabase;
	public TileDatabase tileDatabase;

	[Header("References")]
	public Transform player;
	public InventoryManager inventoryManager;
	public WeatherManager weatherManager;
	public DayNightCycle dayNightCycle;

	[Header("Player Stats")]
	public PlayerStats playerStats;

	[Header("Placed Prefabs")]
	public Transform placedPrefabsParent;
	public string placedPrefabTag = "PlaceableObject";

	[Header("Tilemaps to save")]
	public List<TilemapBinding> tilemaps = new List<TilemapBinding>();

	[Serializable]
	public class TilemapBinding
	{
		public string layerId;
		public Tilemap tilemap;
	}

	private void Awake()
	{
		if (itemDatabase != null) itemDatabase.Build();
		if (tileDatabase != null) tileDatabase.Build();

		if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
		if (placedPrefabsParent == null)
		{
			var go = GameObject.Find("PlacedPrefabs");
			if (go != null) placedPrefabsParent = go.transform;
		}
	}

	private void Start()
	{
		if (GameBoot.LoadRequested && SaveIO.Exists())
		{
			GameBoot.LoadRequested = false;
			Load();
		}
	}

	private void Update()
	{
		if (GameInput.QuickSaveDown) Save();
		if (GameInput.QuickLoadDown) Load();
	}

	public void Save()
	{
		var data = new SaveGameData();

		if (player != null)
		{
			var p = player.position;
			data.player.px = p.x; data.player.py = p.y; data.player.pz = p.z;
		}

		if (playerStats != null)
		{
			data.playerStats.thirst = playerStats.GetCurrentThirst();
			data.playerStats.hunger = playerStats.GetCurrentHunger();
		}

		if (weatherManager != null)
		{
			data.weather.rainActive = weatherManager.IsRainActive();
			data.weather.fogActive = weatherManager.IsFogActive();
		}

		if (dayNightCycle != null)
		{
			data.dayNight.cycleTimer = dayNightCycle.cycleTimer;
			data.dayNight.dayCount = dayNightCycle.GetDayCount();
			data.dayNight.isRainyDay = dayNightCycle.GetIsRainyDay();
		}

		if (inventoryManager != null)
			data.inventory = inventoryManager.CaptureInventoryState();

		var mapDisplay = FindAnyObjectByType<MapDisplayManager>();
		if (mapDisplay != null)
			data.playerMap = mapDisplay.CapturePlayerMapState();

		data.tilemaps.Clear();
		for (int i = 0; i < tilemaps.Count; i++)
		{
			var b = tilemaps[i];
			if (b == null || string.IsNullOrEmpty(b.layerId)) continue;
			data.tilemaps.Add(TilemapSerializer.Capture(b.layerId, b.tilemap));
		}

		data.worldObjects.Clear();
		var entities = FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);
		for (int i = 0; i < entities.Length; i++)
		{
			var e = entities[i];
			if (e == null) continue;

			var holder = e.GetComponent<ItemHolder>();
			if (holder == null || holder.item == null) continue;

			var wod = new WorldObjectData
			{
				uniqueId = e.UniqueId,
				prefabItemId = holder.item.name
			};

			var tr = e.transform;
			wod.px = tr.position.x; wod.py = tr.position.y; wod.pz = tr.position.z;
			wod.rx = tr.rotation.x; wod.ry = tr.rotation.y; wod.rz = tr.rotation.z; wod.rw = tr.rotation.w;

			var comps = e.GetComponents<ISaveableComponent>();
			for (int c = 0; c < comps.Length; c++)
			{
				var comp = comps[c];
				wod.components.Add(new ComponentStateData
				{
					key = comp.SaveKey,
					json = comp.CaptureStateJson()
				});
			}

			data.worldObjects.Add(wod);
		}

		SaveIO.Write(data);
	}

	public void Load()
	{
		var data = SaveIO.Read();
		if (data == null) return;

		for (int i = 0; i < tilemaps.Count; i++)
		{
			var b = tilemaps[i];
			if (b == null || string.IsNullOrEmpty(b.layerId)) continue;

			var layer = data.tilemaps.Find(x => x.layerId == b.layerId);
			if (layer != null)
				TilemapSerializer.Restore(layer, b.tilemap, tileDatabase);
		}

		var existing = FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);
		for (int i = 0; i < existing.Length; i++)
		{
			var e = existing[i];
			var holder = e.GetComponent<ItemHolder>();
			if (holder != null && holder.item != null)
				Destroy(e.gameObject);
		}

		for (int i = 0; i < data.worldObjects.Count; i++)
		{
			var wod = data.worldObjects[i];
			var item = itemDatabase.Get(wod.prefabItemId);
			if (item == null || item.prefab == null) continue;

			var go = Instantiate(item.prefab, placedPrefabsParent);
			go.transform.position = new Vector3(wod.px, wod.py, wod.pz);
			go.transform.rotation = new Quaternion(wod.rx, wod.ry, wod.rz, wod.rw);

			if (!string.IsNullOrEmpty(placedPrefabTag))
				go.tag = placedPrefabTag;

			var entity = go.GetComponent<SaveableEntity>();
			if (entity == null) entity = go.AddComponent<SaveableEntity>();
			SetPrivateId(entity, wod.uniqueId);

			var holder = go.GetComponent<ItemHolder>();
			if (holder == null) holder = go.AddComponent<ItemHolder>();
			holder.item = item;

			var comps = go.GetComponents<ISaveableComponent>();
			for (int c = 0; c < comps.Length; c++)
			{
				var comp = comps[c];
				var csd = wod.components.Find(x => x.key == comp.SaveKey);
				if (csd == null) continue;
				comp.RestoreStateJson(csd.json);
			}
		}

		if (player != null)
			player.position = new Vector3(data.player.px, data.player.py, data.player.pz);

		if (playerStats != null)
			playerStats.SetState(data.playerStats.thirst, data.playerStats.hunger);

		if (weatherManager != null)
		{
			weatherManager.EnableRain(data.weather.rainActive);
			weatherManager.EnableFog(data.weather.fogActive);
		}

		if (dayNightCycle != null)
			dayNightCycle.SetState(data.dayNight.cycleTimer, data.dayNight.dayCount, data.dayNight.isRainyDay);

		if (inventoryManager != null)
			inventoryManager.RestoreInventoryState(data.inventory, itemDatabase);

		var mapDisplay = FindAnyObjectByType<MapDisplayManager>();
		if (mapDisplay != null)
			mapDisplay.RestorePlayerMapState(data.playerMap);
	}

	private static void SetPrivateId(SaveableEntity entity, string id)
	{
		var f = typeof(SaveableEntity).GetField("uniqueId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		if (f != null) f.SetValue(entity, id);
	}
}

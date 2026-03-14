using System;
using System.Collections;
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
	public PlayerStats playerStats;
	public MapDisplayManager mapDisplayManager;
	public MapGeneratorV3 mapGenerator;

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

	// Tilemaps managed by the map generator — skip saving these (regenerated from seed).
	private HashSet<Tilemap> _mapGenTilemaps;

	private void Awake()
	{
		if (itemDatabase != null) itemDatabase.Build();
		if (tileDatabase != null) tileDatabase.Build();

		if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
		if (mapDisplayManager == null) mapDisplayManager = FindAnyObjectByType<MapDisplayManager>();
		if (mapGenerator == null) mapGenerator = FindAnyObjectByType<MapGeneratorV3>();

		if (placedPrefabsParent == null)
		{
			var go = GameObject.Find("PlacedPrefabs");
			if (go != null) placedPrefabsParent = go.transform;
		}

		BuildMapGenTilemapSet();

		// Force the map generator to use the saved seed before its Start() runs,
		// so palm trees and other decorations regenerate at the exact same positions.
		if (GameBoot.LoadRequested && SaveIO.Exists() && mapGenerator != null)
		{
			var earlyData = SaveIO.Read();
			if (earlyData != null && earlyData.mapSeed != 0)
			{
				mapGenerator.ForceSeed(earlyData.mapSeed);
				Debug.Log($"[SaveGameManager] Forcing map seed: {earlyData.mapSeed}");
			}
		}
	}

	private IEnumerator Start()
	{
		// Wait for map generation to finish so tilemaps are fully painted
		// before we restore saved state on top of them.
		if (mapGenerator != null)
		{
			while (mapGenerator.IsGenerating)
				yield return null;
		}

		// One extra frame so the streamer's OnMapRegenerated + initial CheckBoundaryCrossing runs.
		yield return null;

		if (GameBoot.LoadRequested && SaveIO.Exists())
		{
			GameBoot.LoadRequested = false;
			Load();
		}
	}

	private void BuildMapGenTilemapSet()
	{
		_mapGenTilemaps = new HashSet<Tilemap>();
		if (mapGenerator == null) return;
		if (mapGenerator.waterTilemap != null) _mapGenTilemaps.Add(mapGenerator.waterTilemap);
		if (mapGenerator.landTilemap != null) _mapGenTilemaps.Add(mapGenerator.landTilemap);
		if (mapGenerator.grassTilemap != null) _mapGenTilemaps.Add(mapGenerator.grassTilemap);
		if (mapGenerator.oceanOverlayTilemap != null) _mapGenTilemaps.Add(mapGenerator.oceanOverlayTilemap);
		if (mapGenerator.oceanFloorShallowTilemap != null) _mapGenTilemaps.Add(mapGenerator.oceanFloorShallowTilemap);
		if (mapGenerator.oceanFloorMediumTilemap != null) _mapGenTilemaps.Add(mapGenerator.oceanFloorMediumTilemap);
		if (mapGenerator.oceanFloorDeepTilemap != null) _mapGenTilemaps.Add(mapGenerator.oceanFloorDeepTilemap);
		// NOTE: dockTilemap intentionally excluded — it shares the same Tilemap object as
		// buildingTilemap in the current scene setup, so it must be saved/loaded as a player tilemap.
		// TODO: give the dock its own dedicated Tilemap in the scene and re-add it here.
	}

	private bool IsMapGenTilemap(Tilemap tm)
	{
		return _mapGenTilemaps != null && _mapGenTilemaps.Contains(tm);
	}

	private void Update()
	{
		if (GameInput.QuickSaveDown) Save();
		if (GameInput.QuickLoadDown) QuickLoad();
	}

	private void QuickLoad()
	{
		StartCoroutine(QuickLoadCoroutine());
	}

	private IEnumerator QuickLoadCoroutine()
	{
		var data = SaveIO.Read();
		if (data == null) yield break;

		// If the saved seed differs from the current map, regenerate before restoring state.
		// (Happens when a new random map was generated at scene start but the player loads a save.)
		if (mapGenerator != null && data.mapSeed != 0 && data.mapSeed != mapGenerator.LastSeed)
		{
			mapGenerator.ForceSeed(data.mapSeed);
			mapGenerator.Regenerate();

			while (mapGenerator.IsGenerating)
				yield return null;

			// One extra frame so the streamer's OnMapRegenerated + CheckBoundaryCrossing runs.
			yield return null;
		}

		Load();
	}

	public void Save()
	{
		var data = new SaveGameData();

		if (mapGenerator != null)
		{
			data.mapSeed = mapGenerator.LastSeed;
			Debug.Log($"[SaveGameManager] Saved map seed: {data.mapSeed}");
		}

		if (player != null)
		{
			var p = player.position;
			data.player.px = p.x; data.player.py = p.y; data.player.pz = p.z;
		}

		if (playerStats != null)
		{
			data.playerStats.thirst = playerStats.GetCurrentThirst();
			data.playerStats.hunger = playerStats.GetCurrentHunger();
			data.playerStats.stamina = playerStats.GetCurrentStamina();
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
			data.dayNight.isStormyDay = dayNightCycle.GetIsStormyDay();
		}

		if (inventoryManager != null)
			data.inventory = inventoryManager.CaptureInventoryState();

		if (mapDisplayManager != null)
			data.playerMap = mapDisplayManager.CapturePlayerMapState();

		// Only save player-built tilemaps (building, objects), not map-gen tilemaps
		// (water, land, grass, ocean) — those are deterministically regenerated from the seed.
		data.tilemaps.Clear();
		for (int i = 0; i < tilemaps.Count; i++)
		{
			var b = tilemaps[i];
			if (b == null || string.IsNullOrEmpty(b.layerId)) continue;
			if (IsMapGenTilemap(b.tilemap)) continue;
			data.tilemaps.Add(TilemapSerializer.Capture(b.layerId, b.tilemap));
		}

		data.worldObjects.Clear();
		// Only save entities that are children of placedPrefabsParent (player-placed buildings).
		// Map-generated objects (palm trees, rocks) are excluded — their positions come from the seed.
		var entities = placedPrefabsParent != null
			? placedPrefabsParent.GetComponentsInChildren<SaveableEntity>()
			: FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);
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
				wod.components.Add(new ComponentStateData
				{
					key = comps[c].SaveKey,
					json = comps[c].CaptureStateJson()
				});
			}

			data.worldObjects.Add(wod);
		}

		var streamer = mapGenerator != null ? mapGenerator.GetComponent<MapChunkStreamerV3>() : null;
		if (streamer != null)
		{
			data.felledPalmIndices.AddRange(streamer._felledPalmIndices);
			data.felledRockIndices.AddRange(streamer._felledRockIndices);
		}

		SaveIO.Write(data);
	}

	public void Load()
	{
		var data = SaveIO.Read();
		if (data == null) return;

		var streamer = mapGenerator != null ? mapGenerator.GetComponent<MapChunkStreamerV3>() : null;
		if (streamer != null)
		{
			streamer._felledPalmIndices.Clear();
			foreach (var idx in data.felledPalmIndices) streamer._felledPalmIndices.Add(idx);
			streamer._felledRockIndices.Clear();
			foreach (var idx in data.felledRockIndices) streamer._felledRockIndices.Add(idx);
		}

		// Restore only player-built tilemaps (skip map-gen tilemaps).
		for (int i = 0; i < tilemaps.Count; i++)
		{
			var b = tilemaps[i];
			if (b == null || string.IsNullOrEmpty(b.layerId)) continue;
			if (IsMapGenTilemap(b.tilemap)) continue;

			var layer = data.tilemaps.Find(x => x.layerId == b.layerId);
			if (layer != null)
				TilemapSerializer.Restore(layer, b.tilemap, tileDatabase);
		}

		GameManager.Instance.BuildingManager.RunFloodFill();

		// Clear only player-placed objects under placedPrefabsParent.
		// Never touch map-generated objects (palms, rocks) — those are managed by the chunk streamer.
		if (placedPrefabsParent != null)
		{
			for (int i = placedPrefabsParent.childCount - 1; i >= 0; i--)
				Destroy(placedPrefabsParent.GetChild(i).gameObject);
		}

		// Recreate world objects but defer ISaveableComponent restoration
		// until after inventory is restored (otherwise RestoreStateJson additions
		// like catching net items get overwritten by RestoreInventoryState).
		var deferredComponents = new List<(ISaveableComponent comp, string json)>();

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

			var entity = go.GetComponent<SaveableEntity>() ?? go.AddComponent<SaveableEntity>();
			entity.SetId(wod.uniqueId);

			var holder = go.GetComponent<ItemHolder>() ?? go.AddComponent<ItemHolder>();
			holder.item = item;

			var comps = go.GetComponents<ISaveableComponent>();
			for (int c = 0; c < comps.Length; c++)
			{
				var csd = wod.components.Find(x => x.key == comps[c].SaveKey);
				if (csd != null)
					deferredComponents.Add((comps[c], csd.json));
			}
		}

		// Restore game state
		if (player != null)
			player.position = new Vector3(data.player.px, data.player.py, data.player.pz);

		if (playerStats != null)
			playerStats.SetState(data.playerStats.thirst, data.playerStats.hunger, data.playerStats.stamina);

		if (weatherManager != null)
		{
			weatherManager.EnableRain(data.weather.rainActive);
			weatherManager.EnableFog(data.weather.fogActive);
		}

		if (dayNightCycle != null)
			dayNightCycle.SetState(data.dayNight.cycleTimer, data.dayNight.dayCount, data.dayNight.isRainyDay, data.dayNight.isStormyDay);

		if (inventoryManager != null)
			inventoryManager.RestoreInventoryState(data.inventory, itemDatabase);

		if (mapDisplayManager != null)
			mapDisplayManager.RestorePlayerMapState(data.playerMap);

		// Now restore ISaveableComponent state (e.g. catching net items → inventory)
		// after inventory has been restored so additions aren't overwritten.
		for (int i = 0; i < deferredComponents.Count; i++)
			deferredComponents[i].comp.RestoreStateJson(deferredComponents[i].json);
	}
}

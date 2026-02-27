using UnityEngine;

public class MapMenuAdapter : MonoBehaviour, IGameMenu
{
	[SerializeField] private string key = "Map";
	[SerializeField] private bool isOverlay = false;

	private MapDisplayManager map;
	private bool registered;

	public string Key => key;
	public bool IsOverlay => isOverlay;
	public bool IsOpen => map != null && map.IsMapOpen();

	private void Start()
	{
		var coordinator = MenuCoordinator.Instance != null ? MenuCoordinator.Instance : FindAnyObjectByType<MenuCoordinator>();
		if (coordinator == null)
		{
			enabled = false;
			return;
		}

		map = FindAnyObjectByType<MapDisplayManager>();

		coordinator.Register(this);
		registered = true;
	}

	private void OnDestroy()
	{
		if (registered && MenuCoordinator.Instance != null)
			MenuCoordinator.Instance.Unregister(this);
	}

	public void Open()
	{
		if (map == null) map = FindAnyObjectByType<MapDisplayManager>();
		if (map == null) return;

		map.OpenMap();
	}

	public void Close()
	{
		if (map == null) map = FindAnyObjectByType<MapDisplayManager>();
		if (map == null) return;

		map.CloseMap();
	}
}

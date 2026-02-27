using UnityEngine;

public class BuildMenuAdapter : MonoBehaviour, IGameMenu
{
	[SerializeField] private string key = "BuildMenu";
	[SerializeField] private bool isOverlay = false;

	private BuildingMenuManager build;
	private bool registered;

	public string Key => key;
	public bool IsOverlay => isOverlay;
	public bool IsOpen => build != null && build.IsBuildingMenuOpen;

	private void Start()
	{
		var coordinator = MenuCoordinator.Instance != null ? MenuCoordinator.Instance : FindAnyObjectByType<MenuCoordinator>();
		if (coordinator == null)
		{
			enabled = false;
			return;
		}

		if (GameManager.Instance != null)
			build = GameManager.Instance.BuildingMenuManager;

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
		if (build == null)
		{
			if (GameManager.Instance != null) build = GameManager.Instance.BuildingMenuManager;
			if (build == null) return;
		}

		build.ToggleBuildingMenu();
	}

	public void Close()
	{
		if (build == null)
		{
			if (GameManager.Instance != null) build = GameManager.Instance.BuildingMenuManager;
			if (build == null) return;
		}

		build.CloseBuildingMenu();
	}
}

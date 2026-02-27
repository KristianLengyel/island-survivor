using UnityEngine;

public class InventoryMenuAdapter : MonoBehaviour, IGameMenu
{
	[SerializeField] private string key = "Inventory";
	[SerializeField] private bool isOverlay = false;

	private InventoryManager inv;
	private bool registered;

	public string Key => key;
	public bool IsOverlay => isOverlay;
	public bool IsOpen => inv != null && inv.IsInventoryOpen();

	private void Start()
	{
		var coordinator = MenuCoordinator.Instance != null ? MenuCoordinator.Instance : FindAnyObjectByType<MenuCoordinator>();
		if (coordinator == null)
		{
			enabled = false;
			return;
		}

		if (GameManager.Instance != null)
			inv = GameManager.Instance.InventoryManager;

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
		if (inv == null)
		{
			if (GameManager.Instance != null) inv = GameManager.Instance.InventoryManager;
			if (inv == null) return;
		}

		if (!inv.InventoryLocked)
			inv.ShowInventory(true);
	}

	public void Close()
	{
		if (inv == null)
		{
			if (GameManager.Instance != null) inv = GameManager.Instance.InventoryManager;
			if (inv == null) return;
		}

		inv.ShowInventory(false);
	}
}

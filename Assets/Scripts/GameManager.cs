using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("References to Managers")]
	[SerializeField] private InventoryManager inventoryManager;
	[SerializeField] private BuildingManager buildingManager;
	[SerializeField] private BuildingMenuManager buildingMenuManager;
	[SerializeField] private UIManager uiManager;
	[SerializeField] private AudioManager audioManager;
	[SerializeField] private WeatherManager weatherManager;

	public InventoryManager InventoryManager => inventoryManager;
	public BuildingManager BuildingManager => buildingManager;
	public BuildingMenuManager BuildingMenuManager => buildingMenuManager;
	public UIManager UIManager => uiManager;
	public AudioManager AudioManager => audioManager;
	public WeatherManager WeatherManager => weatherManager;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	private void OnDestroy()
	{
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}
}

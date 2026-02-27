using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
	[SerializeField] private float landSpeed = 5f;
	[SerializeField] private float waterSpeed = 2f;
	[SerializeField] private float smoothing = 0.05f;

	[SerializeField] private float maxFillRange = 1f;
	[SerializeField] private Tilemap fillIndicatorTilemap;
	[SerializeField] private TileBase fillIndicatorTile;
	[SerializeField] private GameObject fillTextPrefab;
	[SerializeField] private Canvas uiCanvas;
	[SerializeField] private float textOffsetY = 1f;

	[SerializeField] private float stepSoundCooldown = 0.2f;

	private Rigidbody2D rb;
	private Animator animator;
	private Camera mainCamera;

	private WeatherManager weatherManager;
	private CameraMovement cameraMovement;
	private PlayerTileDetector playerTileDetector;
	private PlayerStats playerStats;

	private PlayerToolController toolController;
	private PlayerInputController inputController;
	private PlayerMovementMotor movementMotor;
	private PlayerAnimationDriver animationDriver;
	private PlayerInteractionController interactionController;
	private PlayerItemUseController itemUseController;
	private PlayerCarryController carryController;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		animator = GetComponent<Animator>();
		playerTileDetector = GetComponent<PlayerTileDetector>();
		playerStats = GetComponent<PlayerStats>();
		mainCamera = Camera.main;
		cameraMovement = mainCamera ? mainCamera.GetComponent<CameraMovement>() : null;
		weatherManager = FindAnyObjectByType<WeatherManager>();

		if (playerStats == null) playerStats = gameObject.AddComponent<PlayerStats>();

		inputController = GetOrAdd<PlayerInputController>();
		movementMotor = GetOrAdd<PlayerMovementMotor>();
		animationDriver = GetOrAdd<PlayerAnimationDriver>();
		interactionController = GetOrAdd<PlayerInteractionController>();
		itemUseController = GetOrAdd<PlayerItemUseController>();
		toolController = GetOrAdd<PlayerToolController>();
		carryController = GetOrAdd<PlayerCarryController>();

		toolController.Initialize();

		inputController.Initialize();
		movementMotor.Initialize(rb, playerTileDetector, landSpeed, waterSpeed, smoothing, cameraMovement);
		animationDriver.Initialize(animator, inputController, playerTileDetector);
		interactionController.Initialize(rb, mainCamera);

		itemUseController.Initialize(
			rb,
			mainCamera,
			playerTileDetector,
			playerStats,
			maxFillRange,
			fillIndicatorTilemap,
			fillIndicatorTile,
			fillTextPrefab,
			uiCanvas,
			textOffsetY
		);
	}

	private void Start()
	{
		if (rb) rb.interpolation = RigidbodyInterpolation2D.Interpolate;
		interactionController.PrimeMousePosition();
	}

	private void Update()
	{
		var inventoryManager = GameManager.Instance.InventoryManager;

		bool inventoryBlocked = inventoryManager != null && inventoryManager.IsInventoryOpen();
		bool busyBlocked = carryController != null && carryController.IsBusy;
		bool blocked = inventoryBlocked || busyBlocked;

		if (blocked)
		{
			inputController.SetBlocked();
			animationDriver.Tick();
			itemUseController.ClearIndicator();
			interactionController.ClearHighlight();
			return;
		}

		inputController.Tick();
		animationDriver.Tick();
		TickWeatherDebug();
		interactionController.Tick();
		itemUseController.Tick();
		toolController.Tick();

		if (GameInput.PickupDown)
		{
			carryController.TryPickUp();
		}

		if (GameInput.PutdownDown)
		{
			carryController.TryPutDown();
		}
	}

	private void FixedUpdate()
	{
		movementMotor.FixedTick(inputController.MovementInput);
		toolController.FixedTick();
	}

	private void TickWeatherDebug()
	{
		if (!weatherManager) return;
		if (GameInput.ToggleRainDown) weatherManager.ToggleRain();
		if (GameInput.ToggleFogDown) weatherManager.ToggleFog();
	}

	public void SetBounds(Vector3 minBounds, Vector3 maxBounds)
	{
		movementMotor.SetBounds(minBounds, maxBounds);
	}

	public void SetHookTransform(Transform hook, float toolMaxRange, MonoBehaviour tool)
	{
		movementMotor.SetHookTransform(hook, toolMaxRange, tool);
	}

	public void ClearHookTransform(MonoBehaviour tool)
	{
		movementMotor.ClearHookTransform(tool);
	}

	public void PlayStepSound()
	{
		if (!playerTileDetector) return;
		if (Time.time - movementMotor.LastStepSoundTime < stepSoundCooldown) return;

		string stepSound = playerTileDetector.IsInWater() ? "WaterStep" : "SoftStep";
		float randomPitch = Random.Range(0.9f, 1.1f);
		AudioManager.instance.PlaySound(stepSound, randomPitch);
		movementMotor.LastStepSoundTime = Time.time;
	}

	private T GetOrAdd<T>() where T : Component
	{
		var c = GetComponent<T>();
		if (!c) c = gameObject.AddComponent<T>();
		return c;
	}
}

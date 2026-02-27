using UnityEngine;

public sealed class GameInput : MonoBehaviour
{
	public static GameInput Instance { get; private set; }

	[SerializeField] private GameInputBindings bindings;

	private IInputBackend backend;
	private bool locked;

	private bool cancelConsumed;

	public static bool IsReady => Instance != null && Instance.backend != null;

	public static void SetLocked(bool value)
	{
		if (Instance != null) Instance.locked = value;
	}

	public static void ConsumeCancel()
	{
		if (Instance != null) Instance.cancelConsumed = true;
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		backend = new LegacyInputBackend();
	}

	private void Update()
	{
		if (bindings == null) return;

		cancelConsumed = false;

		backend.Tick(bindings);
	}

	public static Vector2 Move => Instance.backend.Move;
	public static Vector2 MouseScreen => Instance.backend.MouseScreen;
	public static float Scroll => Instance.backend.Scroll;

	public static bool ShiftHeld => Instance.backend.ShiftHeld;
	public static bool CtrlHeld => Instance.backend.CtrlHeld;

	public static bool InteractDown => Instance.backend.InteractDown;
	public static bool InventoryDown => Instance.backend.InventoryDown;
	public static bool BuildMenuDown => Instance.backend.BuildMenuDown;

	public static bool CancelDown => Instance.backend.CancelDown && !Instance.cancelConsumed;

	public static bool MapDown => Instance.backend.MapDown;
	public static bool RevealMapDown => Instance.backend.RevealMapDown;

	public static bool QuickSaveDown => Instance.backend.QuickSaveDown;
	public static bool QuickLoadDown => Instance.backend.QuickLoadDown;

	public static bool ToggleRainDown => Instance.backend.ToggleRainDown;
	public static bool ToggleFogDown => Instance.backend.ToggleFogDown;

	public static bool PickupDown => Instance.backend.PickupDown;
	public static bool PutdownDown => Instance.backend.PutdownDown;

	public static int ToolbarSlotDown => Instance.backend.ToolbarSlotDown;

	public static bool LmbDown => Instance.backend.LmbDown;
	public static bool LmbHeld => Instance.backend.LmbHeld;
	public static bool LmbUp => Instance.backend.LmbUp;

	public static bool RmbDown => Instance.backend.RmbDown;
	public static bool RmbHeld => Instance.backend.RmbHeld;
	public static bool RmbUp => Instance.backend.RmbUp;

	public static bool StackIncreaseDown => Instance.backend.StackIncreaseDown;
	public static bool StackDecreaseDown => Instance.backend.StackDecreaseDown;
	public static bool AdminConsoleDown => Instance.backend.AdminConsoleDown;
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
#if UNITY_EDITOR
	[SerializeField] private bool editorPreviewVisible = false;
#endif

	[Header("Scenes")]
	[SerializeField] private string mainMenuSceneName = "MainMenu";
	[SerializeField] private string loadingSceneName = "Loading";

	[Header("Optional")]
	[SerializeField] private bool allowToggleWhileTimeScaleZero = true;

	public static bool IsPaused { get; private set; }

	private UIDocument _doc;
	private VisualElement _root;

	private VisualElement _overlay;
	private VisualElement _mainPanel;
	private VisualElement _optionsPanel;

	private Button _resumeBtn;
	private Button _optionsBtn;
	private Button _saveBtn;
	private Button _quitBtn;
	private Button _backFromOptionsBtn;

	private SaveGameManager _saveGameManager;
	private bool _wired;

	private void Awake()
	{
		_doc = GetComponent<UIDocument>();
		_saveGameManager = FindAnyObjectByType<SaveGameManager>();
	}

	private void OnEnable()
	{
		CacheUi();
		ApplyEditorPreviewOrRuntimeHide();
	}

	private void OnDisable()
	{
		UnwireButtons();
		_wired = false;
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (Application.isPlaying) return;

		if (_doc == null) _doc = GetComponent<UIDocument>();
		if (_doc == null) return;

		CacheUi();
		ApplyEditorPreviewOrRuntimeHide();
	}
#endif

	private void CacheUi()
	{
		if (_doc == null) _doc = GetComponent<UIDocument>();
		if (_doc == null) return;

		_root = _doc.rootVisualElement;
		if (_root == null) return;

		_overlay = _root.Q<VisualElement>("overlay");
		_mainPanel = _root.Q<VisualElement>("mainPanel");
		_optionsPanel = _root.Q<VisualElement>("optionsPanel");

		_resumeBtn = _root.Q<Button>("resumeBtn");
		_optionsBtn = _root.Q<Button>("optionsBtn");
		_saveBtn = _root.Q<Button>("saveBtn");
		_quitBtn = _root.Q<Button>("quitBtn");
		_backFromOptionsBtn = _root.Q<Button>("backFromOptionsBtn");

		if (!ValidateUxmlBindings())
			return;

		WireButtons();
	}

	private void ApplyEditorPreviewOrRuntimeHide()
	{
		if (_overlay == null) return;

#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			_overlay.style.display = editorPreviewVisible ? DisplayStyle.Flex : DisplayStyle.None;

			if (_mainPanel != null) _mainPanel.style.display = editorPreviewVisible ? DisplayStyle.Flex : DisplayStyle.None;
			if (_optionsPanel != null) _optionsPanel.style.display = DisplayStyle.None;

			return;
		}
#endif

		HideInstant();
	}

	private void WireButtons()
	{
		if (_wired) return;
		if (_resumeBtn == null) return;

		_resumeBtn.clicked += Resume;
		_optionsBtn.clicked += OpenOptions;
		_saveBtn.clicked += Save;
		_quitBtn.clicked += SaveAndQuitToMenu;
		_backFromOptionsBtn.clicked += BackFromOptions;

		_wired = true;
	}

	private void UnwireButtons()
	{
		if (!_wired) return;

		if (_resumeBtn != null) _resumeBtn.clicked -= Resume;
		if (_optionsBtn != null) _optionsBtn.clicked -= OpenOptions;
		if (_saveBtn != null) _saveBtn.clicked -= Save;
		if (_quitBtn != null) _quitBtn.clicked -= SaveAndQuitToMenu;
		if (_backFromOptionsBtn != null) _backFromOptionsBtn.clicked -= BackFromOptions;
	}

	private bool ValidateUxmlBindings()
	{
		bool ok = true;

		ok &= Require(_overlay, "overlay");
		ok &= Require(_mainPanel, "mainPanel");
		ok &= Require(_optionsPanel, "optionsPanel");

		ok &= Require(_resumeBtn, "resumeBtn");
		ok &= Require(_optionsBtn, "optionsBtn");
		ok &= Require(_saveBtn, "saveBtn");
		ok &= Require(_quitBtn, "quitBtn");
		ok &= Require(_backFromOptionsBtn, "backFromOptionsBtn");

		if (!ok)
			Debug.LogError("[PauseMenuController] UXML bindings missing. Check element 'name' fields in PauseMenu.uxml.");

		return ok;
	}

	private bool Require(VisualElement el, string name)
	{
		if (el != null) return true;
		Debug.LogError($"[PauseMenuController] Missing VisualElement/Button named '{name}' in UXML.");
		return false;
	}

	private void Update()
	{
		if (!Application.isPlaying) return;
		if (_overlay == null) return;

		if (!allowToggleWhileTimeScaleZero && Time.timeScale == 0f) return;

		if (GameManager.Instance != null &&
			GameManager.Instance.InventoryManager != null &&
			GameManager.Instance.InventoryManager.IsAnyChestOpen())
		{
			return;
		}

		if (GameInput.CancelDown)
		{
			if (IsPaused) Resume();
			else Pause();
		}
	}

	public void Pause()
	{
		if (_overlay == null) return;
		if (IsPaused) return;

		IsPaused = true;
		Time.timeScale = 0f;

		_overlay.style.display = DisplayStyle.Flex;
		_mainPanel.style.display = DisplayStyle.Flex;
		_optionsPanel.style.display = DisplayStyle.None;

		_resumeBtn?.Focus();
	}

	public void Resume()
	{
		if (_overlay == null) return;
		if (!IsPaused) return;

		IsPaused = false;
		Time.timeScale = 1f;

		HideInstant();
	}

	private void HideInstant()
	{
		_overlay.style.display = DisplayStyle.None;
		_mainPanel.style.display = DisplayStyle.None;
		_optionsPanel.style.display = DisplayStyle.None;
	}

	private void OpenOptions()
	{
		_mainPanel.style.display = DisplayStyle.None;
		_optionsPanel.style.display = DisplayStyle.Flex;
		_backFromOptionsBtn?.Focus();
	}

	private void BackFromOptions()
	{
		_optionsPanel.style.display = DisplayStyle.None;
		_mainPanel.style.display = DisplayStyle.Flex;
		_resumeBtn?.Focus();
	}

	private void Save()
	{
		if (_saveGameManager == null)
			_saveGameManager = FindAnyObjectByType<SaveGameManager>();

		if (_saveGameManager != null)
			_saveGameManager.Save();
	}

	private void SaveAndQuitToMenu()
	{
		Save();

		IsPaused = false;
		Time.timeScale = 1f;

		if (!string.IsNullOrEmpty(loadingSceneName))
		{
			GameBoot.LoadRequested = false;
			GameBoot.NextSceneName = mainMenuSceneName;
			SceneManager.LoadScene(loadingSceneName);
			return;
		}

		SceneManager.LoadScene(mainMenuSceneName);
	}
}

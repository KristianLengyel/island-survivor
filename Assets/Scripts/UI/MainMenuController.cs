using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
	[SerializeField] private string gameSceneName = "Game";
	[SerializeField] private string loadingSceneName = "Loading";

	Button continueBtn;
	Button newGameBtn;
	Button optionsBtn;
	Button quitBtn;
	Button backFromOptionsBtn;

	VisualElement mainPanel;
	VisualElement optionsPanel;

	void Awake()
	{
		var root = GetComponent<UIDocument>().rootVisualElement;

		continueBtn = root.Q<Button>("continueBtn");
		newGameBtn = root.Q<Button>("newGameBtn");
		optionsBtn = root.Q<Button>("optionsBtn");
		quitBtn = root.Q<Button>("quitBtn");
		backFromOptionsBtn = root.Q<Button>("backFromOptionsBtn");

		mainPanel = root.Q<VisualElement>("mainPanel");
		optionsPanel = root.Q<VisualElement>("optionsPanel");

		continueBtn.clicked += ContinueGame;
		newGameBtn.clicked += NewGame;
		optionsBtn.clicked += OpenOptions;
		quitBtn.clicked += Quit;
		backFromOptionsBtn.clicked += CloseOptions;

		RefreshContinue();
		ShowMain();
	}

	void OnEnable()
	{
		RefreshContinue();
	}

	void RefreshContinue()
	{
		continueBtn.SetEnabled(SaveIO.Exists());
	}

	void ContinueGame()
	{
		GameBoot.LoadRequested = true;
		GameBoot.NextSceneName = gameSceneName;
		SceneManager.LoadScene(loadingSceneName);
	}

	void NewGame()
	{
		GameBoot.LoadRequested = false;
		GameBoot.NextSceneName = gameSceneName;

		var path = SaveIO.GetPath();
		if (System.IO.File.Exists(path))
			System.IO.File.Delete(path);

		SceneManager.LoadScene(loadingSceneName);
	}

	void OpenOptions()
	{
		mainPanel.style.display = DisplayStyle.None;
		optionsPanel.style.display = DisplayStyle.Flex;
	}

	void CloseOptions()
	{
		ShowMain();
	}

	void ShowMain()
	{
		mainPanel.style.display = DisplayStyle.Flex;
		optionsPanel.style.display = DisplayStyle.None;
	}

	void Quit()
	{
		Application.Quit();
	}
}

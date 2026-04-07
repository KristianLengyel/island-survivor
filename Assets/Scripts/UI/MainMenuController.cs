using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
	[SerializeField] private string gameSceneName = "Game";
	[SerializeField] private string loadingSceneName = "Loading";

	private static readonly string[] SizeNames = { "Small", "Medium", "Large", "Huge" };

	Button playBtn;
	Button optionsBtn;
	Button quitBtn;
	Button backFromWorldSelectBtn;
	Button backFromOptionsBtn;
	Button createWorldBtn;
	Button cancelNewWorldBtn;
	Button sizeSmallBtn;
	Button sizeMediumBtn;
	Button sizeLargeBtn;
	Button sizeHugeBtn;
	Button confirmDeleteBtn;
	Button cancelDeleteBtn;

	TextField worldNameField;
	Label confirmDeleteMsg;
	VisualElement slotList;

	VisualElement mainPanel;
	VisualElement worldSelectPanel;
	VisualElement newWorldPanel;
	VisualElement optionsPanel;
	VisualElement confirmDeletePanel;

	Button[] _sizeBtns;
	int _pendingSlot;
	int _selectedSize = 1;
	int _pendingDeleteSlot;

	void Awake()
	{
		var root = GetComponent<UIDocument>().rootVisualElement;

		playBtn = root.Q<Button>("playBtn");
		optionsBtn = root.Q<Button>("optionsBtn");
		quitBtn = root.Q<Button>("quitBtn");
		backFromWorldSelectBtn = root.Q<Button>("backFromWorldSelectBtn");
		backFromOptionsBtn = root.Q<Button>("backFromOptionsBtn");
		createWorldBtn = root.Q<Button>("createWorldBtn");
		cancelNewWorldBtn = root.Q<Button>("cancelNewWorldBtn");
		sizeSmallBtn = root.Q<Button>("sizeSmallBtn");
		sizeMediumBtn = root.Q<Button>("sizeMediumBtn");
		sizeLargeBtn = root.Q<Button>("sizeLargeBtn");
		sizeHugeBtn = root.Q<Button>("sizeHugeBtn");
		confirmDeleteBtn = root.Q<Button>("confirmDeleteBtn");
		cancelDeleteBtn = root.Q<Button>("cancelDeleteBtn");

		worldNameField = root.Q<TextField>("worldNameField");
		confirmDeleteMsg = root.Q<Label>("confirmDeleteMsg");
		slotList = root.Q<VisualElement>("slotList");

		mainPanel = root.Q<VisualElement>("mainPanel");
		worldSelectPanel = root.Q<VisualElement>("worldSelectPanel");
		newWorldPanel = root.Q<VisualElement>("newWorldPanel");
		optionsPanel = root.Q<VisualElement>("optionsPanel");
		confirmDeletePanel = root.Q<VisualElement>("confirmDeletePanel");

		_sizeBtns = new[] { sizeSmallBtn, sizeMediumBtn, sizeLargeBtn, sizeHugeBtn };

		playBtn.clicked += OpenWorldSelect;
		optionsBtn.clicked += OpenOptions;
		quitBtn.clicked += Quit;
		backFromWorldSelectBtn.clicked += ShowMain;
		backFromOptionsBtn.clicked += ShowMain;
		createWorldBtn.clicked += CreateWorld;
		cancelNewWorldBtn.clicked += OpenWorldSelect;
		confirmDeleteBtn.clicked += ExecuteDelete;
		cancelDeleteBtn.clicked += OpenWorldSelect;

		sizeSmallBtn.clicked += () => SetSize(0);
		sizeMediumBtn.clicked += () => SetSize(1);
		sizeLargeBtn.clicked += () => SetSize(2);
		sizeHugeBtn.clicked += () => SetSize(3);

		ShowMain();
	}

	void ShowMain()
	{
		mainPanel.style.display = DisplayStyle.Flex;
		worldSelectPanel.style.display = DisplayStyle.None;
		newWorldPanel.style.display = DisplayStyle.None;
		optionsPanel.style.display = DisplayStyle.None;
		confirmDeletePanel.style.display = DisplayStyle.None;
	}

	void OpenWorldSelect()
	{
		BuildSlotCards();
		mainPanel.style.display = DisplayStyle.None;
		worldSelectPanel.style.display = DisplayStyle.Flex;
		newWorldPanel.style.display = DisplayStyle.None;
		optionsPanel.style.display = DisplayStyle.None;
		confirmDeletePanel.style.display = DisplayStyle.None;
	}

	void OpenOptions()
	{
		mainPanel.style.display = DisplayStyle.None;
		worldSelectPanel.style.display = DisplayStyle.None;
		newWorldPanel.style.display = DisplayStyle.None;
		optionsPanel.style.display = DisplayStyle.Flex;
		confirmDeletePanel.style.display = DisplayStyle.None;
	}

	void OpenNewWorld(int slot)
	{
		_pendingSlot = slot;
		worldNameField.value = $"World {slot + 1}";
		SetSize(1);
		worldSelectPanel.style.display = DisplayStyle.None;
		newWorldPanel.style.display = DisplayStyle.Flex;
		confirmDeletePanel.style.display = DisplayStyle.None;
	}

	void OpenConfirmDelete(int slot)
	{
		_pendingDeleteSlot = slot;
		var meta = SaveIO.GetAllSlotMeta();
		confirmDeleteMsg.text = meta[slot].worldName;
		worldSelectPanel.style.display = DisplayStyle.None;
		confirmDeletePanel.style.display = DisplayStyle.Flex;
	}

	void ExecuteDelete()
	{
		SaveIO.Delete(_pendingDeleteSlot);
		OpenWorldSelect();
	}

	void BuildSlotCards()
	{
		slotList.Clear();
		var meta = SaveIO.GetAllSlotMeta();
		for (int i = 0; i < meta.Length; i++)
		{
			int slot = i;
			var card = new VisualElement();
			card.AddToClassList("slotCard");

			if (meta[i].exists)
			{
				var nameLabel = new Label(meta[i].worldName);
				nameLabel.AddToClassList("slotCardName");
				int preset = Mathf.Clamp(meta[i].mapSizePreset, 0, 3);
				var infoLabel = new Label($"Day {meta[i].dayCount} \u2022 {SizeNames[preset]}");
				infoLabel.AddToClassList("slotCardInfo");

				var deleteBtn = new Button();
				deleteBtn.AddToClassList("slotCardDeleteBtn");
				deleteBtn.text = "\u00d7";
				deleteBtn.RegisterCallback<ClickEvent>(evt =>
				{
					evt.StopPropagation();
					OpenConfirmDelete(slot);
				});

				card.Add(nameLabel);
				card.Add(infoLabel);
				card.Add(deleteBtn);
				card.RegisterCallback<ClickEvent>(_ => ContinueWorld(slot));
			}
			else
			{
				var emptyLabel = new Label("Empty Slot");
				emptyLabel.AddToClassList("slotCardEmpty");
				card.Add(emptyLabel);
				card.RegisterCallback<ClickEvent>(_ => OpenNewWorld(slot));
			}

			slotList.Add(card);
		}
	}

	void SetSize(int size)
	{
		_selectedSize = size;
		for (int i = 0; i < _sizeBtns.Length; i++)
		{
			if (i == size) _sizeBtns[i].AddToClassList("sizeSelected");
			else _sizeBtns[i].RemoveFromClassList("sizeSelected");
		}
	}

	void ContinueWorld(int slot)
	{
		GameBoot.SlotIndex = slot;
		GameBoot.LoadRequested = true;
		GameBoot.NextSceneName = gameSceneName;
		SceneManager.LoadScene(loadingSceneName);
	}

	void CreateWorld()
	{
		var name = worldNameField.value.Trim();
		if (string.IsNullOrEmpty(name))
			name = $"World {_pendingSlot + 1}";

		SaveIO.Delete(_pendingSlot);

		GameBoot.SlotIndex = _pendingSlot;
		GameBoot.LoadRequested = false;
		GameBoot.SizePreset = _selectedSize;
		GameBoot.WorldName = name;
		GameBoot.NextSceneName = gameSceneName;
		SceneManager.LoadScene(loadingSceneName);
	}

	void Quit()
	{
		Application.Quit();
	}
}

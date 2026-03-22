using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class StatBarsController : MonoBehaviour
{
	private const float TrackWidth = 72f;

	[SerializeField] private PlayerStats playerStats;

	private UIDocument _doc;
	private VisualElement _hungerFill;
	private VisualElement _thirstFill;

	private void Awake()
	{
		_doc = GetComponent<UIDocument>();
	}

	private void OnEnable()
	{
		CacheUi();
	}

	private void CacheUi()
	{
		if (_doc == null) _doc = GetComponent<UIDocument>();
		if (_doc == null) return;

		var root = _doc.rootVisualElement;
		if (root == null) return;

		_hungerFill = root.Q<VisualElement>("hunger-fill");
		_thirstFill = root.Q<VisualElement>("thirst-fill");
	}

	private void Update()
	{
		if (playerStats == null || _hungerFill == null || _thirstFill == null) return;

		_hungerFill.style.width = TrackWidth * playerStats.GetHungerPercentage();
		_thirstFill.style.width = TrackWidth * playerStats.GetThirstPercentage();
	}
}

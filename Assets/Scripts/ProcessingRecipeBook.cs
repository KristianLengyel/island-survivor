using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable object/Processing Recipe Book")]
public class ProcessingRecipeBook : ScriptableObject
{
	[SerializeField] private bool autoLoadFromResources = true;
	[SerializeField] private string resourcesFolder = "ProcessingRecipes";

	[SerializeField] private List<ProcessingRecipe> recipes = new();

	private Dictionary<ProcessingRecipe.ProcessorType, List<ProcessingRecipe>> byProcessor;

	public IReadOnlyList<ProcessingRecipe> Recipes => recipes;

	public IEnumerable<ProcessingRecipe> GetForProcessor(ProcessingRecipe.ProcessorType type)
	{
		EnsureLoaded();
		return byProcessor.TryGetValue(type, out var list) ? list : Enumerable.Empty<ProcessingRecipe>();
	}

	public ProcessingRecipe FindMatch(ProcessingRecipe.ProcessorType type, IEnumerable<Item> providedItems)
	{
		EnsureLoaded();

		if (!byProcessor.TryGetValue(type, out var list) || list.Count == 0)
			return null;

		var provided = providedItems
			.Where(i => i != null)
			.GroupBy(i => i.name)
			.ToDictionary(g => g.Key, g => g.Count());

		foreach (var recipe in list)
		{
			if (recipe == null || recipe.inputs == null || recipe.output == null) continue;

			bool match = recipe.inputs.All(req =>
				req != null &&
				req.item != null &&
				req.amount > 0 &&
				provided.TryGetValue(req.item.name, out int count) &&
				count >= req.amount
			);

			if (match)
				return recipe;
		}

		return null;
	}

	private void EnsureLoaded()
	{
		if (byProcessor != null) return;

		if (autoLoadFromResources)
		{
			var loaded = Resources.LoadAll<ProcessingRecipe>(resourcesFolder);
			recipes = loaded != null ? loaded.Distinct().ToList() : new List<ProcessingRecipe>();
		}

		byProcessor = recipes
			.Where(r => r != null)
			.GroupBy(r => r.processor)
			.ToDictionary(g => g.Key, g => g.ToList());
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		byProcessor = null;
		if (autoLoadFromResources)
		{
			var loaded = Resources.LoadAll<ProcessingRecipe>(resourcesFolder);
			recipes = loaded != null ? loaded.Distinct().ToList() : new List<ProcessingRecipe>();
		}
	}
#endif
}

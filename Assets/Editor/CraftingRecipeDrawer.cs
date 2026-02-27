using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingRecipe))]
public class CraftingRecipeDrawer : Editor
{
	public override void OnInspectorGUI()
	{
		CraftingRecipe recipe = (CraftingRecipe)target;

		// Draw the default Inspector fields
		DrawDefaultInspector();

		// Set the icon for the ScriptableObject in the Project window if a result item is assigned
		if (recipe.result != null && recipe.result.image != null)
		{
			// Get the sprite texture from the result item's image
			Texture2D iconTexture = AssetPreview.GetAssetPreview(recipe.result.image);
			if (iconTexture != null)
			{
				// Use EditorGUIUtility.SetIconForObject for Unity 2019+
				EditorGUIUtility.SetIconForObject(target, iconTexture);

				// Optionally, save the changes (though Unity often handles this automatically)
				AssetDatabase.SaveAssets();
			}
			else
			{
				Debug.LogWarning($"Could not generate preview for sprite in {recipe.name}. Check sprite settings for {recipe.result.name}:\n" +
					"- Ensure Texture Type is 'Sprite (2D and UI)'\n" +
					"- Set Sprite Mode to 'Single'\n" +
					"- Uncheck 'Generate Mip Maps'\n" +
					"- Set Max Size to a reasonable value (e.g., 128)\n" +
					"- Use Truecolor or RGBA 32 bit format.");

				// Fallback: Use a default icon if no preview is available
				Texture2D defaultIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DefaultIcon.png");
				if (defaultIcon != null)
				{
					EditorGUIUtility.SetIconForObject(target, defaultIcon);
				}
			}
		}
		else
		{
			// If no result or no sprite is assigned, use a default icon
			Texture2D defaultIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DefaultIcon.png");
			if (defaultIcon != null)
			{
				EditorGUIUtility.SetIconForObject(target, defaultIcon);
				Debug.LogWarning($"No result or sprite assigned for {recipe.name}. Using default icon.");
			}
		}
	}
}
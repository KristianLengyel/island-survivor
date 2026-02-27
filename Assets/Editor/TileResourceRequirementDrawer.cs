using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileResourceRequirement))]
public class TileResourceRequirementDrawer : Editor
{
	public override void OnInspectorGUI()
	{
		TileResourceRequirement requirement = (TileResourceRequirement)target;

		// Draw the default Inspector fields
		DrawDefaultInspector();

		// Try to use the first resource's item image as the icon (if any)
		Texture2D iconTexture = null;
		if (requirement.resourceRequirements != null && requirement.resourceRequirements.Count > 0)
		{
			foreach (var resourceReq in requirement.resourceRequirements)
			{
				if (resourceReq.resource != null && resourceReq.resource.image != null)
				{
					iconTexture = AssetPreview.GetAssetPreview(resourceReq.resource.image);
					if (iconTexture != null) break; // Use the first valid resource image
				}
			}
		}

		// Set the icon if a valid texture was found
		if (iconTexture != null)
		{
			EditorGUIUtility.SetIconForObject(target, iconTexture);
			AssetDatabase.SaveAssets();
		}
		else
		{
			Debug.LogWarning($"Could not generate preview for sprite in {requirement.name}. Check sprite settings for resources:\n" +
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
}
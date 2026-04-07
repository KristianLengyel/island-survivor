using UnityEngine;

public class WaterPipeIndicator : MonoBehaviour
{
	[SerializeField] private Color noWaterColor = Color.white;
	[SerializeField] private Color saltWaterColor = new Color(1f, 0.55f, 0.1f, 1f);
	[SerializeField] private Color freshWaterColor = new Color(0.2f, 0.65f, 1f, 1f);

	private void OnEnable()
	{
		if (PipeNetwork.Instance != null)
		{
			PipeNetwork.Instance.OnColorsDirty += HandleColorsDirty;
			HandleColorsDirty();
		}
	}

	private void OnDisable()
	{
		if (PipeNetwork.Instance != null)
			PipeNetwork.Instance.OnColorsDirty -= HandleColorsDirty;
	}

	private void HandleColorsDirty()
	{
		PipeNetwork.Instance.ColorizePipes(noWaterColor, saltWaterColor, freshWaterColor);
	}
}

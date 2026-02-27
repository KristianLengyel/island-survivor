using UnityEngine;

public class MapInputHandler : MonoBehaviour
{
	private void Update()
	{
		if (!GameInput.IsReady) return;

		if (GameInput.MapDown)
		{
			MenuCoordinator.Instance.Toggle("Map");
		}

		if (GameInput.CancelDown && MenuCoordinator.Instance.IsOpen("Map"))
		{
			MenuCoordinator.Instance.Close("Map");
		}
	}
}

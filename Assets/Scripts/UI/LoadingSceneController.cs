using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneController : MonoBehaviour
{
	IEnumerator Start()
	{
		if (string.IsNullOrEmpty(GameBoot.NextSceneName))
			GameBoot.NextSceneName = "Game";

		yield return null;

		var op = SceneManager.LoadSceneAsync(GameBoot.NextSceneName);
		op.allowSceneActivation = false;

		while (op.progress < 0.9f)
			yield return null;

		yield return null;

		op.allowSceneActivation = true;
	}
}

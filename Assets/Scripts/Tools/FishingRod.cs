using System.Collections;
using UnityEngine;

public class FishingRod : MonoBehaviour, IPlayerTool
{
	[SerializeField] private Item fishingRodItem;

	public GameObject rodInAirPrefab;
	public GameObject rodInWaterPrefab;
	[SerializeField] private LineRenderer lineRenderer;
	public Transform player;
	[SerializeField] private float maxRange = 5f;
	[SerializeField] private float rodSpeed = 2f;
	[SerializeField] private float destroyDistance = 1f;
	[SerializeField] private float collectDistance = 1f;
	[SerializeField] private float maxHoldTime = 2f;
	[SerializeField] private float minCatchTime = 4f;
	[SerializeField] private float maxCatchTime = 15f;

	private const float LineWidth = 0.1f;
	private const int LinePositionCount = 2;

	private enum RodState
	{
		Idle,
		Holding,
		Thrown,
		Pulling,
		CaughtFish
	}

	private RodState currentState = RodState.Idle;
	private float holdTime = 0f;
	private Vector3 throwDirection;
	private Vector3 lastPlayerPosition;

	public GameObject throwIndicator;
	private GameObject instantiatedIndicator;
	private GameObject instantiatedRodInAir;
	private GameObject instantiatedRodInWater;

	private PlayerTileDetector playerTileDetector;
	private PlayerController playerController;

	[SerializeField] private Item fishItem;
	private Coroutine fishCatchCoroutine;

	private bool isActive;

	private void Start()
	{
		InitializeLineRenderer();
		InitializePlayerComponents();
		enabled = false;
	}

	public bool CanHandle(Item selectedItem)
	{
		return selectedItem != null && selectedItem == fishingRodItem;
	}

	public void OnSelected(Item selectedItem)
	{
		isActive = true;
		enabled = true;
	}

	public void OnDeselected()
	{
		isActive = false;
		ResetRodState();
		enabled = false;
	}

	public void Tick()
	{
		if (!isActive) return;

		switch (currentState)
		{
			case RodState.Idle:
				if (CanThrowRod()) HandleRodThrowInput();
				break;
			case RodState.Holding:
				HandleHoldingInput();
				break;
			case RodState.Thrown:
			case RodState.Pulling:
			case RodState.CaughtFish:
				if (!IsPullingDisabled()) HandleRodPullInput();
				break;
		}

		UpdateLineRendererIfEnabled();
		UpdatePullingState();
	}

	public void FixedTick() { }

	private void InitializeLineRenderer()
	{
		if (lineRenderer == null)
		{
			lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.positionCount = LinePositionCount;
			lineRenderer.startWidth = LineWidth;
			lineRenderer.endWidth = LineWidth;
			lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		}
		lineRenderer.enabled = false;
	}

	private void InitializePlayerComponents()
	{
		if (player == null)
		{
			enabled = false;
			return;
		}

		playerTileDetector = player.GetComponent<PlayerTileDetector>();
		playerController = player.GetComponent<PlayerController>();

		if (playerTileDetector == null || playerController == null)
		{
			enabled = false;
		}
	}

	private bool CanThrowRod()
	{
		return playerTileDetector != null && (!playerTileDetector.IsInWater() || playerTileDetector.IsInShallowWater());
	}

	private bool IsPullingDisabled()
	{
		return playerTileDetector != null && playerTileDetector.IsInWater() && !playerTileDetector.IsInShallowWater();
	}

	private void HandleRodThrowInput()
	{
		if (GameInput.LmbDown) { StartHolding(); }
	}

	private void StartHolding()
	{
		currentState = RodState.Holding;
		holdTime = 0f;
		instantiatedIndicator = Instantiate(throwIndicator, player.position, Quaternion.identity);
	}

	private void HandleHoldingInput()
	{
		if (GameInput.LmbHeld) { UpdateHolding(); }
		else if (GameInput.LmbUp) { ThrowRod(); }
	}

	private void UpdateHolding()
	{
		holdTime += Time.deltaTime;
		holdTime = Mathf.Clamp(holdTime, 0, maxHoldTime);

		throwDirection = (GetMouseWorldPosition() - player.position).normalized;
		float indicatorDistance = Mathf.Lerp(0, maxRange, holdTime / maxHoldTime);
		if (instantiatedIndicator != null)
		{
			instantiatedIndicator.transform.position = player.position + throwDirection * indicatorDistance;
		}
	}

	private void ThrowRod()
	{
		currentState = RodState.Thrown;
		float throwDistance = Mathf.Lerp(0, maxRange, holdTime / maxHoldTime);
		Vector3 throwPosition = player.position + throwDirection * throwDistance;

		DestroyPreviousRod();
		DestroyIndicatorIfExists();

		instantiatedRodInAir = Instantiate(rodInAirPrefab, player.position, Quaternion.identity);
		lineRenderer.enabled = true;
		StartCoroutine(AnimateRodThrow(instantiatedRodInAir, throwPosition));

		playerController.SetHookTransform(instantiatedRodInAir.transform, maxRange, this);
		AudioManager.instance?.PlaySound("HookThrow");
	}

	private void HandleRodPullInput()
	{
		if (instantiatedRodInWater == null) return;

		if (GameInput.LmbHeld) { PullRod(); }
		else if (GameInput.RmbDown) { CancelRod(); }
	}

	private void PullRod()
	{
		currentState = currentState == RodState.CaughtFish ? RodState.CaughtFish : RodState.Pulling;
		Vector3 direction = (player.position - instantiatedRodInWater.transform.position).normalized;
		instantiatedRodInWater.transform.position += direction * rodSpeed * Time.deltaTime;

		lineRenderer.SetPosition(0, instantiatedRodInWater.transform.position);

		float distanceToPlayer = Vector3.Distance(instantiatedRodInWater.transform.position, player.position);
		if (distanceToPlayer <= destroyDistance)
		{
			if (currentState == RodState.CaughtFish)
			{
				CollectFish();
			}
			ResetRodState();
		}
		else if (distanceToPlayer <= collectDistance && currentState == RodState.CaughtFish)
		{
			CollectFish();
			currentState = RodState.Pulling;
		}
	}

	private void CancelRod()
	{
		ResetRodState();
	}

	private void ResetRodState()
	{
		if (fishCatchCoroutine != null)
		{
			StopCoroutine(fishCatchCoroutine);
			fishCatchCoroutine = null;
		}
		DestroyPreviousRod();
		lineRenderer.enabled = false;
		currentState = RodState.Idle;
		playerController?.ClearHookTransform(this);
		DestroyIndicatorIfExists();
	}

	private void DestroyPreviousRod()
	{
		if (instantiatedRodInAir != null) Destroy(instantiatedRodInAir);
		if (instantiatedRodInWater != null) Destroy(instantiatedRodInWater);
		instantiatedRodInAir = null;
		instantiatedRodInWater = null;
	}

	private void DestroyIndicatorIfExists()
	{
		if (instantiatedIndicator != null)
		{
			Destroy(instantiatedIndicator);
			instantiatedIndicator = null;
			holdTime = 0f;
		}
	}

	private Vector3 GetMouseWorldPosition()
	{
		if (Camera.main == null) return Vector3.zero;
		Vector3 mousePosition = GameInput.MouseScreen;
		mousePosition.z = -Camera.main.transform.position.z;
		return Camera.main.ScreenToWorldPoint(mousePosition);
	}

	private void UpdateLineRendererIfEnabled()
	{
		if (lineRenderer.enabled && player.position != lastPlayerPosition)
		{
			lineRenderer.SetPosition(1, player.position);
			lastPlayerPosition = player.position;
		}
	}

	private void UpdatePullingState()
	{
		if (IsPullingDisabled() && currentState == RodState.Thrown)
		{
			DestroyIndicatorIfExists();
		}
	}

	private IEnumerator AnimateRodThrow(GameObject rodInAir, Vector3 targetPosition)
	{
		float elapsedTime = 0f;
		Vector3 startPosition = rodInAir.transform.position;

		while (elapsedTime < maxHoldTime)
		{
			if (rodInAir == null) yield break;

			rodInAir.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / maxHoldTime);
			lineRenderer.SetPosition(0, rodInAir.transform.position);
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		if (rodInAir != null)
		{
			rodInAir.transform.position = targetPosition;
			ReplaceRodWithWaterVersion(targetPosition);
		}
	}

	private void ReplaceRodWithWaterVersion(Vector3 position)
	{
		DestroyPreviousRod();
		instantiatedRodInWater = Instantiate(rodInWaterPrefab, position, Quaternion.identity);
		lineRenderer.SetPosition(0, instantiatedRodInWater.transform.position);
		playerController?.SetHookTransform(instantiatedRodInWater.transform, maxRange, this);
		AudioManager.instance?.PlaySound("HookWaterSplash");
		fishCatchCoroutine = StartCoroutine(WaitForFishCatch());
	}

	private IEnumerator WaitForFishCatch()
	{
		float waitTime = Random.Range(minCatchTime, maxCatchTime);
		yield return new WaitForSeconds(waitTime);

		if (instantiatedRodInWater != null)
		{
			currentState = RodState.CaughtFish;
			AudioManager.instance?.PlaySound("HookWaterSplash");
		}
	}

	private void CollectFish()
	{
		var inventoryManager = GameManager.Instance?.InventoryManager;
		if (inventoryManager == null) return;
		if (fishItem == null) return;
		inventoryManager.AddItem(fishItem, 1);
	}
}

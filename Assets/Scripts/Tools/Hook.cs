using System.Collections;
using UnityEngine;

public class Hook : MonoBehaviour, IPlayerTool
{
	[SerializeField] private Item hookItem;

	public GameObject hookInAirPrefab;
	public GameObject hookInWaterPrefab;
	[SerializeField] private LineRenderer lineRenderer;
	public Transform player;
	public float maxRange = 7f;
	public float hookSpeed = 2f;
	public float destroyDistance = 1f;
	public float collectDistance = 1f;
	public float maxHoldTime = 2f;

	public GameObject throwIndicator;

	private bool isHolding = false;
	private bool isHookThrown = false;
	private bool isPullingDisabled = false;
	private float holdTime = 0f;
	private Vector3 throwDirection;

	private GameObject instantiatedIndicator;
	private GameObject instantiatedHookInAir;
	private GameObject instantiatedHookInWater;

	private PlayerTileDetector playerTileDetector;
	private PlayerController playerController;

	private bool isActive;

	private void Start()
	{
		if (lineRenderer == null)
		{
			lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.positionCount = 2;
			lineRenderer.startWidth = 0.1f;
			lineRenderer.endWidth = 0.1f;
			lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		}

		lineRenderer.enabled = false;

		if (player != null)
		{
			playerTileDetector = player.GetComponent<PlayerTileDetector>();
			playerController = player.GetComponent<PlayerController>();
		}

		enabled = false;
	}

	public bool CanHandle(Item selectedItem)
	{
		return selectedItem != null && selectedItem == hookItem;
	}

	public void OnSelected(Item selectedItem)
	{
		isActive = true;
		enabled = true;
	}

	public void OnDeselected()
	{
		isActive = false;
		ResetState();
		enabled = false;
	}

	public void Tick()
	{
		if (!isActive) return;

		if (playerTileDetector != null && playerTileDetector.IsInWater() && !playerTileDetector.IsInShallowWater())
		{
			if (isHookThrown && !isPullingDisabled) isPullingDisabled = true;
			if (instantiatedIndicator != null) Destroy(instantiatedIndicator);
		}
		else
		{
			isPullingDisabled = false;
		}

		if (!isHookThrown && playerTileDetector != null && (!playerTileDetector.IsInWater() || playerTileDetector.IsInShallowWater()))
		{
			if (GameInput.LmbDown)
			{
				isHolding = true;
				holdTime = 0f;
				instantiatedIndicator = Instantiate(throwIndicator, player.position, Quaternion.identity);
			}

			if (GameInput.LmbHeld && isHolding)
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

			if (GameInput.LmbUp && isHolding)
			{
				isHolding = false;
				isHookThrown = true;

				float throwDistance = Mathf.Lerp(0, maxRange, holdTime / maxHoldTime);
				Vector3 throwPosition = player.position + throwDirection * throwDistance;

				DestroyPreviousHook();
				if (instantiatedIndicator != null) Destroy(instantiatedIndicator);

				instantiatedHookInAir = Instantiate(hookInAirPrefab, player.position, Quaternion.identity);
				lineRenderer.enabled = true;
				StartCoroutine(AnimateHookThrow(instantiatedHookInAir, throwPosition));

				playerController.SetHookTransform(instantiatedHookInAir.transform, maxRange, this);
				AudioManager.instance.PlaySound("HookThrow");
			}
		}
		else if (!isPullingDisabled)
		{
			if (GameInput.LmbHeld && instantiatedHookInWater != null)
			{
				Vector3 direction = (player.position - instantiatedHookInWater.transform.position).normalized;
				instantiatedHookInWater.transform.position += direction * hookSpeed * Time.deltaTime;

				lineRenderer.SetPosition(0, instantiatedHookInWater.transform.position);

				float distanceToPlayer = Vector3.Distance(instantiatedHookInWater.transform.position, player.position);
				if (distanceToPlayer <= destroyDistance)
				{
					Destroy(instantiatedHookInWater);
					lineRenderer.enabled = false;
					isHookThrown = false;
					playerController.ClearHookTransform(this);
				}

				if (distanceToPlayer <= collectDistance)
				{
					HookCollision hookCollision = instantiatedHookInWater.GetComponent<HookCollision>();
					if (hookCollision != null)
					{
						hookCollision.CollectItems();
					}
				}
			}

			if (GameInput.RmbDown && instantiatedHookInWater != null)
			{
				HookCollision hookCollision = instantiatedHookInWater.GetComponent<HookCollision>();
				if (hookCollision != null && hookCollision.HasItems())
				{
					UpdateLine();
					return;
				}

				DestroyPreviousHook();
				lineRenderer.enabled = false;
				isHookThrown = false;
				playerController.ClearHookTransform(this);
			}
		}

		UpdateLine();
	}

	public void FixedTick() { }

	private void UpdateLine()
	{
		if (lineRenderer.enabled && player != null)
		{
			lineRenderer.SetPosition(1, player.position);
		}
	}

	private void ResetState()
	{
		DestroyPreviousHook();
		if (instantiatedIndicator != null) Destroy(instantiatedIndicator);
		instantiatedIndicator = null;

		lineRenderer.enabled = false;
		isHolding = false;
		isHookThrown = false;
		isPullingDisabled = false;
		holdTime = 0f;

		if (playerController != null) playerController.ClearHookTransform(this);
	}

	private void DestroyPreviousHook()
	{
		if (instantiatedHookInAir != null) Destroy(instantiatedHookInAir);
		if (instantiatedHookInWater != null) Destroy(instantiatedHookInWater);
		instantiatedHookInAir = null;
		instantiatedHookInWater = null;
	}

	private Vector3 GetMouseWorldPosition()
	{
		Vector3 mousePosition = GameInput.MouseScreen;
		mousePosition.z = -Camera.main.transform.position.z;
		return Camera.main.ScreenToWorldPoint(mousePosition);
	}

	private IEnumerator AnimateHookThrow(GameObject hookInAir, Vector3 targetPosition)
	{
		float elapsedTime = 0f;
		Vector3 startPosition = hookInAir.transform.position;

		while (elapsedTime < maxHoldTime)
		{
			if (hookInAir == null) yield break;

			hookInAir.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / maxHoldTime);
			lineRenderer.SetPosition(0, hookInAir.transform.position);
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		if (hookInAir != null)
		{
			hookInAir.transform.position = targetPosition;
			lineRenderer.SetPosition(0, hookInAir.transform.position);
			ReplaceHookWithWaterVersion(hookInAir.transform.position);
		}
	}

	private void ReplaceHookWithWaterVersion(Vector3 position)
	{
		if (instantiatedHookInAir != null) Destroy(instantiatedHookInAir);

		instantiatedHookInWater = Instantiate(hookInWaterPrefab, position, Quaternion.identity);
		lineRenderer.SetPosition(0, instantiatedHookInWater.transform.position);

		InitializeHook(instantiatedHookInWater);

		playerController.SetHookTransform(instantiatedHookInWater.transform, maxRange, this);
		AudioManager.instance.PlaySound("HookWaterSplash");
	}

	private void InitializeHook(GameObject hook)
	{
		HookCollision hookCollision = hook.GetComponent<HookCollision>();
		if (hookCollision != null)
		{
			hookCollision.enabled = true;
		}

		Animation animation = hook.GetComponent<Animation>();
		if (animation != null && animation.clip == null)
		{
			animation.clip = animation.GetClip("HookThrownIntoWater");
		}
	}
}

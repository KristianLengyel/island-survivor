using UnityEngine;
using UnityEngine.Rendering;

public class FakeShadow2D : MonoBehaviour
{
	[SerializeField] private SpriteRenderer sourceRenderer;
	[SerializeField] private Transform shadowPivot;
	[SerializeField] private Sprite customShadowSprite;
	[SerializeField] private bool autoDetectPivot = true;
	[SerializeField] private Vector2 pivotOffset;
	[SerializeField] private int sortingOrderOffset = 3;

	private GameObject _shadowGO;
	private MeshFilter _meshFilter;
	private MeshRenderer _meshRenderer;
	private Mesh _mesh;
	private Material _shadowMaterial;
	private FakeShadow2DLight _light;
	private Sprite _lastSprite;
	private Vector3[] _verts;

	private void Awake()
	{
		if (sourceRenderer == null)
			sourceRenderer = GetComponent<SpriteRenderer>();

		_light = FindFirstObjectByType<FakeShadow2DLight>();

		_shadowGO = new GameObject("_Shadow");

		_meshFilter = _shadowGO.AddComponent<MeshFilter>();
		_meshRenderer = _shadowGO.AddComponent<MeshRenderer>();
		_meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		_meshRenderer.receiveShadows = false;

		_mesh = new Mesh { name = "FakeShadowMesh" };
		_mesh.MarkDynamic();
		_meshFilter.mesh = _mesh;

		var shadowShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
			?? Shader.Find("Sprites/Default");
		_shadowMaterial = new Material(shadowShader);
		_shadowMaterial.color = Color.clear;
		_meshRenderer.material = _shadowMaterial;

		if (sourceRenderer != null)
		{
			_meshRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
			_meshRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
		}
	}

	private void OnEnable()
	{
		if (_meshRenderer != null) _meshRenderer.enabled = true;
	}

	private void OnDisable()
	{
		if (_meshRenderer != null) _meshRenderer.enabled = false;
	}

	private void OnDestroy()
	{
		if (_shadowGO != null) Destroy(_shadowGO);
		if (_mesh != null) Destroy(_mesh);
		if (_shadowMaterial != null) Destroy(_shadowMaterial);
	}

	private void LateUpdate()
	{
		if (sourceRenderer == null || sourceRenderer.sprite == null)
		{
			_meshRenderer.enabled = false;
			return;
		}

		if (_light == null)
		{
			_light = FindFirstObjectByType<FakeShadow2DLight>();
			if (_light == null) { _meshRenderer.enabled = false; return; }
		}

		if (!_light.IsVisible) { _meshRenderer.enabled = false; return; }

		_meshRenderer.enabled = sourceRenderer.enabled;
		_meshRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
		_meshRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;

		Transform src = sourceRenderer.transform;
		_shadowGO.transform.SetPositionAndRotation(src.position, src.rotation);
		Vector3 lossy = src.lossyScale;
		_shadowGO.transform.localScale = new Vector3(lossy.x, lossy.y, 1f);

		Sprite spr = customShadowSprite != null ? customShadowSprite : sourceRenderer.sprite;

		if (spr != _lastSprite)
		{
			RebuildBaseMesh(spr);
			_lastSprite = spr;
		}

		ApplyShear(spr);

		_shadowMaterial.mainTexture = spr.texture;
		_shadowMaterial.color = new Color(0f, 0f, 0f, _light.ShadowAlpha);
	}

	private void RebuildBaseMesh(Sprite spr)
	{
		Vector2[] sprVerts = spr.vertices;
		ushort[] sprTris = spr.triangles;

		if (_verts == null || _verts.Length != sprVerts.Length)
			_verts = new Vector3[sprVerts.Length];

		int[] tris = new int[sprTris.Length];
		for (int i = 0; i < sprTris.Length; i++)
			tris[i] = sprTris[i];

		_mesh.Clear();
		_mesh.vertices = _verts;
		_mesh.triangles = tris;
		_mesh.uv = spr.uv;
	}

	private void ApplyShear(Sprite spr)
	{
		Vector2[] sprVerts = spr.vertices;
		Vector2 dir = _light.ShadowDirection;
		float length = _light.ShadowLength;
		float squish = _light.shadowSquish;

		float pivotY = shadowPivot != null
			? shadowPivot.localPosition.y
			: autoDetectPivot ? spr.bounds.min.y : pivotOffset.y;

		for (int i = 0; i < sprVerts.Length; i++)
		{
			float h = Mathf.Max(0f, sprVerts[i].y - pivotY);
			_verts[i] = new Vector3(
				sprVerts[i].x + dir.x * h * length,
				pivotY + h * squish + dir.y * h * length,
				0f
			);
		}

		_mesh.vertices = _verts;
		_mesh.RecalculateBounds();
	}
}

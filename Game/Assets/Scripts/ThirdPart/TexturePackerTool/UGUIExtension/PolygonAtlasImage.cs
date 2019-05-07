using UnityEngine;
using UnityEngine.UI;

public class PolygonAtlasImage : AtlasImage {
	[Range(3,40)]
	[SerializeField]private int edges = 6;
	[SerializeField]private float rotationOffset = 0.0f;

	protected PolygonAtlasImage()
	{
		useLegacyMeshGeneration = false;
	}

	protected override void OnPopulateMesh (VertexHelper vh)
	{
		vh.Clear ();
		if (edges < 3 || rectTransform.rect.width == 0.0f || rectTransform.rect.height == 0.0f) {
			return;
		}
		float width = rectTransform.rect.width;
		float height = rectTransform.rect.height;
		float pivotX = rectTransform.pivot.x;
		float pivotY = rectTransform.pivot.y;
		Vector3 offset = new Vector3 (width * (pivotX - 0.5f), height * (pivotY - 0.5f), 0.0f);
		float radius = Mathf.Max (width, height) * 0.5f;
		float multiplierX = width * 0.5f / radius;
		float multiplierY = height * 0.5f / radius;
		float edgesRotationOffset = edges % 2 == 0 ? 0.0f : 90.0f;
		float angleDelta = 360.0f / edges;

		Vector3[] vertices = new Vector3[edges];
		for (int i = 0; i < edges; i++) {
			float totalOffset = edgesRotationOffset + rotationOffset;
			float radians = DegreeToRadians (angleDelta * i + totalOffset);
			vertices [i] = radius * new Vector3 ((Mathf.Cos (radians)) * multiplierX, (Mathf.Sin (radians)) * multiplierY, 0.0f);
			vertices [i] = vertices [i] - offset;
			Vector2 uv = Vector2.zero;
			if (sprite != null) {
				uv.x = ((vertices [i].x / width + pivotX) * sprite.rect.width + sprite.rect.x) / sprite.texture.width;
				uv.y = ((vertices [i].y / height + pivotY) * sprite.rect.height + sprite.rect.y) / sprite.texture.height;
			}
			vh.AddVert (vertices [i], color, uv);
		}
		for (int i = 1; i < edges - 2; i++) {
			vh.AddTriangle (0, i + 1, i);
		}
		vh.AddTriangle (0, edges - 1, edges - 2);	
	}

	private float DegreeToRadians(float degree)
	{
		return degree * Mathf.PI / 180.0f;
	}
}
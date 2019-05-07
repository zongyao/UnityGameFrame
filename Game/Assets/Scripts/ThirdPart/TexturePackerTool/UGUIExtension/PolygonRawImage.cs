using UnityEngine.UI;
using UnityEngine;

public class PolygonRawImage : RawImage {
	[Range(3,40)]
	[SerializeField]private int edges = 6;
	[SerializeField]private float rotationOffset = 0.0f;

	protected PolygonRawImage()
	{
		useLegacyMeshGeneration = false;
	}

	protected override void OnPopulateMesh (VertexHelper vh)
	{
		vh.Clear ();
		if (edges < 3 || rectTransform.rect.width == 0 || rectTransform.rect.height == 0) {
			return;
		}
		float width = rectTransform.rect.width;
		float height = rectTransform.rect.height;
		float pivotX = rectTransform.pivot.x;
		float pivotY = rectTransform.pivot.y;
		float radius = Mathf.Max (width, height) * 0.5f;
		float multiplierX = width * 0.5f / radius;
		float multiplierY = height * 0.5f / radius;
		Vector3 offset = new Vector3 (width * (pivotX - 0.5f), height * (pivotY - 0.5f), 0);
		float edgesRotationOffset = edges % 2 == 0 ? 0.0f : 90.0f;
		float angleDelta = 360.0f / edges;

		Vector3[] vertices = new Vector3[edges];
		for (int i = 0; i < edges; i++) {
			float totalOffset = edgesRotationOffset + rotationOffset;
			float radians = DegreeToRadians (angleDelta * i + totalOffset);
			vertices [i] = radius * new Vector3 ((Mathf.Cos (radians)) * multiplierX, (Mathf.Sin (radians)) * multiplierY, 0.0f);
			vertices [i] = vertices [i] - offset;
			vh.AddVert (vertices [i], color, new Vector2 (vertices [i].x / width + pivotX, vertices [i].y / height + pivotY));
		}
		for (int i = 1; i < edges - 2; i++) {
			vh.AddTriangle (0, i, i + 1);
		}
		vh.AddTriangle (0, edges - 2, edges - 1);	
	}

	private float DegreeToRadians(float degree)
	{
		return degree * Mathf.PI / 180.0f;
	}
}
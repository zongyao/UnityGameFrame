using UnityEngine;
using UnityEngine.UI;

public class RoundCornerRawImage : RawImage {

	[Range(0, 40)]
	[SerializeField]private int segment = 9;
	[SerializeField]private float topLeftRadius = 10.0f;
	[SerializeField]private float topRightRadius = 10.0f;
	[SerializeField]private float bottomLeftRadius = 10.0f;
	[SerializeField]private float bottomRightRadius = 10.0f;
	protected RoundCornerRawImage()
	{
		useLegacyMeshGeneration = false;
	}

	protected override void OnPopulateMesh (VertexHelper vh)
	{
		vh.Clear ();
		if (rectTransform.rect.width == 0.0f || rectTransform.rect.height == 0.0f || segment < 0.0f) {
			return;
		}
		float width = rectTransform.rect.width;
		float height = rectTransform.rect.height;
		float maxRadius = Mathf.Min (width, height) * 0.5f;
		float pivotX = rectTransform.pivot.x;
		float pivotY = rectTransform.pivot.y;
		Vector3 offset = new Vector3 (width * pivotX, height * pivotY, 0.0f);
		topLeftRadius = topLeftRadius > maxRadius ? maxRadius : topLeftRadius;
		topRightRadius = topRightRadius > maxRadius ? maxRadius : topRightRadius;
		bottomLeftRadius = bottomLeftRadius > maxRadius ? maxRadius : bottomLeftRadius;
		bottomRightRadius = bottomRightRadius > maxRadius ? maxRadius : bottomRightRadius;

		Vector3[] vertices = new Vector3[12 + segment * 4];
		vertices [0] = new Vector3 (topLeftRadius, height - topLeftRadius, 0.0f) - offset;
		vertices [1] = new Vector3 (width - topRightRadius, height - topRightRadius, 0.0f) - offset;
		vertices [2] = new Vector3 (width - bottomRightRadius, bottomRightRadius, 0.0f) - offset;
		vertices [3] = new Vector3 (bottomLeftRadius, bottomLeftRadius, 0.0f) - offset;
		vertices [4] = new Vector3 (0.0f, height - topLeftRadius, 0.0f) - offset;
		vertices [5] = new Vector3 (topLeftRadius, height, 0.0f) - offset;
		vertices [6] = new Vector3 (width - topRightRadius, height, 0.0f) - offset;
		vertices [7] = new Vector3 (width, height - topRightRadius, 0.0f) - offset;
		vertices [8] = new Vector3 (width, bottomRightRadius, 0.0f) - offset;
		vertices [9] = new Vector3 (width - bottomRightRadius, 0.0f, 0.0f) - offset;
		vertices [10] = new Vector3 (bottomLeftRadius, 0.0f, 0.0f) - offset;
		vertices [11] = new Vector3 (0.0f, bottomLeftRadius, 0.0f) - offset;
		for (int i = 0; i < 12; i++) {
			vh.AddVert (vertices [i], color, new Vector2 (vertices [i].x / width + pivotX, vertices [i].y / height + pivotY));
		}
		vh.AddTriangle (0, 1, 2);
		vh.AddTriangle (0, 2, 3);
		vh.AddTriangle (0, 5, 6);
		vh.AddTriangle (0, 6, 1);
		vh.AddTriangle (2, 1, 7);
		vh.AddTriangle (2, 7, 8);
		vh.AddTriangle (2, 9, 10);
		vh.AddTriangle (2, 10, 3);
		vh.AddTriangle (0, 3, 11);
		vh.AddTriangle (0, 11, 4);

		if (segment == 0) {
			vh.AddTriangle (4, 5, 0);
			vh.AddTriangle (6, 7, 1);
			vh.AddTriangle (8, 9, 2);
			vh.AddTriangle (10, 11, 3);
		} else {
			float angleDelta = 90.0f / (segment + 1);
			for (int i = 4; i < 12; i += 2) {
				int centerIndex = (int)(i * 0.5f - 2);
				int startIndex = 12 + centerIndex * segment;
				int endIndex = startIndex + segment - 1;
				for (int j = startIndex; j <= endIndex; j++) {
					vertices [j] = Quaternion.Euler (0.0f, 0.0f, -angleDelta * (j - startIndex + 1)) * (vertices [i] - vertices [centerIndex]) + vertices [centerIndex];
					vh.AddVert (vertices [j], color, new Vector2 (vertices [j].x / width + pivotX, vertices [j].y / height + pivotY));
					vh.AddTriangle (j, j < endIndex ? j + 1 : i + 1, centerIndex);
				}
				vh.AddTriangle (i, startIndex, centerIndex);
				vh.AddTriangle (endIndex, i + 1, centerIndex);
			}
		} 
	}
}
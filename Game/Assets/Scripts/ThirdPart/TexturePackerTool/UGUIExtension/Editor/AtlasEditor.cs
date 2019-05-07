using UnityEditor;
using UnityEngine;

[CustomEditor (typeof(Atlas))]
public class AtlasEditor : Editor
{
	public override bool HasPreviewGUI ()
	{
		return true;
	}

	public override void OnPreviewGUI (Rect r, GUIStyle background)
	{
		base.OnPreviewGUI (r, background);
		Texture texture = (target as Atlas).Source;
		EditorGUI.DrawTextureTransparent (r, texture, ScaleMode.ScaleToFit, 0);
	}

	public override void OnInspectorGUI ()
	{
		using (EditorGUI.DisabledScope disableScope = new EditorGUI.DisabledScope (true)) {
			base.OnInspectorGUI ();
			Atlas atlas = target as Atlas;
			EditorGUILayout.LabelField ("Total Count", atlas.Sprites.Length.ToString ());
			foreach (var sprite in atlas.Sprites) {
				EditorGUILayout.ObjectField (sprite, typeof(Sprite), true);
			}
		}
	}
}
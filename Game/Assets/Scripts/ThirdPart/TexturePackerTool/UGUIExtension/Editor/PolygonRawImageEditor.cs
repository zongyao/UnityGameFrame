using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[CustomEditor(typeof(PolygonRawImage))]
public class PolygonRawImageEditor : RawImageEditor  {
	private SerializedProperty propertyEdges;
	private SerializedProperty propertyRotationOffset;

	protected override void OnEnable()
	{
		base.OnEnable ();
		propertyEdges = serializedObject.FindProperty ("edges");
		propertyRotationOffset = serializedObject.FindProperty ("rotationOffset");
	}

	public override void OnInspectorGUI (){
		serializedObject.Update ();
		EditorGUI.BeginDisabledGroup (true);
		EditorGUILayout.ObjectField ("Scrpt", MonoScript.FromMonoBehaviour (target as MonoBehaviour), typeof(MonoScript), false);
		EditorGUI.EndDisabledGroup ();
		base.OnInspectorGUI();
		EditorGUILayout.PropertyField (propertyEdges);
		EditorGUILayout.PropertyField (propertyRotationOffset);
		serializedObject.ApplyModifiedProperties ();
	}

	[MenuItem("GameObject/UI/Polygon Raw Image", false, -100)]
	private static void Create(){
		UGUIEditorUtil.CreateUGUIObject<PolygonRawImage> ("PolygonRawImage");
	}
}
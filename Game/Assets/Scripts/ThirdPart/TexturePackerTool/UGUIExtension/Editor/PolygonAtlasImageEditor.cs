using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[CustomEditor(typeof(PolygonAtlasImage))]
public class PolygonAtlasImageEditor : AtlasImageEditor {
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
		base.OnInspectorGUI();
		EditorGUILayout.PropertyField (propertyEdges);
		EditorGUILayout.PropertyField (propertyRotationOffset);
		serializedObject.ApplyModifiedProperties ();
	}

	[MenuItem("GameObject/UI/Polygon Atlas Image", false, -100)]
	private static void Create(){
		UGUIEditorUtil.CreateUGUIObject<PolygonAtlasImage> ("PolygonAtlasImage");
	}
}
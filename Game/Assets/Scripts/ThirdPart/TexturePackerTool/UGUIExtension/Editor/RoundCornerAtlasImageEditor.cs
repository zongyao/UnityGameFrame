using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[CustomEditor(typeof(RoundCornerAtlasImage))]
public class RoundCornerAtlasImageEditor : AtlasImageEditor {
	private SerializedProperty propertySegment;
	private SerializedProperty propertyTopLeftRadius;
	private SerializedProperty propertyTopRightRadius;
	private SerializedProperty propertyBottomLeftRadius;
	private SerializedProperty propertyBottomRightRadius;

	private bool unifiedRadius = true;
	protected override void OnEnable()
	{
		base.OnEnable ();
		propertySegment = serializedObject.FindProperty ("segment");
		propertyTopLeftRadius = serializedObject.FindProperty ("topLeftRadius");
		propertyTopRightRadius = serializedObject.FindProperty ("topRightRadius");
		propertyBottomLeftRadius = serializedObject.FindProperty ("bottomLeftRadius");
		propertyBottomRightRadius = serializedObject.FindProperty ("bottomRightRadius");
	}

	public override void OnInspectorGUI (){
		serializedObject.Update ();
		base.OnInspectorGUI();
		EditorGUILayout.PropertyField (propertySegment);
		unifiedRadius = EditorGUILayout.Toggle ("Unified Radius", unifiedRadius);
		if (unifiedRadius == false) {
			EditorGUILayout.PropertyField (propertyTopLeftRadius);
			EditorGUILayout.PropertyField (propertyTopRightRadius);
			EditorGUILayout.PropertyField (propertyBottomLeftRadius);
			EditorGUILayout.PropertyField (propertyBottomRightRadius);
		} else {
			EditorGUILayout.PropertyField (propertyTopLeftRadius,new GUIContent("Radius"));
			propertyTopRightRadius.doubleValue = propertyTopLeftRadius.doubleValue;
			propertyBottomLeftRadius.doubleValue = propertyTopLeftRadius.doubleValue;
			propertyBottomRightRadius.doubleValue = propertyTopLeftRadius.doubleValue;
		}
		propertyTopLeftRadius.doubleValue = propertyTopLeftRadius.doubleValue < 0 ? 0 : propertyTopLeftRadius.doubleValue;
		propertyTopRightRadius.doubleValue = propertyTopRightRadius.doubleValue < 0 ? 0 : propertyTopRightRadius.doubleValue;
		propertyBottomLeftRadius.doubleValue = propertyBottomLeftRadius.doubleValue < 0 ? 0 : propertyBottomLeftRadius.doubleValue;
		propertyBottomRightRadius.doubleValue = propertyBottomRightRadius.doubleValue < 0 ? 0 : propertyBottomRightRadius.doubleValue;
		serializedObject.ApplyModifiedProperties ();
	}

	[MenuItem("GameObject/UI/Round Corner Atlas Image", false, -100)]
	private static void Create(){
		UGUIEditorUtil.CreateUGUIObject <RoundCornerAtlasImage>("RoundCornerAtlasImage");
	}
}
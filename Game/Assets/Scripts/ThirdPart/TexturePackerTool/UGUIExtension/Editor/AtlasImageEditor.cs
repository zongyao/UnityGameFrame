using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;
using UnityEditor.AnimatedValues;
using UnityEngine.EventSystems;

[CustomEditor (typeof(AtlasImage), true)]
public class AtlasImageEditor : ImageEditor
{

	private SerializedProperty atlasProperty;
	private SerializedProperty spriteProperty;
	private SerializedProperty typeProperty;
	private SerializedProperty preserveAspectProperty;

	private AnimBool showType;

	protected override void OnEnable ()
	{
		base.OnEnable ();
		atlasProperty = serializedObject.FindProperty ("atlas");
		spriteProperty = serializedObject.FindProperty ("m_Sprite");
		typeProperty = serializedObject.FindProperty ("m_Type");
		preserveAspectProperty = serializedObject.FindProperty ("m_PreserveAspect");
		showType = new AnimBool (spriteProperty.objectReferenceValue != null);
	}

	private void OnDoubleClickAtlas (Atlas atlas)
	{
		serializedObject.Update ();
		SerializedProperty atlasProperty = serializedObject.FindProperty ("atlas");
		if (atlasProperty != null && atlasProperty.objectReferenceValue != atlas) {
			Undo.RecordObject (serializedObject.targetObject, "Atlas Selection");
			atlasProperty.objectReferenceValue = atlas;
			SerializedProperty spriteProperty = serializedObject.FindProperty ("m_Sprite");
			if (spriteProperty != null) {
				spriteProperty.objectReferenceValue = null;
			}
			serializedObject.ApplyModifiedProperties ();
			EditorUtility.SetDirty (serializedObject.targetObject);
		}
		AtlasSelector.Hide ();
	}

	private void OnClickSprite (Sprite sprite)
	{
		serializedObject.Update ();
		SerializedProperty spriteProperty = serializedObject.FindProperty ("m_Sprite");
		if (spriteProperty != null && spriteProperty.objectReferenceValue != sprite) {
			Undo.RecordObject (serializedObject.targetObject, "Sprite Selection");
			spriteProperty.objectReferenceValue = sprite;
			serializedObject.ApplyModifiedProperties ();
			EditorUtility.SetDirty (serializedObject.targetObject);
		}
	}

	private void OnDoubleClickSprite (Sprite sprite)
	{
		AtlasSpriteSelector.Hide ();
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update ();
		EditorGUI.BeginDisabledGroup (true);
		EditorGUILayout.ObjectField ("Scrpt", MonoScript.FromMonoBehaviour (target as MonoBehaviour), typeof(MonoScript), false);
		EditorGUI.EndDisabledGroup ();
		if (atlasProperty != null) {
			using (EditorGUILayout.HorizontalScope horizontalScope = new EditorGUILayout.HorizontalScope ()) {
				if (GUILayout.Button ("Atlas", "DropDown", GUILayout.Width (EditorGUIUtility.labelWidth - 5.0f)) == true) {
					AtlasSelector.Show (null, OnDoubleClickAtlas, atlasProperty.objectReferenceValue as Atlas);
				}
				//绘制图集属性
				Atlas selectedAtlas = EditorGUILayout.ObjectField (atlasProperty.objectReferenceValue, typeof(Atlas), true) as Atlas;
				if (selectedAtlas != atlasProperty.objectReferenceValue) {
					atlasProperty.objectReferenceValue = selectedAtlas;
					spriteProperty.objectReferenceValue = null;
					//图集不为空，绘制图集信息下拉列表
					serializedObject.ApplyModifiedProperties ();
				}
			}
				
			using (EditorGUILayout.HorizontalScope horizontalScope = new EditorGUILayout.HorizontalScope ()) {
				Atlas atlas = atlasProperty.objectReferenceValue as Atlas;
				if (atlas != null) {
					if (GUILayout.Button ("Sprite", "DropDown", GUILayout.Width (EditorGUIUtility.labelWidth - 5.0f)) == true) {
						AtlasSpriteSelector.Show (atlas, OnClickSprite, OnDoubleClickSprite, spriteProperty.objectReferenceValue as Sprite);
					}
				} else {
					GUILayout.Label ("Sprite", GUILayout.Width (EditorGUIUtility.labelWidth - 5.0f));
				}
				using (EditorGUI.DisabledScope disableGroup = new EditorGUI.DisabledScope (atlas != null)) {
					using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope ()) {		
						EditorGUILayout.PropertyField (spriteProperty, new GUIContent (""));
						serializedObject.ApplyModifiedProperties ();

						if (changeCheckScope.changed == true) {
							var newSprite = spriteProperty.objectReferenceValue as Sprite;
							if (newSprite) {
								Image.Type oldType = (Image.Type)typeProperty.enumValueIndex;
								if (newSprite.border.SqrMagnitude () > 0) {
									typeProperty.enumValueIndex = (int)Image.Type.Sliced;
								} else if (oldType == Image.Type.Sliced) {
									typeProperty.enumValueIndex = (int)Image.Type.Simple;
								}
							}
						}
						serializedObject.ApplyModifiedProperties ();
					}
				}
			}

			AppearanceControlsGUI ();
			RaycastControlsGUI ();

			showType.target = spriteProperty.objectReferenceValue != null;
			if (EditorGUILayout.BeginFadeGroup (showType.faded))
				TypeGUI ();
			EditorGUILayout.EndFadeGroup ();

			SetShowNativeSize (true, true);
			if (EditorGUILayout.BeginFadeGroup (m_ShowNativeSize.faded)) {
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField (preserveAspectProperty);
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFadeGroup ();

			NativeSizeButtonGUI ();
			serializedObject.ApplyModifiedProperties ();
		}
	}

	[MenuItem ("GameObject/UI/Atlas Image", false, -100)]
	private static void Create ()
	{
		UGUIEditorUtil.CreateUGUIObject<AtlasImage> ("AtlasImage");
	}

	[MenuItem ("CONTEXT/Image/Replace by AtlasImage", false)]
	private static void ReplaceImageByAtlasImage ()
	{
		Debug.Log ("replace");
	}
}
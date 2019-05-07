using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

public class UGUIEditorUtil {
	private static Canvas CreateCanvas()
	{
		GameObject objectCanvas = new GameObject ("Canvas");
		Canvas canvas = objectCanvas.AddComponent<Canvas> ();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;

		objectCanvas.AddComponent<CanvasScaler> ();
		objectCanvas.AddComponent<GraphicRaycaster> ();

		//EventSystem不存在，添加之
		EventSystem eventSystem = GameObject.FindObjectOfType<EventSystem> ();
		if (eventSystem == null) {
			GameObject objectEventSystem = new GameObject ("EventSystem");
			objectEventSystem.transform.position = Vector3.zero;
			objectEventSystem.AddComponent<EventSystem> ();
			objectEventSystem.AddComponent<StandaloneInputModule> ();
		}
		return canvas;
	}

	public static void CreateUGUIObject<T>(string objectName) where T : UIBehaviour
	{
		Transform transformRoot;
		//选中物体
		if (Selection.activeTransform != null) {
			//查找父级Canvas
			Canvas canvas = Selection.activeTransform.GetComponentInParent<Canvas> ();
			//存在，设置根节点
			if (canvas != null) {
				transformRoot = Selection.activeTransform;
			} else {//不存在，创建Canvas
				canvas = GameObject.FindObjectOfType<Canvas> ();
				if (canvas == null) {
					canvas = UGUIEditorUtil.CreateCanvas ();
				}
				transformRoot = canvas.transform;
			}
		} else {//没有选中，查找是否存在Canvas
			Canvas canvas = GameObject.FindObjectOfType<Canvas> ();
			if (canvas != null) {//存在，根节点为Canvas
				transformRoot = canvas.transform;
			} else {//不存在，创建之
				transformRoot = UGUIEditorUtil.CreateCanvas ().transform;
			}
		}

		GameObject newObject = new GameObject (objectName);
		newObject.AddComponent<RectTransform> ();
		newObject.AddComponent <T> ();

		newObject.transform.SetParent (transformRoot);
		newObject.transform.localScale = Vector3.one;
		newObject.transform.localPosition = Vector3.zero;

		newObject.layer = LayerMask.NameToLayer ("UI");
		Selection.activeGameObject = newObject;
	}
}
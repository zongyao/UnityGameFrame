using UnityEngine;
using System.Collections;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
	/// <summary>
	/// 是否被销毁
	/// </summary>
	public static bool Destroyed { get { return instance == null; } }

	//程序退出标识
	private static bool applicationIsQuitting = false;

	/// <summary>
	/// 单例
	/// </summary>
	public static T Instance {
		get {
			//程序已退出，返回空
			if (applicationIsQuitting) {return null;}
			if (instance==null) {
				//先尝试寻找实例
				instance = GameObject.FindObjectOfType<T> ();
				//没有实例，创建之
				if (instance==null) {
					instance = new GameObject ().AddComponent<T> ();
					instance.gameObject.name = string.Format ("[{0}]", instance.GetType ().Name);
				}
			}
			return instance;
		}
	}
	private static T instance = null;


	protected virtual void OnDestroy ()
	{
		instance = null;
	}

	protected virtual void OnApplicationQuit ()
	{
		instance = null;
		applicationIsQuitting = true;
	}
}
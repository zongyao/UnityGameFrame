using System;

/// <summary>
/// 泛型单例类。
/// </summary>
public abstract class Singleton<T> where T : class//, new()//使用new()限定后，参数类必须是非抽象的、具有公共无参构造函数。
{
	//单例
	protected static T instance;
	private static readonly object lockObject = new object ();

	/// <summary>
	/// 获取单例。
	/// </summary>
	/// <value>The instance.</value>
	public static T Instance {
		get {
			//使用双重锁创建对象
			if (instance == null) {
				lock (lockObject) {
					if (instance == null) {
						//instance=new T();//使用new()限定后，可以使用这种方式创建对象
						//使用反射创建对象
						instance = (T)Activator.CreateInstance(typeof(T), true);
					}
				}
			}
			return instance;
		}
	}

	protected Singleton(){}

	/// <summary>
	/// 创建单例
	/// </summary>
	public static void Create()
	{
		instance = (T)Activator.CreateInstance (typeof(T), true);
	}
	/// <summary>
	/// 销毁单例
	/// </summary>
	public static void DestroyInstance()
	{
		instance = null;
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class AssetBundleLoader : MonoSingleton<AssetBundleLoader>
{
    public class AssetBundleTask : IDisposable
    {
        public string path;
        public AssetBundleCreateRequest request;
        public Action<AssetBundle> onFinishAction;
        public Action<AssetBundleCreateRequest> onProcessingAction;
        public AssetBundleTask(
            string path,
            AssetBundleCreateRequest request,
            Action<AssetBundle> onFinishAction,
            Action<AssetBundleCreateRequest> onProcessingAction)
        {
            this.path = path;
            this.request = request;
            this.onFinishAction = onFinishAction;
            this.onProcessingAction = onProcessingAction;
        }

        public void Dispose()
        {
            path = null;
            onFinishAction = null;
            onProcessingAction = null;
            request = null;
        }
    }

    private Dictionary<string, AssetBundleTask> taskMap = new Dictionary<string, AssetBundleTask>();
    private Dictionary<string, AssetBundleTask> taskMapToAdd = new Dictionary<string, AssetBundleTask>();

    private void CreateTaskToAdd(
        string path,
        Action<AssetBundle> onFinishAction,
        Action<AssetBundleCreateRequest> onProcessingAction
    )
    {
        lock (taskMapToAdd)
        {
            if (taskMapToAdd.ContainsKey(path) == true)
            {
                AssetBundleTask task = taskMapToAdd[path];
                task.onFinishAction = onFinishAction;
                task.onProcessingAction = onProcessingAction;
            }
            else
            {
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
                AssetBundleTask task = new AssetBundleTask(path, request, onFinishAction, onProcessingAction);
                taskMapToAdd.Add(path, task);
            }
            this.enabled = true;
        }
    }

    public bool CheckPath(string path)
    {
        if (string.IsNullOrEmpty(path) == true)
        {
#if UNITY_EDITOR
            Debug.LogWarningFormat("{0} Invalid path : {1}, do finish action immediately.", this.name, path);
#endif
            return false;
        }
        else
        {
            return true;
        }
    }

    public void LoadFromFileAsync(
        string path,
        Action<AssetBundle> onFinishAction = null,
        Action<AssetBundleCreateRequest> onProcessingAction = null
    )
    {
        if (CheckPath(path) == false)
        {
            if (onFinishAction != null)
            {
                try
                {
                    onFinishAction.Invoke(null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                return;
            }
        }
        else
        {
            CreateTaskToAdd(path, onFinishAction, onProcessingAction);
        }
    }

    void Update()
    {
        if (taskMapToAdd != null && taskMapToAdd.Count > 0)
        {
            foreach (var kvp in taskMapToAdd)
            {
                string keyToAdd = kvp.Key;
                AssetBundleTask taskToAdd = kvp.Value;
                if (taskMap.ContainsKey(keyToAdd) == true)
                {
                    AssetBundleTask task = taskMap[keyToAdd];
                    task.onFinishAction = taskToAdd.onFinishAction;
                    task.onProcessingAction = taskToAdd.onProcessingAction;
                }
                else
                {
                    taskMap.Add(keyToAdd, taskToAdd);
                }
            }
            taskMapToAdd.Clear();
        }

        if (taskMap != null && taskMap.Count > 0)
        {
            List<string> tasksToRemove = null;
            foreach (var kvp in taskMap)
            {
                AssetBundleTask task = kvp.Value;
                AssetBundleCreateRequest request = task.request;
                if (request.isDone == true)
                {
                    AssetBundle assetBundle = request.assetBundle;
                    if (assetBundle == null)
                    {
#if UNITY_EDITOR
                        Debug.LogWarningFormat("{0} Load AssetBundle failed at path : {1}", this.name, task.path);
#endif
                    }
                    if (task.onFinishAction != null)
                    {
                        try
                        {
                            task.onFinishAction.Invoke(assetBundle);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                    tasksToRemove = tasksToRemove ?? new List<string>();
                    tasksToRemove.Add(task.path);
                }
                else
                {
                    if (task.onProcessingAction != null)
                    {
                        try
                        {
                            task.onProcessingAction.Invoke(request);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }

            if (tasksToRemove != null && tasksToRemove.Count > 0)
            {
                for (int i = tasksToRemove.Count - 1; i >= 0; i--)
                {
                    AssetBundleTask task = taskMap[tasksToRemove[i]];
                    taskMap.Remove(tasksToRemove[i]);
                    task.Dispose();
                }
                tasksToRemove.Clear();
                tasksToRemove = null;
            }

            if (taskMap.Count == 0 && taskMapToAdd.Count == 0)
            {
                this.enabled = false;
            }
        }
    }
}
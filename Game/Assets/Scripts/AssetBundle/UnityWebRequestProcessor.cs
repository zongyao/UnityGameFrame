using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class UnityWebRequestProcessor : MonoSingleton<UnityWebRequestProcessor>
{
    public class UnityWebRequestTask : IDisposable
    {
        public string path { get; private set; }
        public UnityWebRequest request { get; private set; }
        public Action<UnityWebRequest> onFinishAction;
        public Action<UnityWebRequest> onProcessingAction;

        public UnityWebRequestTask(
            string path,
            UnityWebRequest request,
            Action<UnityWebRequest> onFinishAction,
            Action<UnityWebRequest> onProcessingAction
        )
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
            request.Dispose();
            request = null;
        }
    }
    private Dictionary<string, UnityWebRequestTask> downloadTaskMap = new Dictionary<string, UnityWebRequestTask>();
    private Dictionary<string, UnityWebRequestTask> downloadTaskMapToAdd = new Dictionary<string, UnityWebRequestTask>();

    private void CreateTaskToAdd(
        string path,
        Action<UnityWebRequest> onFinishAction,
        Action<UnityWebRequest> onProcessingAction,
        int timeout = 60
    )
    {
        lock (downloadTaskMapToAdd)
        {
            if (downloadTaskMapToAdd.ContainsKey(path) == true)
            {
                UnityWebRequestTask task = downloadTaskMapToAdd[path];
                task.onFinishAction = onFinishAction;
                task.onProcessingAction = onProcessingAction;
                task.request.timeout = timeout;
            }
            else
            {
                UnityWebRequest request = UnityWebRequest.Get(path);
                request.timeout = timeout;
                UnityWebRequestTask task = new UnityWebRequestTask(path, request, onFinishAction, onProcessingAction);
                downloadTaskMapToAdd = downloadTaskMapToAdd ?? new Dictionary<string, UnityWebRequestTask>();
                downloadTaskMapToAdd.Add(path, task);
            }
            this.enabled = true;
        }
    }

    private bool CheckPath(string path)
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
    public void GetBytes(
        string path,
        Action<byte[]> onFinishAction = null,
        Action<UnityWebRequest> onProcessingAction = null,
        int timeout = 60
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
            CreateTaskToAdd(path,
                (request) =>
                {
                    if (onFinishAction != null)
                    {
                        try
                        {
                            byte[] data = request == null ? null : request.downloadHandler.data;
                            onFinishAction.Invoke(data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                },
                onProcessingAction, timeout);
        }
    }

    public void GetText(
        string path,
        Action<string> onFinishAction = null,
        Action<UnityWebRequest> onProcessingAction = null,
        int timeout = 60
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
            CreateTaskToAdd(path,
                (request) =>
                {
                    if (onFinishAction != null)
                    {
                        try
                        {
                            string text = request == null ? null : request.downloadHandler.text;
                            onFinishAction.Invoke(text);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                },
                onProcessingAction, timeout);
        }
    }

    void Update()
    {
        if (downloadTaskMapToAdd != null && downloadTaskMapToAdd.Count > 0)
        {
            foreach (var kvp in downloadTaskMapToAdd)
            {
                string keyToAdd = kvp.Key;
                UnityWebRequestTask taskToAdd = kvp.Value;
                if (downloadTaskMap.ContainsKey(keyToAdd) == true)
                {
                    UnityWebRequestTask task = downloadTaskMap[keyToAdd];
                    task.onFinishAction = taskToAdd.onFinishAction;
                    task.onProcessingAction = taskToAdd.onProcessingAction;
                }
                else
                {
                    downloadTaskMap.Add(keyToAdd, taskToAdd);
                    taskToAdd.request.SendWebRequest();
                }
            }
            downloadTaskMapToAdd.Clear();
        }

        if (downloadTaskMap != null && downloadTaskMap.Count > 0)
        {
            List<string> tasksToRemove = null;
            foreach (var kvp in downloadTaskMap)
            {
                UnityWebRequestTask task = kvp.Value;
                string path = kvp.Key;
                UnityWebRequest request = task.request;
                if (request.isNetworkError == true || request.isHttpError == true)
                {
#if UNITY_EDITOR
                    Debug.LogWarningFormat("[{0}] Error : {1} at path : {2}", this.name, request.error, path);
#endif
                    if (task.onFinishAction != null)
                    {
                        try
                        {
                            task.onFinishAction.Invoke(request);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                    tasksToRemove = tasksToRemove ?? new List<string>();
                    tasksToRemove.Add(path);
                }
                else if (request.isDone == true)
                {
                    if (task.onFinishAction != null)
                    {
                        try
                        {
                            task.onFinishAction.Invoke(request);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                    tasksToRemove = tasksToRemove ?? new List<string>();
                    tasksToRemove.Add(path);
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
                    UnityWebRequestTask task = downloadTaskMap[tasksToRemove[i]];
                    downloadTaskMap.Remove(tasksToRemove[i]);
                    task.Dispose();
                }
                tasksToRemove.Clear();
                tasksToRemove = null;
            }

            if (downloadTaskMap.Count == 0 && downloadTaskMapToAdd.Count == 0)
            {
                this.enabled = false;
            }
        }
    }
}
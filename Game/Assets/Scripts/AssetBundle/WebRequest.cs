using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebRequest
{
    /// <summary>
    /// 从网络获取一段文本
    /// </summary>
    public static void GetText(
        string path,
        Action<string> onFinishAction = null,
        Action<UnityWebRequest> onProcessingAction = null,
        int timeout = 60
    )
    {
        UnityWebRequestProcessor.Instance.GetText(
            path,
            onFinishAction,
            onProcessingAction,
            timeout
        );
    }
    /// <summary>
    /// 从网络获取二进制数据
    /// </summary>
    public static void GetBytes(
        string path,
        Action<byte[]> onFinishAction = null,
        Action<UnityWebRequest> onProcessingAction = null,
        int timeout = 60
    )
    {
        UnityWebRequestProcessor.Instance.GetBytes(
            path,
            onFinishAction,
            onProcessingAction,
            timeout
        );
    }
}
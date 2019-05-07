using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ABDemo : MonoBehaviour
{

    void Start()
    {
        // AssetBundleManager.Instance.LoadAbAssetAsync<GameObject>("cube", "Cube", (obj)=>{
        //     Instantiate(obj);
        // });

       GameObject a = ResourceManager.Instance.LoadFromAssetBundleSync<GameObject>("cube", "Cube") ;
        Instantiate(a);


         ResourceManager.Instance.LoadFromAssetBundleAsync<GameObject>("cube", "Cube", (obj)=>{
         GameObject b = obj ;
          Instantiate(b);
         });
    }



}

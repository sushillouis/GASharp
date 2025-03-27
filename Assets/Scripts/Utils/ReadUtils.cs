using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ReadUtils : MonoBehaviour
{
    public static ReadUtils inst;
    private void Awake() {
        inst = this;
    }

    private void Start() {
        Debug.Log("Ready to read");
    }

    public string URL;
    public string fileText;
    public bool isReadDone;
    public void ReadFile(string filename) {
        Debug.Log("Reading file: " + filename);
        isReadDone = false;
        StartCoroutine(ReadFileCoroutine(filename));
    }

    IEnumerator ReadFileCoroutine(string filename) {
        string url = URL + "/" + filename;
        if(Application.platform == RuntimePlatform.WebGLPlayer) {
            using(UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
                Debug.Log("Dowloading from: " + url);
                yield return webRequest.SendWebRequest();
                if(webRequest.result != UnityWebRequest.Result.Success) {
                    Debug.Log($"Failed to download: {webRequest.error}");
                } else {
                    Debug.Log($"Succeeded in downloading: {url}");
                    fileText = webRequest.downloadHandler.text;
                }
                isReadDone = true; //success or not, you are done
            }
        } else {
            ReadLocal(filename);
            isReadDone = true;
        }
    }


    public void ReadLocal(string filename) {
        using(StreamReader sr = new StreamReader(filename)) {
            fileText = sr.ReadToEnd();
        }
    }



    IEnumerator WebReader(string url) {
        using(UnityWebRequest webRequest = UnityWebRequest.Get(url)) {
            Debug.Log("Dowloading from: " + url);
            yield return webRequest.SendWebRequest();
            if(webRequest.result != UnityWebRequest.Result.Success) {
                Debug.Log($"Failed to download: {webRequest.error}");
            } else {
                Debug.Log($"Succeeded in downloading: {url}");
                fileText = webRequest.downloadHandler.text;
            }
            isReadDone = true; //success or not, you are done
        }
    }

}

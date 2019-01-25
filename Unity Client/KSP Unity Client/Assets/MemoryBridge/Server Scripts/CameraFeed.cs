using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Winterdom.IO.FileMap;

public class CameraFeed : MonoBehaviour
{
    Texture2D readTex, tex, texCopy;
    MemoryMappedFile camFeedFile;

    MemoryBridge memoryBridge;

    bool _receivingResponce = false;
    public bool receivingResponce { get { return _receivingResponce; } }

    int rendTextWidth, rendTextHeight, feedByteCount;
    Transform cameraRig;
    public RenderTexture rendText;
    string fileName;
    Texture2D camTexture;
    MeshRenderer meshRenderer;
    public void StartFeed(MemoryBridge memoryBridge, string fileName)
    {
        meshRenderer = GetComponent<MeshRenderer>();

        this.memoryBridge = memoryBridge;
        this.fileName = fileName;

        memoryBridge.SetVector3("CamLocalPos" + fileName + memoryBridge.cameraFeeds.Count, Vector3.zero);
        memoryBridge.SetVector3("CamLocalEuler" + fileName + memoryBridge.cameraFeeds.Count, Vector3.zero);

        //meshRenderer.material.SetTexture("_MainTex", readTex);
    }

    void BuildCamera()
    {
        // rendTextHeight = cam.pixelHeight;
        //  rendTextWidth = cam.pixelWidth;
        var newRig = Instantiate(Resources.Load("CamPrefab", typeof(GameObject))) as GameObject;
        cameraRig = newRig.transform;
        cameraRig.SetParent(memoryBridge.transform);
        meshRenderer = cameraRig.Find("Screen").GetComponent<MeshRenderer>();

        rendTextHeight = (int)memoryBridge.GetFloat("rendTextHeight" + fileName + memoryBridge.cameraFeeds.Count);
        rendTextWidth = (int)memoryBridge.GetFloat("rendTextWidth" + fileName + memoryBridge.cameraFeeds.Count);
        feedByteCount = (int)memoryBridge.GetFloat("feedByteCount" + fileName + memoryBridge.cameraFeeds.Count);

        readTex = new Texture2D(rendTextWidth, rendTextHeight, TextureFormat.RGB24, false);
        tex = new Texture2D(rendTextWidth, rendTextHeight, TextureFormat.RGB24, false);
        texCopy = new Texture2D(rendTextWidth, rendTextHeight, TextureFormat.RGB24, false);
        Debug.Log("rendTextWidth " + rendTextWidth);
        rendText = new RenderTexture(rendTextWidth, rendTextHeight, 16, RenderTextureFormat.ARGB32);
        rendText.Create();
    }
    Stream mapStream;
    void Update()
    {
        if (!receivingResponce) //IS THE CLIENT RESPONDING
        {
            Debug.Log("client file empty");
            //camFeedFile = MemoryMappedFile.Create(MapProtection.PageReadWrite, textureByte.Length, "CamFeedFile" + fileName + this.memoryBridge.cameraFeeds.Count); 
            camFeedFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "CamFeedFile" + fileName + memoryBridge.cameraFeeds.Count);

            if (camFeedFile != null)
            {
                _receivingResponce = true;
                BuildCamera();
            }
        }
        else
        {
            Debug.Log("cam feed update");
            memoryBridge.SetVector3("CamLocalPos" + fileName + memoryBridge.cameraFeeds.Count,cameraRig.localPosition);
            memoryBridge.SetVector3("CamLocalEuler" + fileName + memoryBridge.cameraFeeds.Count,cameraRig.localEulerAngles);
            /////WORKS////
            using (Stream camStream = camFeedFile.MapView(MapAccess.FileMapAllAccess, 0, feedByteCount))
            {
                byte[] buffer = new byte[feedByteCount];
                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = camStream.Read(buffer, 0, buffer.Length)) > 0)
                        ms.Write(buffer, 0, read);
                    var byteArray = ms.ToArray();
                    readTex.LoadRawTextureData(byteArray);
                }
            }
            readTex.Apply();
            meshRenderer.material.SetTexture("_MainTex", readTex);
        }
    }

    private void OnDestroy()
    {
        if(camFeedFile != null)
            camFeedFile.Close();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;
using System.Threading;
using System.IO;
using System.Collections;


namespace MemoryBridgeServer
{
    class CameraFeed : MonoBehaviour
    {
        public RenderTexture rendText;
        Texture2D camTexture;
        Camera feedCam;
        Material camMat;
        public MeshRenderer meshRenderer;

        MemoryMappedFile camFeedFile;

        int rendTextWidth, rendTextHeight;

        List<int> threadList;

        Texture2D tex, texCopy;
        MemoryBridge memoryBridge;

        string fileName;

        public void BuildCameraFeed(MemoryBridge memoryBridge, string fileName)
        {
            //Debug.Log("Build Camera Feed");
            this.memoryBridge = memoryBridge;
            this.fileName = fileName;

            threadList = new List<int>();
            threadList.Add(0);
            threadList.Add(1);
         //   threadList.Add(2);
          //  threadList.Add(3);

            GameObject camObject = new GameObject();
            DebugVector.DrawVector(camObject.transform);
            camObject.transform.SetParent(FlightGlobals.ActiveVessel.vesselTransform);

            feedCam = camObject.AddComponent<Camera>();

            rendTextHeight = feedCam.pixelHeight;
            rendTextWidth = feedCam.pixelWidth;
            //Debug.Log("height " + rendTextHeight + " width " + rendTextWidth);
            rendText = new RenderTexture(rendTextWidth, rendTextHeight, 16, RenderTextureFormat.ARGB32);
            rendText.Create();
            feedCam.targetTexture = rendText;

            tex = new Texture2D(rendTextWidth, rendTextWidth, TextureFormat.RGB24, false);
            texCopy = new Texture2D(rendTextWidth, rendTextHeight, TextureFormat.RGB24, false);

            RenderTexture.active = rendText;
            //Debug.Log("Render width " + rendTextWidth);
            //Debug.Log("render height " + rendTextHeight);
            tex.ReadPixels(new Rect(0, 0, rendTextWidth, rendTextHeight), 0, 0);
            tex.Apply();
            camTexture = tex;
            // var colorBuffer = rendText.colorBuffer;
            var textureByte = camTexture.GetRawTextureData();

            camFeedFile = MemoryMappedFile.Create(MapProtection.PageReadWrite, textureByte.Length,"CamFeedFile" + fileName + memoryBridge.cameraFeeds.Count);

            memoryBridge.SetFloat("rendTextHeight" + fileName + memoryBridge.cameraFeeds.Count,rendTextHeight);
            memoryBridge.SetFloat("rendTextWidth" + fileName + memoryBridge.cameraFeeds.Count,rendTextWidth);
            memoryBridge.SetFloat("feedByteCount" + fileName + memoryBridge.cameraFeeds.Count,textureByte.Length);

            ////Debug.Log("ByteSize " + textureByte.Length);
            //Debug.Log("mem map loaded");
        }

        bool writingCamFile = false;
        void Update()
        {
            if (!writingCamFile)
                StartCoroutine(WriteCameraFeed());          
        }

        IEnumerator WriteCameraFeed()
        {
            writingCamFile = true;
            var tempLocalPos = feedCam.transform.localPosition;
            var bridgeCamPos = memoryBridge.GetVector3("CamLocalPos" + fileName + memoryBridge.cameraFeeds.Count);
            tempLocalPos.x = bridgeCamPos.x;
            tempLocalPos.y = -bridgeCamPos.z;
            tempLocalPos.z = bridgeCamPos.y;
            feedCam.transform.localPosition = tempLocalPos;

            feedCam.transform.localEulerAngles = memoryBridge.GetVector3("CamLocalEuler" + fileName + memoryBridge.cameraFeeds.Count);
            RenderTexture.active = rendText;
            tex.ReadPixels(new Rect(0, 0, rendTextWidth, rendTextHeight), 0, 0);
            //tex.Apply();
            //  camTexture = tex;
            var textureByte = tex.GetRawTextureData();
            // //Debug.Log(textureByte.Length);

            var multiple = textureByte.Length / threadList.Count;

            Stream mapStream0 = camFeedFile.MapView(MapAccess.FileMapAllAccess, 0, multiple);
            Stream mapStream1 = camFeedFile.MapView(MapAccess.FileMapAllAccess, 0, multiple);
            Stream mapStream2 = camFeedFile.MapView(MapAccess.FileMapAllAccess, 0, multiple);
            Stream mapStream3 = camFeedFile.MapView(MapAccess.FileMapAllAccess, 0, multiple);

            ParallelProcessor.EachParallel(threadList,
            thread =>
            {
                switch (thread)
                {
                    case 0:
                        mapStream0.Write(textureByte, 0, multiple);
                        break;
                    case 1:
                        mapStream1.Write(textureByte, 0, multiple);
                        break;
                    case 2:
                        mapStream2.Write(textureByte, 0, multiple);
                        break;
                    case 3:
                        mapStream3.Write(textureByte, 0, multiple);
                        break;
                }
            });
            mapStream0.Flush();
            mapStream1.Flush();
            mapStream2.Flush();
            mapStream3.Flush();

            mapStream0.Close();
            mapStream1.Close();
            mapStream2.Close();
            mapStream3.Close();

            RenderTexture.active = null;

            writingCamFile = false;

            yield break;


        }

        void OnDestroy()
        {
            if(camFeedFile != null)
            {
                camFeedFile.Dispose();
                camFeedFile.Close();
            }
                
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace KSPMechs
{
    public class MechManager : MonoBehaviour
    {
        MemoryBridge memoryBridge;
        public bool debugWriteToSelf;
        void Awake()
        {
            Application.runInBackground = true;
            // bridgeServer = new ServerManager();
            memoryBridge = GameObject.Find("Bridge Origin").GetComponent<MemoryBridge>();
            //memoryBridge = new MemoryBridge();
            memoryBridge.StartClient("Test");
        }

        private void Update()
        {
            // memoryBridge.Update();
            if (Input.anyKeyDown)
            {
              //  var valueTest = memoryBridge.GetFloat("ClientCameraFeedCountTest");
               // Debug.Log(valueTest);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("wtf");
                memoryBridge.CreateCameraFeed();
            }
                

        }
        private void LateUpdate()
        {
           // memoryBridge.LateUpdate();
        }
        private void OnDestroy()
        {
           // memoryBridge.OnDestroy();
        }
    }
}


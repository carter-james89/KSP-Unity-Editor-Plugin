using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace KSPMechs
{
    public class MechManager : MonoBehaviour
    {
        MemoryBridge memoryBridge;
        public bool debugWriteToSelf;
        protected virtual void Awake()
        {
            Application.runInBackground = true;
            // bridgeServer = new ServerManager();
            memoryBridge = GameObject.Find("Bridge Origin").GetComponent<MemoryBridge>();
            //memoryBridge = new MemoryBridge();
            memoryBridge.StartClient("Test");
            Debug.Log("Memory Bridge Opened");

            //memoryBridge.roboticController.ActivateIK();
        }

        protected virtual void Start()
        {

        }
        protected virtual void Update()
        {
            // memoryBridge.Update();
            //if (Input.GetKeyDown(KeyCode.Alpha6))
            //{
            //    memoryBridge.roboticController.ActivateIK();
            //    Debug.Log("IK Activated");
            //}
              //  ActivateIK();

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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MemoryBridgeServer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Class1 : MonoBehaviour
    {
        MemoryBridge memoryBridge;
        bool serverStarted = false;
        private void Start()
        {

            StartProgram();

            //for (int i = 0; i < 1000; i++)
            //{
            //    memoryBridge.SetValue("KeyCount" + i, i);
            //}
            //Debug.Log("Values set");
        
            //  if ()
            //     UnityEngine.Debug.Log("Hexapod IR ready to go");
        }

        void StartProgram()
        {
            Debug.Log("Init wrapper");
            var success = IRWrapper.InitWrapper();// IRWrapper.InitWrapper();


            // Debug.Log(".net version " + Application.)


            serverStarted = true;
            Debug.Log("start server " + Time.frameCount);
            Debug.Log("Unity version " + Application.version);
            Debug.Log("Unity version " + Application.unityVersion);
            memoryBridge = gameObject.AddComponent(typeof(MemoryBridge)) as MemoryBridge;
            memoryBridge.StartServer("Test");

        }

        bool firstFrame = true;
        private void Update()
        {
            if (firstFrame)
            {
                firstFrame = false;
            }
            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
             //   StartProgram();
            }
            if (IRWrapper.APIReady & !serverStarted)
            {
                //serverStarted = true;
                //Debug.Log("start server " + Time.frameCount);
                //memoryBridge = gameObject.AddComponent(typeof(MemoryBridge)) as MemoryBridge;
                //memoryBridge.StartServer("Test");
            }
            //for (int i = 0; i < 1000; i++)
            //{
            //    memoryBridge.SetValue("KeyCount" + i, i);
            //}
            //for (int i = 0; i < 1000; i++)
            //{
            //    memoryBridge.ReadValue("KeyCount" + i);
            //}

           
        }
    }
}

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



            //for (int i = 0; i < 1000; i++)
            //{
            //    memoryBridge.SetValue("KeyCount" + i, i);
            //}
            //Debug.Log("Values set");
            Debug.Log("Init wrapper");
            var success = IRWrapper.InitWrapper();// IRWrapper.InitWrapper();

            

           

            serverStarted = true;
            Debug.Log("start server " + Time.frameCount);
            memoryBridge = gameObject.AddComponent(typeof(MemoryBridge)) as MemoryBridge;
            memoryBridge.StartServer("Test");

            //  if ()
            //     UnityEngine.Debug.Log("Hexapod IR ready to go");
        }

        bool firstFrame = true;
        private void Update()
        {
            if (firstFrame)
            {
                firstFrame = false;
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

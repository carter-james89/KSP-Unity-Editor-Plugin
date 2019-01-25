using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BridgeManager : MonoBehaviour {
    MemoryBridge memoryBridge;

    // Use this for initialization
    void Awake () {
        System.Console.WriteLine("yo");
        memoryBridge = new MemoryBridge();
        //memoryBridge.CreateFile("Test");
       // MemoryBridge.
	}
	
	// Update is called once per frame
	void Update () {
       // memoryBridge.ReadFile();
	}

    private void OnDestroy()
    {
       // memoryBridge.OnDestroy();
    }
}

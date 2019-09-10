using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MemoryBridge))]
public class BridgeController : MonoBehaviour
{
    MemoryBridge memoryBridge;

    private void Awake()
    {
        memoryBridge = GetComponent<MemoryBridge>();
        memoryBridge.StartClient("Test");
        Debug.Log("Memory Bridge Opened");
    }

    void Start()
    {
        
    }


    void Update()
    {
        
    }
}

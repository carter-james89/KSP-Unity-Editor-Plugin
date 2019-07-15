using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneManager : MonoBehaviour
{
    MemoryBridge memoryBridge;
    public bool debugWriteToSelf;
    // Start is called before the first frame update
    void Start()
    {
        //Application.runInBackground = true;
        //// bridgeServer = new ServerManager();
        //memoryBridge = GameObject.Find("Bridge Origin").GetComponent<MemoryBridge>();
        ////memoryBridge = new MemoryBridge();
        //memoryBridge.StartClient("Test");
        //Debug.Log("Memory Bridge Opened");

       
    }

    // Update is called once per frame
    public void CustomUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            Debug.Log("7");
            FindObjectOfType<VesselControl>().ToggleAutoPilot(true);

           // FindObjectOfType<MemoryBridge>().SetFloat("yo11", Time.deltaTime);
        }
    }
}

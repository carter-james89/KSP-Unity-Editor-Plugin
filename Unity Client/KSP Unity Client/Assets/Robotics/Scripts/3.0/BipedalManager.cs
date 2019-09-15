using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BipedalManager : MonoBehaviour
{
    MemoryBridge memoryBridge;
    ServoLimb[] limbs;


    private void Awake()
    {
        memoryBridge = FindObjectOfType<MemoryBridge>();
        memoryBridge.StartClient("Test");
        Debug.Log("Memory Bridge Opened");

        limbs = FindObjectOfType<RoboticAssembler>().Assemble(memoryBridge);

        foreach (var limb in limbs)
        {
            limb.Initialize(memoryBridge);
            limb.FindContactGroundPoint();

            limb.CreateGait(true);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        memoryBridge.StartUpdate();

        foreach (var limb in limbs)
        {
            limb.MirrorServos();

        }
        foreach (var limb in limbs)
        {
            limb.SetGroundPos();

        }
    }
}

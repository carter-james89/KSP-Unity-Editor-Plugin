using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BipedalManager : RobotManager
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

            limb.CreateGait(true,gaitCurve);
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
        foreach (var limb in limbs)
        {
            CalculateTwoServoIK(limb.xAxisServos[0], limb.xAxisServos[1], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
            //  limb.ikServos[1].SetServoPos(50);
        }
    }
}

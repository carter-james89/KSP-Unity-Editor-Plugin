using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodManager : RobotManager
{
    MemoryBridge memoryBridge;
    ServoLimb[] limbs;

    List<ServoLimb> legs;

    ServoLimb neck;

    bool ikActive = false;

    private void Awake()
    {
        memoryBridge = FindObjectOfType<MemoryBridge>();
        memoryBridge.StartClient("Test");
        Debug.Log("Memory Bridge Opened");

        limbs = FindObjectOfType<RoboticAssembler>().Assemble(memoryBridge);

        legs = new List<ServoLimb>();
        foreach (var limb in limbs)
        {
            limb.Initialize(memoryBridge);
            limb.FindContactMeshPoint(ServoLimb.LimbAxis.Y);

            if (limb.name.Contains("leg"))
            {
                legs.Add(limb);
                limb.CreateGait(true,gaitCurve);
            }
            else
            {
                neck = limb;
                limb.CreateGait(false,gaitCurve);
            }
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
        foreach (var limb in legs)
        {
            limb.SetGroundPos();

        }

        if (ikActive)
        {
            RunIK();
        }     
    }

    void RunIK()
    {
        foreach (var limb in legs)
        {
            CalculateTwoServoIK(limb.zAxisServos[0], limb.zAxisServos[1], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
        }
    }
}

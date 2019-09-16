using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodManager : RobotManager
{
    MemoryBridge memoryBridge;
    ServoLimb[] limbs;

    List<ServoLimb> legs;

    ServoLimb neck;

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
                limb.CreateGait(true);
            }
            else
            {
                neck = limb;
                limb.CreateGait(false);
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

        CalculateTwoServoIK(limbs[1].zAxisServos[0], limbs[1].zAxisServos[1], limbs[1].gait.IKtargetTransform.position, limbs[1].limbEndPointIK.position);

        foreach (var limb in limbs)
        {
          //  limb.ikServos[1].SetServoPos(50);
        }
    }
}

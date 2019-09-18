using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BipedalManager : RobotManager
{
    ServoLimb spine;


    protected void Awake()
    {
        memoryBridge = FindObjectOfType<MemoryBridge>();
        memoryBridge.StartClient("Test");
        vessel = memoryBridge.vesselControl;
        Debug.Log("Memory Bridge Opened");

        limbs = FindObjectOfType<RoboticAssembler>().Assemble(memoryBridge);

        foreach (var limb in limbs)
        {
            limb.Initialize(this,memoryBridge);
           

            if (limb.name.ToLower().Contains("leg"))
            {
                legs.Add(limb);
            }
            else if (limb.name.ToLower().Contains("arm"))
            {
                arms.Add(limb);
            }
            else if (limb.name.ToLower().Contains("spine"))
            {
                spine = limb;
            }
            //limb.CreateGait(true,gaitCurve);
        }

        foreach (var limb in legs)
        {
            //limb.FindContactGroundPoint();
            limb.CreateLimbEndPoint(new Vector3(0, .22f, 0));
        }
        foreach (var limb in arms)
        {
            limb.FindContactMeshPoint(ServoLimb.LimbAxis.Y);
        }
        spine.CreateLimbEndPoint(new Vector3(0, .1f, 0));
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
  
        if(robotStatus != RobotStatus.Deactivated)
        {
            foreach (var limb in legs)
            {
                limb.SetGroundPos();

            }

            foreach (var limb in legs)
            {
                CalculateTwoServoIK(limb.xAxisServos[0], limb.xAxisServos[1], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
                //  limb.ikServos[1].SetServoPos(50);
               
            }
            foreach (var limb in arms)
            {
             //   CalculateSingleServoIK(limb.yAxisServos[0], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
                CalculateTwoServoIK(limb.xAxisServos[0], limb.zAxisServos[0], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
            }
        }
      
     
    }


    protected override void ActivateIK()
    {
        base.ActivateIK();
     
        foreach (var limb in arms)
        {
            limb.ActivateIK();
            limb.CreateGait(false, gaitCurve);
        }

    }
}

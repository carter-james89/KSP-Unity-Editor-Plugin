using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodRoboticController : RoboticController
{
    LimbController neckArm;
    Vector3 groundPos;
    public override void CustomAwake(MemoryBridge memoryBridge)
    {
        base.CustomAwake(memoryBridge);

        Debug.Log("Hexapod Awake");

        foreach (var limbObj in limbObjects)
        {
            LimbController newLimb;
            if (limbObj.name.ToLower().Contains("leg"))
            {
                newLimb = limbObj.AddComponent<LegController>();
                legs.Add(newLimb as LegController);
            }
            else
            {
                newLimb = limbObj.AddComponent(typeof(LimbController)) as LimbController;
                neckArm = newLimb;
            }
            limbs.Add(newLimb);
            newLimb.CustomAwake(memoryBridge, vesselControl, this);
        }

        if (legs.Count == 6)
        {
            Debug.Log("robot is a hexapod");
            groupLeft = new HexapodLimbGroup();
            groupLeft.limbs = new List<LimbController>();
            groupRight = new HexapodLimbGroup();
            groupRight.limbs = new List<LimbController>();

            group0 = new HexapodLimbGroup();
            group0.limbs = new List<LimbController>();
            group1 = new HexapodLimbGroup();
            group1.limbs = new List<LimbController>();

            foreach (var leg in legs)
            {
                var offset = memoryBridge.vesselControl.vessel.transform.InverseTransformPoint(leg.transform.position);
                Debug.Log(leg.name);
                var group = groupRight;
                if (offset.x < 0)
                {
                    Debug.Log("left leg found");
                    group = groupLeft;
                }

                group.limbs.Add(leg);
                if (leg.name.Contains("1"))
                {
                    Debug.Log("legs one found");
                    group.limb0 = leg;
                }
                if (leg.name.Contains("2"))
                {
                    Debug.Log("limb two found");
                    group.limb1 = leg;
                }
                if (leg.name.Contains("3"))
                {
                    Debug.Log("legs three found");
                    group.limb2 = leg;
                }

                if (offset.x < 0)
                {
                    groupLeft = group;
                }
                else
                {
                    groupRight = group;
                }
            }

            //foreach (var item in groupRight.limbs)
            //{
            //    Debug.Log(item.name);
            //}

            group0.limbs.Add(groupLeft.limb0);
            group0.limbs.Add(groupLeft.limb2);
            group0.limbs.Add(groupRight.limb1);

            group1.limbs.Add(groupRight.limb0);
            group1.limbs.Add(groupRight.limb2);
            group1.limbs.Add(groupLeft.limb1);
        }
    }

    public RoboticServoIK debugServo;
    public override void CustomUpdate()
    {
        base.CustomUpdate();

        if (robotStatus != RobotStatus.Deactivated)
        {
            neckArm.limbIK.CalculateIK(neckArm.limbIK.IKAxisY);

            neckArm.limbIK.CalculateTwoServoIK(neckArm.limbIK.IKAxisX.servoGroup[1], neckArm.limbIK.IKAxisX.servoGroup[2], neckArm.limbIK.IKtargetTransform.position - neckArm.limbIK.IKtargetTransform.forward.normalized * .3f);
         //   neckArm.limbIK.IKAxisX.servoGroup[0].MatchTargetAngle(neckArm.limbIK.IKtargetTransform);
            neckArm.limbIK.CalculateSingleServoIK(neckArm.limbIK.IKAxisX.servoGroup[0], neckArm.limbIK.IKAxisX.servoGroup[0].transform.position + neckArm.limbIK.IKtargetTransform.forward * .2f);
            //  neckArm.limbIK.CalculateIK(neckArm.limbIK.IKAxisZ);
          //  neckArm.SetServos();

        }
    }

    public override void ActivateIK()
    {
        base.ActivateIK();

        if (neckArm && neckArm)
            neckArm.ActivateIK(false);

        neckArm.limbIK.IKtargetTransform.position = new Vector3(0.012f, 2.28f, 2.27f);


        //   baseHeight = legs[0].servoBase.transform.position.y - groundPos.y;
    }
}

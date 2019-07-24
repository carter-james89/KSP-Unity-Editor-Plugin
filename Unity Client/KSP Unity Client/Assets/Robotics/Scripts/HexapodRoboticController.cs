using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodRoboticController : RoboticController
{
    LimbController neckArm;
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

            foreach (var legs in limbs)
            {
                var offset = memoryBridge.vesselControl.vessel.transform.InverseTransformPoint(legs.transform.position);
                Debug.Log(legs.name);
                var group = groupRight;
                if (offset.x < 0)
                {
                    Debug.Log("left leg found");
                    group = groupLeft;
                }

                group.limbs.Add(legs);
                if (legs.name.Contains("1"))
                {
                    Debug.Log("legs one found");
                    group.limb0 = legs;
                }
                if (legs.name.Contains("2"))
                {
                    Debug.Log("limb two found");
                    group.limb1 = legs;
                }
                if (legs.name.Contains("3"))
                {
                    Debug.Log("legs three found");
                    group.limb2 = legs;
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

    public override void ActivateIK()
    {
        base.ActivateIK();
        if (neckArm)
            neckArm.ActivateIK(false);
    }
}

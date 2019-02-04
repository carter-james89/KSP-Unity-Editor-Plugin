using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticController : MonoBehaviour
{
    IR_Manager IRmanager;
    public List<LimbController> limbs;

    struct HexapodLimbGroup { public List<LimbController> limbs; public LimbController limb0, limb1, limb2; }

    HexapodLimbGroup groupLeft, groupRight;
    public Transform baseTargets, directionTarget;

    MemoryBridge memoryBridge;

    public void CustomAwake(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;

        IRmanager = gameObject.GetComponent<IR_Manager>();
        IRmanager.CustomAwake(memoryBridge, memoryBridge.vesselControl, ref limbs);

        Debug.Log("IR Manager Enabled, Limb Count : " + limbs.Count);

        if (limbs.Count == 6)
        {
            Debug.Log("robot is a hexapod");
            groupLeft = new HexapodLimbGroup();
            groupLeft.limbs = new List<LimbController>();
            groupRight = new HexapodLimbGroup();
            groupRight.limbs = new List<LimbController>();
        }


        



        foreach (var limb in limbs)
        {
            limb.roboticController = this;
            var offset = memoryBridge.vesselControl.vessel.transform.InverseTransformPoint(limb.transform.position);
            Debug.Log(limb.name);
            var group = groupRight;
            if (offset.x > 0)
            {
                Debug.Log("left leg found");
                group = groupLeft;
            }

            group.limbs.Add(limb);
            if (limb.name.Contains("1"))
            {
                Debug.Log("limb one found");
                group.limb0 = limb;
            }
            if (limb.name.Contains("2"))
            {
                Debug.Log("limb two found");
                group.limb1 = limb;
            }
            if (limb.name.Contains("3"))
            {
                Debug.Log("limb three found");
                group.limb2 = limb;
            }

            if (offset.x > 0)
            {
                groupLeft = group;
            }
            else
            {
                groupRight = group;
            }
        }   
    }

    public void ActivateIK()
    {
        baseTargets = new GameObject("Base Targets").transform;
        baseTargets.SetParent(GameObject.Find("Vessel Offset").transform);
        baseTargets.localEulerAngles = Vector3.zero;
        baseTargets.SetParent(null);
        baseTargets.localPosition = Vector3.zero;

        directionTarget = new GameObject("Direction Target").transform;
        directionTarget.SetParent(GameObject.Find("Gimbal").transform);
        directionTarget.localEulerAngles = GameObject.Find("Mirror Vessel COM").transform.localEulerAngles;
        foreach (var limb in limbs)
        {
            var baseTarget = Instantiate(GameObject.Find("Base Target")).transform;
            baseTarget.SetParent(baseTargets);
            limb.SetBaseTarget(baseTarget);

            limb.ActivateIK();
        }
    }

    bool walking = false;
    void BeginWalkCycle()
    {
        groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointBack);
       // groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointHeight);
        groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointFront);
        groupLeft.limb0.RunGait();

        groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointFront);
        groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointBack);
     //   groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointHeight);
        groupLeft.limb1.RunGait();

        groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointBack);
       // groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointHeight);
        groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointFront);
        groupLeft.limb2.RunGait();


        groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointFront);  
        groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointBack);
      //  groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointHeight);
        groupRight.limb0.RunGait();

        groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointBack);
       // groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointHeight);
        groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointFront);
        groupRight.limb1.RunGait();

        groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointFront);      
        groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointBack);
       // groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointHeight);
        groupRight.limb2.RunGait();

        walking = true;
    }


    public float walkSpeed = 1;
    int walkCycle = 0;
    public void CustomUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            BeginWalkCycle();
        }
        //if (Input.GetKeyDown(KeyCode.Alpha8))
        //{
        //    Debug.Log(groupLeft.limbs.Count);
        //    groupLeft.limb0.SetGaitTarget(RoboticLimbIK.GaitTarget.FrontPoint);
        //    groupLeft.limb1.SetGaitTarget(RoboticLimbIK.GaitTarget.BackPoint);
        //    groupLeft.limb2.SetGaitTarget(RoboticLimbIK.GaitTarget.FrontPoint);
        //    groupRight.limb0.SetGaitTarget(RoboticLimbIK.GaitTarget.BackPoint);
        //    groupRight.limb1.SetGaitTarget(RoboticLimbIK.GaitTarget.FrontPoint);
        //    groupRight.limb2.SetGaitTarget(RoboticLimbIK.GaitTarget.BackPoint);
        //}

        if (walking)
        {
            List<bool> legsAtTarget = new List<bool>();

            foreach (var limb in limbs)
            {
                if (limb.MirrorLegAtTarget())
                {
                    // legsAtTarget = false;
                    // break;
                    legsAtTarget.Add(true);
                }
            }
            if (legsAtTarget.Count >= 6)
            {
                Debug.Log("all legs at target");
                walkCycle++;
                foreach (var limb in limbs)
                {
                    limb.NextGaitSeguence();
                }
            }

            if (walkCycle > 3 & walkSpeed == 1)
            {
                walkSpeed = 1.3f;
                Debug.Log("Update walk speed");
            }

            var dirError = directionTarget.eulerAngles.y - memoryBridge.vesselControl.mirrorVessel.vesselOffset.eulerAngles.y;
            var tempEuler = baseTargets.eulerAngles;
            tempEuler.y = memoryBridge.vesselControl.mirrorVessel.vesselOffset.eulerAngles.y;
            baseTargets.eulerAngles = tempEuler;

            //Debug.Log("Dir Error " + dirError);
        }


        foreach (var limb in limbs)
        {
            limb.CustomUpdate();
        }
    }
}

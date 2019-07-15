using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoboticController : MonoBehaviour
{
    IR_Manager IRmanager;
    public List<LimbController> limbs;

    struct HexapodLimbGroup { public List<LimbController> limbs; public LimbController limb0, limb1, limb2; public float rotStridePercent; public bool steeringAdjust; }

    HexapodLimbGroup groupLeft, groupRight;

    public List<LimbController> limbs0, limbs1;

    public Transform baseTargets, directionTarget;

    MemoryBridge memoryBridge;

    PidController steeringPID;

    public void CustomAwake(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;

        IRmanager = gameObject.GetComponent<IR_Manager>();
        IRmanager.CustomAwake(memoryBridge, memoryBridge.vesselControl, ref limbs);

        steeringPID = new PidController(.5f, 0, 0, 0, -.3f);
        steeringPID.SetPoint = 0;

        Debug.Log("IR Manager Enabled, Limb Count : " + limbs.Count);

        if (limbs.Count == 6)
        {
            Debug.Log("robot is a hexapod");
            groupLeft = new HexapodLimbGroup();
            groupLeft.limbs = new List<LimbController>();
            groupRight = new HexapodLimbGroup();
            groupRight.limbs = new List<LimbController>();

            limbs0 = new List<LimbController>();
            limbs1 = new List<LimbController>();


            foreach (var limb in limbs)
            {
                limb.roboticController = this;
                var offset = memoryBridge.vesselControl.vessel.transform.InverseTransformPoint(limb.transform.position);
                Debug.Log(limb.name);
                var group = groupRight;
                if (offset.x < 0)
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

                if (offset.x < 0)
                {
                    groupLeft = group;
                }
                else
                {
                    groupRight = group;
                }
            }

            foreach (var item in groupRight.limbs)
            {
                Debug.Log(item.name);
            }

            limbs0.Add(groupLeft.limb0);
            limbs0.Add(groupLeft.limb2);
            limbs0.Add(groupRight.limb1);

            limbs1.Add(groupRight.limb0);
            limbs1.Add(groupRight.limb2);
            limbs1.Add(groupLeft.limb1);

            foreach (var limb in groupLeft.limbs)
            {
                Debug.Log("Group Left Limb : " + limb.name);
            }
            foreach (var limb in groupRight.limbs)
            {
                Debug.Log("Group Right Limb : " + limb.name);
            }


        }
    }

    public UnityEngine.Events.UnityEvent IKactivaed;
    public bool robotActive = false;
    public void ActivateIK()
    {
        Debug.Log("activate Ik");
        robotActive = true;
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
            limb.ActivateIK();

            var baseTarget = Instantiate(GameObject.Find("Base Target")).transform;
            baseTarget.SetParent(baseTargets);
            limb.SetBaseTarget(baseTarget);
        }
        baseTargets.SetParent(directionTarget);
        IKactivaed.Invoke();
        DebugVector.DrawVector(directionTarget, DebugVector.Direction.z, 3, .1f, Color.red, Color.white, Color.green);
        DebugVector.DrawVector(memoryBridge.vesselControl.mirrorVessel.vesselOffset, DebugVector.Direction.z, 3, .1f, Color.red, Color.white, Color.blue);
    }

    bool walking = false;
    void BeginWalkCycle()
    {
        groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointBack);
        // groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointHeight);
        groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointFront);
        groupLeft.limb0.StartGait();

        groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointFront);
        groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointBack);
        //   groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointHeight);
        groupLeft.limb1.StartGait();

        groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointBack);
        // groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointHeight);
        groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointFront);
        groupLeft.limb2.StartGait();


        groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointFront);
        groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointBack);
        //  groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointHeight);
        groupRight.limb0.StartGait();

        groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointBack);
        // groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointHeight);
        groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointFront);
        groupRight.limb1.StartGait();

        groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointFront);
        groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointBack);
        // groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointHeight);
        groupRight.limb2.StartGait();

        walking = true;
    }

    public void SetBaseHeights(float newHeight)
    {
        var tempPos = baseTargets.localPosition;
        tempPos.y = newHeight;
        baseTargets.localPosition = tempPos;
        Debug.Log("Set base targets to height " + newHeight);
    }

    public float walkSpeed = 1;
    public float walkCycle { get; private set; } = 0;
    float atTargetTime = 0;

    public void CustomUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            BeginWalkCycle();
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ResetLimbPositions();
        }

        float globalYPos = 0;
        foreach (var limb in limbs)
        {
            limb.MirrorServos();
            limb.CheckClearance();
            globalYPos += limb.ground.position.y + 1.7f;
        }

        var baseHeight = globalYPos / 6;

        foreach (var limb in limbs)
        {
            //limb.SetBaseHeight(baseHeight);
        }

        if (robotActive)
        {
            foreach (var limb in limbs)
            {
                limb.SetGaitHeight();
            }
        }

        if (walking)
        {
            CheckCycleComplete();
            var tempEuler = baseTargets.eulerAngles;
            tempEuler.y = memoryBridge.vesselControl.mirrorVessel.vesselOffset.eulerAngles.y;
            baseTargets.eulerAngles = tempEuler;

            //find the stride percent average for the rotating limbs on each side of mech
            var stridePercents = new List<float>();
            
            foreach (var limb in groupLeft.limbs)
            {
                var stridePercent = limb.CalculateStridePercent();
                if (stridePercent != 300)
                    stridePercents.Add(stridePercent);
            }
            if (stridePercents.Count > 0)
            {
                groupLeft.rotStridePercent = stridePercents.Average();
            }
            else
            {
                groupLeft.rotStridePercent = 0;
                Debug.LogError("For some reason there is no rot limb + left");
            }

            stridePercents = new List<float>();
            foreach (var limb in groupRight.limbs)
            {
                var stridePercent = limb.CalculateStridePercent();
                if (stridePercent != 300)
                    stridePercents.Add(stridePercent);
            }
            if (stridePercents.Count > 0)
            {
                groupRight.rotStridePercent = stridePercents.Average();
            }
            else
            {
                Debug.LogError("For some reason there is no rot limb + right");
                groupRight.rotStridePercent = 0;
            }


            // run the gait for the limb, if the limb is in rotate mode, it requires the average stridepercent of all the limbs in rot mode
            foreach (var limb in groupLeft.limbs)
            {
                limb.RunGait(groupLeft.rotStridePercent);
            }
            foreach (var limb in groupRight.limbs)
            {
                limb.RunGait(groupRight.rotStridePercent);
            }


        }

        if (robotActive)
        {
            //set all the servos
            foreach (var limb in limbs)
            {
                limb.LimbUpdate();

            }
        }

    }

    void CheckCycleComplete()
    {
        List<bool> legsAtTarget = new List<bool>();

        var limbGroup = limbs0;
        if (limbs1[0].legMode == LimbController.LegMode.Rotate)
            limbGroup = limbs1;

        foreach (var limb in limbGroup)
        {
            if (limb.MirrorLegAtTarget())
            {
                legsAtTarget.Add(true);
            }
        }

        //  if (limbGroup.Count > 2)
        //     atTargetTime += Time.deltaTime;

        bool cycleComplete = false;
        if (legsAtTarget.Count >= 3)
        {
            cycleComplete = true;
        }
        if (atTargetTime > 100f)
        {
            cycleComplete = true;
            Debug.LogWarning("Time Kick");
        }

        //if the cycle is complete, update the stride for steering and set limb targets to next
        if (cycleComplete)
        {
            atTargetTime = 0;
            Debug.Log("NEXT WALK CYCLE");
            //  var dirError = directionTarget.eulerAngles.y - memoryBridge.vesselControl.mirrorVessel.vesselOffset.eulerAngles.y;
            // var angleError = Vector3.Angle(directionTarget.forward, memoryBridge.vesselControl.mirrorVessel.vesselOffset.forward);

            var forwardPoint = memoryBridge.vesselControl.mirrorVessel.vesselOffset.forward + new Vector3(0, 0, 10);
            var offset = directionTarget.InverseTransformPoint(forwardPoint);

            System.TimeSpan deltaTime = TimeSpan.FromSeconds(Time.fixedDeltaTime);
            steeringPID.ProcessVariable = offset.x;
            var strideAdjust = steeringPID.ControlVariable(TimeSpan.FromSeconds(10));
             Debug.Log("dir error " + offset.x);

            walkCycle++;
            foreach (var limb in limbs)
            {
                limb.NextGaitSeguence();
            }
            strideAdjust = -1f;
            if (Math.Abs(offset.x) > .3f)
            {
                if (offset.x < 0)
                {
                    // groupLeft.steeringAdjust = true;
                    // groupRight.steeringAdjust = false;
                     Debug.Log("Dir ErrorRight " + strideAdjust);
                    // Debug.Log("Shorten left stride : " + strideAdjust);
                    foreach (var limb in groupRight.limbs)
                    {
                       // if (limb.legMode == LimbController.LegMode.Translate)
                       //     limb.limbIK.UpdateStrideLength((float)(limb.limbIK.strideLength + strideAdjust));
                    }
                    foreach (var limb in groupLeft.limbs)
                    {
                         // limb.limbIK.DefaultStrideLength();
                    }
                }
                else
                {
                    // dirError -= 360;
                    // groupLeft.steeringAdjust = false;
                    // groupRight.steeringAdjust = true;
                     Debug.Log("Dir Error Left " + strideAdjust);
                    // Debug.Log("Shorten right stride : " + strideAdjust);
                    foreach (var limb in groupLeft.limbs)
                    {
                       // if (limb.legMode == LimbController.LegMode.Translate)
                       //     limb.limbIK.UpdateStrideLength((float)(limb.limbIK.strideLength + strideAdjust));
                    }
                    foreach (var limb in groupRight.limbs)
                    {
                       //  limb.limbIK.DefaultStrideLength();
                    }
                }
            }
            else
            {
                foreach (var limb in limbs)
                {
                    // limb.limbIK.DefaultStrideLength();
                }
            }

           
        }

    }

    void ResetLimbPositions()
    {

    }
}

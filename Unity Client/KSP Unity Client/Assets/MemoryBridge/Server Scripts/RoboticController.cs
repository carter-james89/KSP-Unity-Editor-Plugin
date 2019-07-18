using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoboticController : MonoBehaviour
{
    public enum RobotStatus { Deactivated, Idle, AdjustingGaitPosition, Walking }
    public RobotStatus robotStatus = RobotStatus.Deactivated;
    IR_Manager IRmanager;
    public List<LimbController> limbs;

    struct HexapodLimbGroup { public List<LimbController> limbs; public LimbController limb0, limb1, limb2; public float rotStridePercent; public bool steeringAdjust; }

    HexapodLimbGroup groupLeft, groupRight, group0, group1;

    // public List<LimbController> limbs0, limbs1;
    public Transform baseTargets, directionTarget;

    MemoryBridge memoryBridge;
    PidController steeringPID;

    public VesselControl vesselControl;

    public void CustomAwake(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;
        vesselControl = memoryBridge.vesselControl;

        IRmanager = gameObject.GetComponent<IR_Manager>();
        IRmanager.CustomAwake(memoryBridge, memoryBridge.vesselControl, ref limbs);

        steeringPID = new PidController(1, 0, .3f, .3, -.3f);
        steeringPID.SetPoint = 0;

        Debug.Log("IR Manager Enabled, Limb Count : " + limbs.Count);

        if (limbs.Count == 6)
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

            group0.limbs.Add(groupLeft.limb0);
            group0.limbs.Add(groupLeft.limb2);
            group0.limbs.Add(groupRight.limb1);

            group1.limbs.Add(groupRight.limb0);
            group1.limbs.Add(groupRight.limb2);
            group1.limbs.Add(groupLeft.limb1);

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
    public float gaitDistance;
    public UnityEngine.Events.UnityEvent IKactivaed;

    public void ActivateIK()
    {
        Debug.Log("activate Ik");
        robotStatus = RobotStatus.Idle;
        baseTargets = new GameObject("Base Targets").transform;
        baseTargets.SetParent(GameObject.Find("Vessel Offset").transform);
        baseTargets.localEulerAngles = Vector3.zero;
        baseTargets.SetParent(null);
        baseTargets.localPosition = Vector3.zero;

        directionTarget = new GameObject("Direction Target").transform;
        directionTarget.SetParent(GameObject.Find("Gimbal").transform);
        directionTarget.localEulerAngles = GameObject.Find("Mirror Vessel COM").transform.localEulerAngles;
        targetBaseHeight = directionTarget.position;
        foreach (var limb in limbs)
        {
            var baseTarget = Instantiate(GameObject.Find("Base Target")).transform;
            baseTarget.SetParent(baseTargets);
            limb.SetBaseTarget(baseTarget);

            limb.ActivateIK();
        }
        baseTargets.SetParent(directionTarget);
        IKactivaed.Invoke();
        DebugVector.DrawVector(directionTarget, DebugVector.Direction.z, 3, .1f, Color.red, Color.white, Color.green);
        DebugVector.DrawVector(memoryBridge.vesselControl.mirrorVessel.vesselOffset, DebugVector.Direction.z, 3, .1f, Color.red, Color.white, Color.blue);

        steeringPoint = new GameObject("steering point");
        DebugVector.DrawVector(steeringPoint.transform, DebugVector.Direction.all, 2, .1f, Color.red, Color.white, Color.blue);
        // Debug.LogError("");
    }


    void BeginWalkCycle()
    {
        Debug.Log("Begin Walking");
        groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointBack, LimbController.LegMode.Translate);
        // groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointHeight);
        groupLeft.limb0.AddGaitTarget(groupLeft.limb0.limbIK.pointFront, LimbController.LegMode.Rotate);
        groupLeft.limb0.StartGait();

        groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointFront, LimbController.LegMode.Rotate);
        groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointBack, LimbController.LegMode.Translate);
        //   groupLeft.limb1.AddGaitTarget(groupLeft.limb1.limbIK.pointHeight);
        groupLeft.limb1.StartGait();

        groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointBack, LimbController.LegMode.Translate);
        // groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointHeight);
        groupLeft.limb2.AddGaitTarget(groupLeft.limb2.limbIK.pointFront, LimbController.LegMode.Rotate);
        groupLeft.limb2.StartGait();


        groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointFront, LimbController.LegMode.Rotate);
        groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointBack, LimbController.LegMode.Translate);
        //  groupRight.limb0.AddGaitTarget(groupRight.limb0.limbIK.pointHeight);
        groupRight.limb0.StartGait();

        groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointBack, LimbController.LegMode.Translate);
        // groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointHeight);
        groupRight.limb1.AddGaitTarget(groupRight.limb1.limbIK.pointFront, LimbController.LegMode.Rotate);
        groupRight.limb1.StartGait();

        groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointFront, LimbController.LegMode.Rotate);
        groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointBack, LimbController.LegMode.Translate);
        // groupRight.limb2.AddGaitTarget(groupRight.limb2.limbIK.pointHeight);
        groupRight.limb2.StartGait();

        robotStatus = RobotStatus.Walking;
    }

    Vector3 targetBaseHeight;
    public void SetBaseHeights(float newHeight)
    {
        //var tempPos = baseTargets.localPosition;
        //tempPos.y = newHeight;
        //baseTargets.localPosition = tempPos;
        //Debug.Log("Set base targets to height " + newHeight);
        targetBaseHeight = directionTarget.position + new Vector3(0, newHeight, 0);
    }

    public float walkSpeed = 1;
    public float walkCycle { get; private set; } = 0;
    float atTargetTime = 0;

    float rotPercent = 0;

    bool movingGroup0 = false;

    GameObject steeringPoint;

    float simTime = 0;
    bool activateIK = false;
    bool moveGait = false;
    bool walk = false;
    public void CustomUpdate()
    {
        
        simTime += Time.deltaTime;
        if(simTime > 1 && !activateIK)
        {
            ActivateIK();
            activateIK = true;
        }
       else if(simTime > 3 && !moveGait)
        {
            Debug.Log("point mid ", group0.limbs[0].limbIK.pointMid.gameObject);
            group0.limbs[0].AddGaitTarget(group0.limbs[0].limbIK.pointMid, LimbController.LegMode.Rotate);
            group0.limbs[1].AddGaitTarget(group0.limbs[1].limbIK.pointMid, LimbController.LegMode.Rotate);
            group0.limbs[2].AddGaitTarget(group0.limbs[2].limbIK.pointMid, LimbController.LegMode.Rotate);

            group1.limbs[0].AddGaitTarget(group1.limbs[0].limbIK.pointMid, LimbController.LegMode.Rotate);
            group1.limbs[1].AddGaitTarget(group1.limbs[1].limbIK.pointMid, LimbController.LegMode.Rotate);
            group1.limbs[2].AddGaitTarget(group1.limbs[2].limbIK.pointMid, LimbController.LegMode.Rotate);

            group0.limbs[0].StartGait();
            group0.limbs[1].StartGait();
            group0.limbs[2].StartGait();

            movingGroup0 = true;
            moveGait = true;

            robotStatus = RobotStatus.AdjustingGaitPosition;
        }
        else if(simTime> 8 & !walk)
        {
            //foreach (var item in groupLeft.limbs)
            //{
            //    item.SetGaitRotation(10);
            //}
            walk = true;
            BeginWalkCycle();
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            BeginWalkCycle();
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ResetLimbPositions();
        }

        foreach (var limb in limbs)
        {
            //mirror the servos from ksp
            limb.MirrorServos();
            //calculate the ground position
            limb.CheckClearance();
        }

        if (robotStatus != RobotStatus.Deactivated)
        {
            if (directionTarget.position != targetBaseHeight)
            {
                directionTarget.position = Vector3.Lerp(directionTarget.position, targetBaseHeight, Time.deltaTime);
                if (Vector3.Distance(directionTarget.position, targetBaseHeight) < .02f)
                {
                    directionTarget.position = targetBaseHeight;
                }
            }
            foreach (var limb in limbs)
            {
                //mirror the servos from ksp
                // limb.MirrorServos();
                //calculate the ground position
                // limb.CheckClearance();
                //set the gait height according to baseOffset
                limb.SetGaitHeight();
            }

            if (robotStatus == RobotStatus.AdjustingGaitPosition)
            {
                bool group0AtTarget = true;
                if (movingGroup0)
                {
                    foreach (var limb in group0.limbs)
                    {
                        limb.RunGait(rotPercent += Time.deltaTime/4);
                        var atTarget = limb.MirrorLegAtTarget();
                        if (!atTarget)
                            group0AtTarget = false;
                    }
                    if (group0AtTarget && movingGroup0)
                    {
                        movingGroup0 = false;
                        group1.limbs[0].StartGait();
                        group1.limbs[1].StartGait();
                        group1.limbs[2].StartGait();
                        rotPercent = 0;
                    }
                }
                else
                {
                    foreach (var limb in group1.limbs)
                    {
                        limb.RunGait(rotPercent += Time.deltaTime / 4);
                        var atTarget = limb.MirrorLegAtTarget();
                        if (!atTarget)
                            group0AtTarget = false;
                    }

                    if (group0AtTarget)
                    {
                        // directionTarget.position += new Vector3(0, .2f, 0);
                        SetBaseHeights((directionTarget.position + new Vector3(0, .2f, 0)).y);
                        foreach (var limb in limbs)
                        {
                            limb.ResetGaitSequence();
                        }
                        robotStatus = RobotStatus.Idle;
                    }                    
                }
                // group0.limbs[0].RunGait(rotPercent += Time.deltaTime / 30);
            }

            if (robotStatus == RobotStatus.Walking)
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

            //runs the ik and sets ther memory bridge servo values
            foreach (var limb in limbs)
            {
                limb.LimbUpdate();
            }
        }
    }

    float strideTime = 0;
    public float steeringError;
    void CheckCycleComplete()
    {
        List<bool> legsAtTarget = new List<bool>();
        foreach (var limb in limbs)
        {
            if (limb.MirrorLegAtTarget() && limb.legMode == LimbController.LegMode.Rotate)
            {
                legsAtTarget.Add(true);
            }
        }

        strideTime += Time.deltaTime;
        bool cycleComplete = false;
        if (legsAtTarget.Count >= 3)
        {
            cycleComplete = true;
        }
        //if (atTargetTime > 1f)
        //{
        //    cycleComplete = true;
        //    Debug.LogWarning("Time Kick");
        //}

        //if the cycle is complete, update the stride for steering and set limb targets to next
        if (cycleComplete)
        {
            atTargetTime = 0;
            Debug.Log("NEXT WALK CYCLE");
            //  var dirError = directionTarget.eulerAngles.y - memoryBridge.vesselControl.mirrorVessel.vesselOffset.eulerAngles.y;
            // var angleError = Vector3.Angle(directionTarget.forward, memoryBridge.vesselControl.mirrorVessel.vesselOffset.forward);

           // limbEndPoint.position + (newTransform.position - limbEndPoint.position) / 2

            var forwardPoint = memoryBridge.vesselControl.mirrorVessel.vesselOffset.position + (memoryBridge.vesselControl.mirrorVessel.vesselOffset.forward.normalized * 3);
            steeringPoint.transform.position = forwardPoint;
            var offset = directionTarget.InverseTransformPoint(forwardPoint);

            steeringError = offset.x;

          
            walkCycle++;
            foreach (var limb in limbs)
            {
                limb.NextGaitSeguence();
            }
          //  strideAdjust = -.3f;
            if (Math.Abs(steeringError) > .1f)
            {
                System.TimeSpan deltaTime = TimeSpan.FromSeconds(strideTime);// new TimeSpan(0, 0, (int)strideTime);         
                steeringPID.ProcessVariable = steeringError;
                var strideAdjust = steeringPID.ControlVariable(deltaTime);

                if (offset.x < 0)
                {
                    // groupLeft.steeringAdjust = true;
                    // groupRight.steeringAdjust = false;
                    Debug.Log("Dir ErrorRight " + offset.x);
                     Debug.Log("Shorten left stride : " + strideAdjust);
                    foreach (var limb in groupRight.limbs)
                    {
                         if (limb.legMode == LimbController.LegMode.Translate)
                             limb.limbIK.UpdateStrideLength((float)(limb.limbIK.strideLength - strideAdjust));
                    }
                    foreach (var limb in groupLeft.limbs)
                    {
                         limb.limbIK.DefaultStrideLength();
                    }
                }
                else
                {
                    // dirError -= 360;
                    // groupLeft.steeringAdjust = false;
                    // groupRight.steeringAdjust = true;
                    Debug.Log("Dir Error Left " + offset.x);
                    Debug.Log("Shorten right stride : " + strideAdjust);
                    foreach (var limb in groupLeft.limbs)
                    {
                         if (limb.legMode == LimbController.LegMode.Translate)
                             limb.limbIK.UpdateStrideLength((float)(limb.limbIK.strideLength + strideAdjust));
                    }
                    foreach (var limb in groupRight.limbs)
                    {
                         limb.limbIK.DefaultStrideLength();
                    }
                }
            }
            else
            {
                foreach (var limb in limbs)
                {
                     limb.limbIK.DefaultStrideLength();
                }
            }
            strideTime = 0;
        }
    }

    void ResetLimbPositions()
    {

    }
}

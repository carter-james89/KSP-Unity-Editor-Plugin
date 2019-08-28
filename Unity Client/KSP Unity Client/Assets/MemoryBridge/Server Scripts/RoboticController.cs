using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using System.IO;
using System.Text;
using Winterdom.IO.FileMap;

public class RoboticController : MonoBehaviour
{
    MemoryMappedFile IRFile;

    public enum RobotStatus { Deactivated, Idle, AdjustingGaitPosition, Walking }
    public RobotStatus robotStatus = RobotStatus.Deactivated;
   // IR_Manager IRmanager;
    public List<LimbController> limbs;
    public List<LegController> legs;

    public struct HexapodLimbGroup { public List<LimbController> limbs; public LimbController limb0, limb1, limb2; public float rotStridePercent; public bool steeringAdjust; }

    public HexapodLimbGroup groupLeft, groupRight, group0, group1;

    // public List<LimbController> limbs0, limbs1;
    public Transform baseTargets, directionTarget;

    MemoryBridge memoryBridge;
    PidController steeringPID;

    float footClearance = 0;
    public bool groundContact;

    public float strideLength = 2;

    public enum GaitType { Arc, Graph }
    public GaitType gaitType;

    public AnimationCurve gaitCurve = AnimationCurve.EaseInOut(-1, 0, 1, 0);

    public VesselControl vesselControl;

    protected List<GameObject> limbObjects;

    //public AnimationCurve myCurve;

    void ReadLegsFromBridge()
    {
        IRFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "IRFile" + memoryBridge.fileName);
        limbs = new List<LimbController>();
        float byteCount;
        float limbCount;
        using (Stream mapStream = IRFile.MapView(MapAccess.FileMapAllAccess, 0, 16))
        {
            var floatBuffer = new byte[4];

            mapStream.Read(floatBuffer, 0, 4);
            byteCount = BitConverter.ToInt32(floatBuffer, 0);
            mapStream.Read(floatBuffer, 0, 4);
            limbCount = BitConverter.ToInt32(floatBuffer, 0);
        }
        Debug.Log("vessel has limbs " + limbCount);
        limbObjects = new List<GameObject>();
        using (Stream mapStream = IRFile.MapView(MapAccess.FileMapAllAccess, 0, (int)byteCount))
        {
            mapStream.Position = 16;

            for (int i = 0; i < limbCount; i++)
            {
                var floatBuffer = new byte[4];
                mapStream.Read(floatBuffer, 0, 4);
                var stringByteLength = BitConverter.ToSingle(floatBuffer, 0);

                var stringBuffer = new byte[(int)stringByteLength];
                mapStream.Read(stringBuffer, 0, stringBuffer.Length);
                string limbName = ASCIIEncoding.ASCII.GetString(stringBuffer);

                var newLimbObject = new GameObject();
                newLimbObject.name = limbName;
                newLimbObject.transform.SetParent(vesselControl.vessel.transform);

                limbObjects.Add(newLimbObject);
                //if (limbName.ToLower().Contains("leg"))
                //{
                //    newLimb = newLimbObject.AddComponent<LegController>();

                //}
                //else
                //{
                //    newLimb = newLimbObject.AddComponent(typeof(LimbController)) as LimbController;
                //}

                //newLimb.CustomAwake(memoryBridge, limbName, vesselControl);
                //limbs.Add(newLimb);
                //  Debug.Log("Add limb " + limbName);
            }
        }
    }

    //protected void 
    public virtual void CustomAwake(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;
        vesselControl = memoryBridge.vesselControl;

        ReadLegsFromBridge();
        // IRmanager = gameObject.GetComponent<IR_Manager>();
        //convert IR parts on this vessel to robotic servos and create limbcontroller/limbs
        // IRmanager.CustomAwake(memoryBridge, memoryBridge.vesselControl, ref limbs);

        //legs = new List<LegController>();
        //foreach (var limb in limbs)
        //{
        //    limb.roboticController = this;
        //    if (limb.GetType() == typeof(LegController))
        //    {
        //        legs.Add(limb as LegController);
        //    }
        //}

        steeringPID = new PidController(1, 0, .3f, .3, -.3f);
        steeringPID.SetPoint = 0;

        Debug.Log("IR Manager Enabled, Limb Count : " + limbs.Count);
    }
    public float gaitDistance;
    public UnityEngine.Events.UnityEvent IKactivaed;

    public virtual void ActivateIK()
    {
        Debug.Log("activate Ik");
        robotStatus = RobotStatus.Idle;
        baseTargets = new GameObject("Base Targets").transform;
        baseTargets.SetParent(GameObject.Find("Vessel Offset").transform);
        baseTargets.localEulerAngles = Vector3.zero;

        Vector3 baseOffset = Vector3.zero;
        foreach (var leg in legs)
        {
            baseOffset += vesselControl.adjustedGimbal.InverseTransformPoint(leg.servoBase.transform.position);
        }
        baseOffset /= legs.Count;
        baseTargets.transform.position = baseOffset;
        SetBaseHeights(baseTargets.position.y - vesselControl.ground.position.y, false);

        baseTargets.SetParent(null);
       // baseTargets.localPosition = Vector3.zero;

        directionTarget = new GameObject("Direction Target").transform;
        directionTarget.SetParent(GameObject.Find("Gimbal").transform);
        directionTarget.localEulerAngles = GameObject.Find("Mirror Vessel COM").transform.localEulerAngles;
    
        foreach (var leg in legs)
        {
            var baseTarget = Instantiate(GameObject.Find("Base Target")).transform;
            baseTarget.SetParent(baseTargets);
            leg.SetBaseTarget(baseTarget);
            leg.ActivateIK(true);
        }
       // baseTargets.SetParent(directionTarget);
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

    float targetBaseHeight;
    public float walkHeight = 1;
    public void SetBaseHeights(float newHeight, bool lerp = true)
    {
            Debug.Log("______________Adjust base height");
            targetBaseHeight = newHeight;
        if(!lerp)
        {
            baseHeight = newHeight;
        }
    }

    public float walkSpeed = 1;
    public float walkCycle { get; private set; } = 0;
    float atTargetTime = 0;

    float rotPercent = 0;

    bool movingGroup0 = false;

    GameObject steeringPoint;

    protected float baseHeight;

    public bool writeServoToBridge = true;

    float simTime = 0;
    bool activateIK = false;
    bool moveGait = false;
    bool walk = false;
    public bool autoStartUp;
    public virtual void CustomUpdate()
    {
        if (autoStartUp)
        {
            simTime += Time.deltaTime;
            if (simTime > 1 && !activateIK)
            {
                ActivateIK();
                activateIK = true;
            }
            else if (simTime > 3 && !moveGait)
            {
                MoveGaitToStartPosition();
            }
            else if (simTime > 8 & !walk)
            {
                //foreach (var item in groupLeft.limbs)
                //{
                //    item.SetGaitRotation(10);
                //}
                walk = true;
                BeginWalkCycle();
            }
        }
    
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ActivateIK();
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            MoveGaitToStartPosition();
            //BeginWalkCycle();
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            BeginWalkCycle();
        }

        foreach (var limb in limbs)
        {
            //mirror the servos from ksp
            limb.MirrorServos();
        }
        foreach (var leg in legs)
        {
            //calculate the ground position
            leg.CheckClearance();
        }

        if (robotStatus != RobotStatus.Deactivated)
        {
            //set robot walk height
            if (targetBaseHeight != baseHeight)
            {
                baseHeight = Mathf.Lerp(baseHeight, targetBaseHeight, Time.deltaTime);
                if (Math.Abs(targetBaseHeight - baseHeight) < .02f)
                {
                    baseHeight = targetBaseHeight;
                }
            }
            baseTargets.transform.position = vesselControl.ground.position + new Vector3(0, baseHeight, 0);
            //Set gait height
            foreach (var leg in legs)
            {
                leg.SetGaitHeight();
            }

            if (robotStatus == RobotStatus.AdjustingGaitPosition)
            {
                bool group0AtTarget = true;
                if (movingGroup0)
                {
                    foreach (var limb in group0.limbs)
                    {
                        limb.RunGait(rotPercent += Time.deltaTime / 4);
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
                        SetBaseHeights(walkHeight);
                      //  SetBaseHeights((directionTarget.position + new Vector3(0, .2f, 0)).y);
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
            foreach (var limb in legs)
            {
                limb.LimbUpdate();
            }
        }
    }

    void MoveGaitToStartPosition()
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
            foreach (var limb in legs)
            {
                limb.NextGaitSeguence();
            }
            //  strideAdjust = -.3f;
            if (Math.Abs(steeringError) > .05f)
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
                foreach (var limb in legs)
                {
                    limb.limbIK.DefaultStrideLength();
                }
            }
            strideTime = 0;
        }
    }

    protected virtual void OnDestroy()
    {
        if (IRFile != null)
        {
            IRFile.Dispose();
            IRFile.Close();
        }
    }

    void ResetLimbPositions()
    {

    }
}

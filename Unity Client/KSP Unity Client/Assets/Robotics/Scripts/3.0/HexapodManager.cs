using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexapodManager : RobotManager
{
    MemoryBridge memoryBridge;
    ServoLimb[] limbs;

    List<ServoLimb> legs;

    ServoLimb neck;

    bool moveGait = false;
    bool movingGroup0 = false;

    float rotPercent = 0;

    Vector3 neckGaitStartPos;

    public struct HexapodLimbGroup { public List<ServoLimb> limbs; public ServoLimb limb0, limb1, limb2; public float rotStridePercent; public bool steeringAdjust; }
    public HexapodLimbGroup groupLeft, groupRight, group0, group1;

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
                // limb.CreateGait(true, gaitCurve);
            }
            else
            {
                neck = limb;
                // limb.CreateGait(false, gaitCurve);
            }
        }
        neckGaitStartPos = neck.limbEndPointIK.position;

        PopulateLimbGroups();
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

        if(Time.frameCount > 20 && robotStatus == RobotStatus.Deactivated)
        {
            ActivateIK();
        }

        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            MoveGaitToStartPosition();
        }

        if (robotStatus != RobotStatus.Deactivated)
        {
            RunIK();

            if(robotStatus == RobotStatus.AdjustingGaitPosition)
            {
                bool group0AtTarget = true;
                if (movingGroup0)
                {
                    foreach (var limb in group0.limbs)
                    {
                        limb.gait.RunGait(rotPercent += Time.deltaTime / 4);
                        var atTarget = limb.MirrorLimbAtTarget();
                        if (!atTarget)
                            group0AtTarget = false;
                    }
                    if (group0AtTarget && movingGroup0)
                    {
                        movingGroup0 = false;
                        //group1.limbs[0].StartGait();
                        //group1.limbs[1].StartGait();
                        //group1.limbs[2].StartGait();
                        //rotPercent = 0;
                    }
                }
            }
        }
    }

    void ActivateIK()
    {
        robotStatus = RobotStatus.Idle;
        foreach (var limb in legs)
        {
            limb.ActivateIK();
            limb.CreateGait(true, gaitCurve);
        }
        neck.ActivateIK();
        neck.CreateGait(false, gaitCurve);
        neck.gait.IKtargetTransform.position = neckGaitStartPos;
    }
    void MoveGaitToStartPosition()
    {
        //Debug.Log("point mid ", group0.limbs[0].limbIK.pointMid.gameObject);
        group0.limbs[0].gait.AddGaitTarget(group0.limbs[0].gait.pointMid, Gait.MovementType.Rotate);
        group0.limbs[1].gait.AddGaitTarget(group0.limbs[1].gait.pointMid, Gait.MovementType.Rotate);
        group0.limbs[2].gait.AddGaitTarget(group0.limbs[2].gait.pointMid, Gait.MovementType.Rotate);

        group1.limbs[0].gait.AddGaitTarget(group1.limbs[0].gait.pointMid, Gait.MovementType.Rotate);
        group1.limbs[1].gait.AddGaitTarget(group1.limbs[1].gait.pointMid, Gait.MovementType.Rotate);
        group1.limbs[2].gait.AddGaitTarget(group1.limbs[2].gait.pointMid, Gait.MovementType.Rotate);

        group0.limbs[0].gait.StartGait();
        group0.limbs[1].gait.StartGait();
        group0.limbs[2].gait.StartGait();

        movingGroup0 = true;
        moveGait = true;

        robotStatus = RobotStatus.AdjustingGaitPosition;
    }

    void RunIK()
    {
        foreach (var limb in legs)
        {
            limb.SetGroundPos();

        }
        foreach (var limb in legs)
        {
            CalculateTwoServoIK(limb.zAxisServos[0], limb.zAxisServos[1], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
            CalculateSingleServoIK(limb.xAxisServos[0], limb.gait.IKtargetTransform.position, limb.limbEndPointIK);
        }
        CalculateTwoServoIK(neck.zAxisServos[0], neck.zAxisServos[2], neck.gait.IKtargetTransform.position, neck.limbEndPointIK);
        CalculateSingleServoIK(neck.xAxisServos[0], neck.gait.IKtargetTransform.position, neck.limbEndPointIK);
        CalculateSingleServoIK(neck.zAxisServos[3], neck.gait.IKtargetTransform.position, neck.limbEndPointIK);
        //neck.zAxisServos[3].SetServo(neck.zAxisServos[2].setAngle);
    }

    void PopulateLimbGroups()
    {
        groupLeft = new HexapodLimbGroup();
        groupLeft.limbs = new List<ServoLimb>();
        groupRight = new HexapodLimbGroup();
        groupRight.limbs = new List<ServoLimb>();

        group0 = new HexapodLimbGroup();
        group0.limbs = new List<ServoLimb>();
        group1 = new HexapodLimbGroup();
        group1.limbs = new List<ServoLimb>();

        foreach (var leg in legs)
        {
            var offset = memoryBridge.vesselControl.vessel.transform.InverseTransformPoint(leg.transform.position);
            var group = groupRight;
            if (offset.x < 0)
            {
                group = groupLeft;
            }

            group.limbs.Add(leg);
            if (leg.name.Contains("1"))
            {
                group.limb0 = leg;
            }
            if (leg.name.Contains("2"))
            {
                group.limb1 = leg;
            }
            if (leg.name.Contains("3"))
            {
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
        group0.limbs.Add(groupLeft.limb0);
        group0.limbs.Add(groupLeft.limb2);
        group0.limbs.Add(groupRight.limb1);

        group1.limbs.Add(groupRight.limb0);
        group1.limbs.Add(groupRight.limb2);
        group1.limbs.Add(groupLeft.limb1);
    }
}

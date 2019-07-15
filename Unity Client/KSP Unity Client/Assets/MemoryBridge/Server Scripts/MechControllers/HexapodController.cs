using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSPMechs;

public class HexapodController : MechManager
{
    public float baseHeights = 0;
    public bool useServoPID;

    public float hipRotPID_P, hipRotPID_I, hipRotPID_D, hipRotPID_Max, hipRotPID_Min;
    public float hipElvPID_P, hipElvPID_I, hipElvPID_D, hipElvPID_Max, hipElvPID_Min;
    public float kneePID_P, kneePID_I, kneePID_D, kneePID_Max, kneePID_Min;

    RoboticController roboController;

    public float strideLength = 1.7f;
    public float baseLerpSpeed = 2;

    public float walkSpeed = 2;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        var roboticLimbs = FindObjectsOfType<RoboticLimbIK>();
        foreach (var limb in roboticLimbs)
        {
            limb.IKAxisX.servo0.UpdatePIDValue(kneePID_P, kneePID_I, kneePID_D, kneePID_Min, kneePID_Max);
            limb.IKAxisX.servo1.UpdatePIDValue(hipElvPID_P, hipElvPID_I, hipElvPID_D, hipElvPID_Min, hipElvPID_Max);

            limb.IKAxisY.servo0.UpdatePIDValue(hipRotPID_P, hipRotPID_I, hipRotPID_D, hipRotPID_Min, hipRotPID_Max);

            limb.strideLength = strideLength;
        }

        roboController = FindObjectOfType<RoboticController>();
        roboController.IKactivaed.AddListener(ONIKActivated);
        roboController.walkSpeed = walkSpeed;

        foreach (var servo in FindObjectsOfType<RoboticServoIK>())
        {
            servo.useServoSpeedPID = useServoPID;
        }
        foreach (var limb in FindObjectsOfType<LimbController>())
        {
            limb.baseLerpSpeed = baseLerpSpeed;
        }     
    }
    public float hipAngle;
    protected override void Update()
    {
        base.Update();

        //if (Input.GetKeyDown(KeyCode.Keypad7))
        //{
        //    Debug.Log("7");
        //    FindObjectOfType<VesselControl>().ToggleAutoPilot(true);

        //    FindObjectOfType<MemoryBridge>().SetFloat("yo11", Time.deltaTime);
        //}

       // FindObjectOfType<DroneManager>().CustomUpdate();

        //if (roboController.robotActive)
        //{

        //    var roboticLimbs = FindObjectsOfType<RoboticLimbIK>();
        //    foreach (var limb in roboticLimbs)
        //    {
        //        limb.IKAxisY.servo0.SetOffset(hipAngle);
        //    }
        //}

    }

    void ONIKActivated()
    {
        roboController.SetBaseHeights(baseHeights);
    }
}


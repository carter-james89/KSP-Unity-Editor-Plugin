using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSPMechs;

public class HexapodController : MechManager
{
    public float hipRotPID_P, hipRotPID_I, hipRotPID_D, hipRotPID_Max, hipRotPID_Min;
    public float hipElvPID_P, hipElvPID_I, hipElvPID_D, hipElvPID_Max, hipElvPID_Min;
    public float kneePID_P, kneePID_I, kneePID_D, kneePID_Max, kneePID_Min;

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
        }
    }
}


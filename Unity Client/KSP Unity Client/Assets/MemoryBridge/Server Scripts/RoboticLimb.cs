using System.Collections;
using System.Collections.Generic;
using Winterdom.IO.FileMap;
using UnityEngine;
using System.IO;
using System;
using System.Text;

public class RoboticLimb : MonoBehaviour
{
    public RoboticServo[] servos;   
    public Part[] limbParts;
    public enum LimbPart { Base, Wrist, Middle };
    
    public RoboticServo servoWrist, servoBase;
    
    public LimbController limbController;

    public Transform limbEndPoint;

    // public Transform limbEnd;

    private void Awake()
    {
        limbParts = gameObject.GetComponentsInChildren<Part>();
        servos = gameObject.GetComponentsInChildren<RoboticServo>();
    }
    public virtual void CustomStart(LimbController limbController)
    {
        this.limbController = limbController;
    }

    public void SetStartAngles()
    {
        Debug.Log("Set start pos at limb");
        foreach (var servo in servos)
        {
            servo.SetStartAngle();
        }
    }

    public void FindEndPoint()
    {
        limbEndPoint = servoWrist.CalculateTarget().transform;
        //foreach (var servo in servos)
        //{
        //    servo.groundPoint = limbEnd;
        //}
    }

    public void SetLimbReference()
    {
        foreach (var servo in servos)
        {
            servo.limb = this;
            if (servo.limbControllerPart == LimbPart.Base)
                servoBase = servo;
            else if (servo.limbControllerPart == LimbPart.Wrist)
                servoWrist = servo;
        }
    }

    public Part FindServo(int partID)
    {
        Part returnPart = null;
        foreach (var part in limbParts)
        {
            if (part.ID == partID)
                returnPart = part;
        }
        return returnPart;
    }

    public void CalculateGroups()
    {
        if (servos != null)
        {
            foreach (var servo in servos)
            {
                servo.CalculateGroupAngle();
            }
        }
    }
    public void DisableParts()
    {
        foreach (var part in limbParts)
        {
            part.ToggleRenderers(false);
        }
    }
}

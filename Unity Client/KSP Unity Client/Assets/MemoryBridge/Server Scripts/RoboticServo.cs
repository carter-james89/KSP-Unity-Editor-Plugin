using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticServo : MonoBehaviour
{
    public string servoName;

    public RoboticServo servoParent, servoChild;

    public bool disabled = false;

    public RoboticLimb.LimbPart limbControllerPart = RoboticLimb.LimbPart.Middle;

    public Transform servoBase;

    public LimbController.LimbAxis limbAxis;
    float rotDifX, rotDifY, rotDifZ;
    float negRotDifX, negRotDifY, negRotDifZ;

    public MemoryBridge memoryBridge;

    LineRenderer lineRenderer;

    Color color;

    public Part hostPart;
    public LimbController limbController;
    public RoboticLimb limb;

    public float targetOffset;

    public float limbLength;

    public float limitMin, limitMax;

    public bool invert = false;

    public virtual void CustomStart(string servoName, MemoryBridge memoryBridge, LimbController limbController, int parentID)
    {
        this.servoName = servoName;
        gameObject.name = servoName;
        this.limbController = limbController;

        this.memoryBridge = memoryBridge;

        limitMin = memoryBridge.GetFloat(servoName + "minPos");
        limitMax = memoryBridge.GetFloat(servoName + "maxPos");
        // Debug.Log(limitMax);

        if (servoName.ToLower().Contains("base"))
        {
            Debug.Log("Let limb base");
            limbControllerPart = RoboticLimb.LimbPart.Base;
            limbController.servoBase = this;
        }
        else if (servoName.ToLower().Contains("wrist"))
        {
            limbControllerPart = RoboticLimb.LimbPart.Wrist;
        }

        if (servoName.ToLower().Contains("reverse"))
        {
            invert = true;
        }

        hostPart = GetComponent<Part>();
        hostPart.ServoAdded(this);

        lineRenderer = hostPart.lineRenderer;

        foreach (var part in hostPart.vessel.parts)
        {
            if (part.ID == parentID)
            {
                servoParent = part.servo;
                servoParent.SetChild(this);
            }
        }

        if (hostPart.name.ToLower().Contains("skip"))
        {
            DisableServo(true);
            Debug.Log(name + " disabled");
        }

    }

    public virtual void CustomUpdate()
    {

    }

    public virtual void CreateBaseAnchor()
    {
        //create anchor base
        servoBase = new GameObject().transform;
        servoBase.name = "Servo Base";
        servoBase.SetParent(transform);
        //servoBase.localEulerAngles = Vector3.zero;
        servoBase.localPosition = Vector3.zero;
        // servoBase.localEulerAngles -= new Vector3(kspStartAngle, 0, 0);
        servoBase.localRotation = Quaternion.Inverse(memoryBridge.GetQuaternion(servoName + "servoLocalRot"));
        servoBase.SetParent(transform.parent);
        transform.SetParent(servoBase);
        //servoBase.SetParent(transform.parent);
        //servoBase.SetParent(transform);

        transform.localEulerAngles = Vector3.zero;


        //works
        //servoBase.SetParent(transform.parent);
        //servoBase.rotation = transform.parent.rotation;
        //servoBase.localPosition = transform.localPosition;
        //servoBase.name = servoName + " base";

        //DebugVector.DrawVector(servoBase, DebugVector.Direction.all, .5f, .05f, Color.red, Color.green, Color.blue);

        //Vector3 lookAtPoint = Vector3.zero;
        //if (hostPart.kspPartName == "IR.Rotatron.Basic")
        //    servoBase.localEulerAngles = new Vector3(0, 90, 90);
        //lookAtPoint = servoBase.position + servoBase.rotation * (new Vector3(0, 1, 0));
        //servoBase.LookAt(lookAtPoint, Vector3.up);
    }

    public GameObject CalculateTarget(bool andWrite, string name)
    {
        var worldVertPositions = new List<Vector3>();
        var offsets = new List<Vector3>();
        var childFilters = GetComponentsInChildren<MeshFilter>();
        foreach (var meshFilter in childFilters)
        {
            var vertices = meshFilter.mesh.vertices;
            foreach (var vertex in vertices)
            {
                var worldPos = meshFilter.transform.TransformPoint(vertex);
                worldVertPositions.Add(worldPos);
            }
            //meshFilter.gameObject.SetActive(false);
        }
        foreach (var worldPos in worldVertPositions)
        {
            var offset = transform.InverseTransformPoint(worldPos);
            offsets.Add(offset);
        }
        float furthestY = 0;
        Vector3 footOffset = Vector3.zero;
        for (int i = 0; i < offsets.Count; i++)
        {
            if (offsets[i].y > furthestY)
            {
                furthestY = offsets[i].y;
                footOffset = offsets[i];
            }
        }
        Debug.Log("offset " + name + " " + footOffset, gameObject);
        var foot = Instantiate(Resources.Load("Foot", typeof(GameObject))) as GameObject;
        foot.name = "True End Point " + name;
        foot.transform.SetParent(transform);
        foot.GetComponent<MeshRenderer>().material.color = color;
        // footOffset.y = -footOffset.y;
        foot.transform.localPosition = footOffset;
        //  foot.transform.position = transform.InverseTransformPoint(memoryBridge.GetVector3(servoName + "CollisionPoint"));

        if (andWrite)
            memoryBridge.SetVector3(servoName + "contactPoint", footOffset);

        targetOffset = (float)(Math.Atan2(footOffset.z, footOffset.y));

        targetOffset *= (float)(180 / Math.PI);
        //targetOffset = Vector3.Angle(transform.position, foot.transform.position);
        // Debug.Log("target offset " + targetOffset);

        var footRenderer = foot.GetComponent<LineRenderer>();
        footRenderer.SetPosition(0, Vector3.zero);
        var wristOffset = foot.transform.InverseTransformPoint(transform.position);
        footRenderer.SetPosition(1, wristOffset);
        footRenderer.material.color = color;




        //  limbLength = Vector3.Distance(transform.position, foot.transform.position);
        // groundPoint = foot.transform;
        // limbController.contactPoint = groundPoint;
        return foot;
    }
    // public Transform groundPoint;

    public void Clone(RoboticServo servoToClone)
    {
        servoToClone.servoName = servoName;
        servoToClone.servoParent = servoParent;
        servoToClone.servoChild = servoChild;
        servoToClone.limbControllerPart = limbControllerPart;
        servoToClone.servoBase = servoBase;
        servoToClone.limitMin = limitMin;
        servoToClone.limitMax = limitMax;
        // servoToClone.targetOffset = targetOffset;
        //servoToClone.memoryBridge = memoryBridge;
        servoToClone.limbAxis = limbAxis;
        servoToClone.lineRenderer = lineRenderer;
        servoToClone.color = color;
        servoToClone.hostPart = hostPart;
        //servoToClone.limbController = limbController;
        servoToClone.disabled = disabled;
        //  servoToClone.invert = invert;
    }
    //called from robotic Limb
    public void CalculateGroupAngle()
    {
        var angleList = new List<float>();
        // if (servoName.Contains("Ro"))
        rotDifX = Vector3.Angle(limbController.transform.right, transform.right);
        negRotDifX = Vector3.Angle(limbController.transform.right, -transform.right);
        rotDifY = Vector3.Angle(limbController.transform.up, transform.right);
        negRotDifY = Vector3.Angle(limbController.transform.up, -transform.right);
        rotDifZ = Vector3.Angle(limbController.transform.forward, transform.right);
        negRotDifZ = Vector3.Angle(limbController.transform.forward, -transform.right);
        angleList.Add(rotDifX);
        angleList.Add(negRotDifX);
        angleList.Add(rotDifY);
        angleList.Add(negRotDifY);
        angleList.Add(rotDifZ);
        angleList.Add(negRotDifZ);

        float smallestAngle = 361;

        foreach (var angle in angleList)
        {
            if (angle < smallestAngle)
                smallestAngle = angle;
        }

        if (smallestAngle == rotDifX || smallestAngle == negRotDifX)
            limbAxis = LimbController.LimbAxis.X;
        else if (smallestAngle == rotDifY || smallestAngle == negRotDifY)
            limbAxis = LimbController.LimbAxis.Y;
        else if (smallestAngle == rotDifZ || smallestAngle == negRotDifZ)
            limbAxis = LimbController.LimbAxis.Z;

        color = Color.blue;
        switch (limbAxis)
        {
            case LimbController.LimbAxis.X:
                color = Color.red;
                break;
            case LimbController.LimbAxis.Y:
                color = Color.green;
                break;
            case LimbController.LimbAxis.Z:
                color = Color.blue;
                break;
        }

        if (disabled)
        {
            Debug.Log("servo disabled at sort");
            color = Color.white;
        }

        hostPart.SetJointColor(color);

        //Draw line renderer
        if (servoChild)
        {
            Material mat = new Material(Shader.Find("Diffuse"));
            if (lineRenderer)
            {
                var offsetFromParent = lineRenderer.transform.InverseTransformPoint(servoChild.transform.position);
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, offsetFromParent);
                mat.color = color;
                lineRenderer.material = mat;
            }
        }
    }

    public void DisableServo(bool disable)
    {
        disabled = disable;
        Debug.Log("d");

        if (disable)
        {
            Debug.Log("disable " + name);
            //   hostPart.lineRenderer.material.color = Color.red;
        }
    }


    public void SetChild(RoboticServo servo)
    {
        limbLength = Vector3.Distance(transform.position, servo.transform.position);
        servoChild = servo;
    }

    public virtual void SetStartAngle() { }
    public virtual void MirrorServoPos() { }
}
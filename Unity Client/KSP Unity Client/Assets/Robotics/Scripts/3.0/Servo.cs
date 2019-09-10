using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Servo : MonoBehaviour
{
    public string servoName;

    public ServoLimb.LimbAxis limbAxis;

    public Servo servoParentDirect, servoChildDirect;

    ServoLimb limb;

    Transform servoBase;

    public float limitMin, limitMax;

    public Part hostPart;

    MemoryBridge memoryBridge;

    LineRenderer lineRenderer;

    GameObject jointObject;

    public bool disabled {get; private set;}= false;

    public void Initialize(string servoName, ServoLimb limb, int parentID, MemoryBridge memoryBridge)
    {
        this.servoName = servoName;
        gameObject.name = servoName;
        this.limb = limb;
        this.memoryBridge = memoryBridge;

        limitMin = memoryBridge.GetFloat(servoName + "minPos");
        limitMax = memoryBridge.GetFloat(servoName + "maxPos");

        hostPart = GetComponent<Part>();

        hostPart.ToggleRenderers(false);

   
        BuildServoMesh();

        foreach (var part in hostPart.vessel.parts)
        {
            if (part.ID == parentID)
            {
                servoParentDirect = part.GetComponent<Servo>();
                servoParentDirect.SetServoChildDirect(this);
            }
        }

        if (hostPart.name.ToLower().Contains("skip"))
        {
            DisableServo(true);
            // Debug.Log(name + " disabled");
        }
    }

    public void DisableServo(bool disable)
    {
        disabled = disable;
        // Debug.Log("d");

        if (disable)
        {
            // Debug.Log("disable " + name);
            //   hostPart.lineRenderer.material.color = Color.red;
        }
    }
    public void SetServoChildDirect(Servo servo)
    {
        servoChildDirect = servo;
    }

    public void CreateBaseAnchor()
    {
        //create anchor base
        servoBase = new GameObject().transform;
        servoBase.name = "Servo Base";
        servoBase.SetParent(transform);
        servoBase.localPosition = Vector3.zero;
        servoBase.localRotation = Quaternion.Inverse(memoryBridge.GetQuaternion(servoName + "servoLocalRot"));
        servoBase.SetParent(transform.parent);
        transform.SetParent(servoBase);
        transform.localEulerAngles = Vector3.zero;
    }
    public void CalculateGroupAngle()
    {
        var angleList = new List<float>();
        // if (servoName.Contains("Ro"))
        var rotDifX = Vector3.Angle(limb.transform.right, transform.right);
        var negRotDifX = Vector3.Angle(limb.transform.right, -transform.right);
        var rotDifY = Vector3.Angle(limb.transform.up, transform.right);
        var negRotDifY = Vector3.Angle(limb.transform.up, -transform.right);
        var rotDifZ = Vector3.Angle(limb.transform.forward, transform.right);
        var negRotDifZ = Vector3.Angle(limb.transform.forward, -transform.right);
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
            limbAxis = ServoLimb.LimbAxis.X;
        else if (smallestAngle == rotDifY || smallestAngle == negRotDifY)
            limbAxis = ServoLimb.LimbAxis.Y;
        else if (smallestAngle == rotDifZ || smallestAngle == negRotDifZ)
            limbAxis = ServoLimb.LimbAxis.Z;

        var color = Color.blue;
        switch (limbAxis)
        {
            case ServoLimb.LimbAxis.X:
                color = Color.red;
                break;
            case ServoLimb.LimbAxis.Y:
                color = Color.green;
                break;
            case ServoLimb.LimbAxis.Z:
                color = Color.blue;
                break;
        }

        if (disabled)
        {
            // Debug.Log("servo disabled at sort");
            color = Color.white;
        }
        SetServoColor(color);
        //hostPart.SetJointColor(color);
        //Draw line renderer     
    }

    public void SetServoColor(Color newColor)
    {
        Debug.Log("set limb color");
        jointObject.GetComponent<MeshRenderer>().material.color = newColor;
        if (servoChildDirect)
        {
            Material mat = new Material(Shader.Find("Diffuse"));
            if (lineRenderer)
            {
                var offsetFromParent = lineRenderer.transform.InverseTransformPoint(servoChildDirect.transform.position);
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, offsetFromParent);
                mat.color = newColor;
                lineRenderer.material = mat;
            }
        }
    }
   
    void BuildServoMesh()
    {
        var kspPartName = hostPart.kspPartName;
        GameObject fakeJoint = null;
        if (kspPartName.Contains("Rotatron"))
        {
            fakeJoint = Instantiate(Resources.Load("Rotatron", typeof(GameObject))) as GameObject;
            fakeJoint.transform.SetParent(transform);
            fakeJoint.transform.localPosition = Vector3.zero;
            fakeJoint.transform.localEulerAngles = new Vector3(0, 0, 90);
            if (kspPartName == "IR.Rotatron.Right.v3")
            {
                Debug.Log("found right joint");
                fakeJoint.transform.localEulerAngles = new Vector3(0, 0, 90);
            }
            lineRenderer = fakeJoint.GetComponent<LineRenderer>();
        }
        if (kspPartName == "IR.Pivotron.RangeNinety" || kspPartName == "IR.Pivotron.Sixty.v3" || kspPartName == "IR.Pivotron.Hinge.Basic.v3" || kspPartName == "IR.Pivotron.Basic" || kspPartName == "IR.Pivotron.OneTwenty.v3" || kspPartName == "IR.Pivotron.Basic.v3"
            || kspPartName == "IR.Pivotron.RangeNinety.v3")
        {
            fakeJoint = Instantiate(Resources.Load("Pivotron", typeof(GameObject))) as GameObject;
            fakeJoint.transform.SetParent(transform);
            fakeJoint.transform.localPosition = Vector3.zero;
            fakeJoint.transform.localEulerAngles = new Vector3(0, 0, 90);
            lineRenderer = fakeJoint.GetComponent<LineRenderer>();
        }
        if (kspPartName == "IR.Extendatron.BasicHalf" || kspPartName == "IR.Extendatron.RightHalf.v3")
        {
            fakeJoint = Instantiate(Resources.Load("Extend", typeof(GameObject))) as GameObject;
            fakeJoint.transform.SetParent(transform);
            fakeJoint.transform.localPosition = Vector3.zero;
            fakeJoint.transform.localEulerAngles = new Vector3(90, 0, 0);
            lineRenderer = fakeJoint.GetComponent<LineRenderer>();
        }
        jointObject = fakeJoint;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

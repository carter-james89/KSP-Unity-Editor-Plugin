using System.Collections.Generic;
using Winterdom.IO.FileMap;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using System.Linq;

public class ServoLimb : MonoBehaviour
{
    public List<Servo> servos;
    private List<Servo> mirrorServos;
    public List<Servo> ikServos;

    public List<Servo> xAxisServos, yAxisServos, zAxisServos;

    public enum LimbAxis { X, Y, Z }

    MemoryBridge memoryBridge;

    public Gait gait;

    Transform limbEndPointMirror;
    public Transform limbEndPointIK;

    Transform groundPoint;

    public void Initialize(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;
        servos = new List<Servo>();
        //add mirror servo to leg that has already been created
        mirrorServos = BuildLimb(memoryBridge, LimbType.Mirror);
        //set the leg as a child of this
        transform.position = mirrorServos[0].servoBase.position;
        transform.rotation = mirrorServos[0].servoBase.rotation;
        transform.SetParent(mirrorServos[0].servoBase.parent);
        mirrorServos[0].servoBase.transform.SetParent(transform);
        foreach (var servo in mirrorServos)
        {
            servo.CalculateGroupAngle();
        }

        ikServos = BuildLimb(memoryBridge, LimbType.IK);
        ikServos[0].servoBase.transform.SetParent(transform);

        

        xAxisServos = new List<Servo>();
        yAxisServos = new List<Servo>();
        zAxisServos = new List<Servo>();
        foreach (var servo in ikServos)
        {
            servo.CalculateGroupAngle();
            servo.servoType = Servo.ServoType.IK;

            if (servo.limbAxis == LimbAxis.X)
            {
                xAxisServos.Add(servo);
            }
            else if (servo.limbAxis == LimbAxis.Y)
            {
                yAxisServos.Add(servo);
            }
            else if (servo.limbAxis == LimbAxis.Z)
            {
                zAxisServos.Add(servo);
            }
        }

       
    }

    void SetIKContactPoint(Vector3 localPos)
    {

    }

    #region Construction
    public void FindContactGroundPoint()
    {
        var endServo = mirrorServos[mirrorServos.Count - 1].transform;

        var footOffset = endServo.position;
        footOffset.y = memoryBridge.vesselControl.ground.position.y;

        // var foot = Instantiate(Resources.Load("Foot", typeof(GameObject))) as GameObject;
        //foot.name = "True End Point " + name;
        // foot.transform.SetParent(endServo);
        //  foot.GetComponent<MeshRenderer>().material.color = color;
        // footOffset.y = -footOffset.y;
        //  foot.transform.position = footOffset;

        footOffset = endServo.InverseTransformPoint(footOffset);

        limbEndPointMirror = CreateLimbEndtObject(mirrorServos[mirrorServos.Count - 1], footOffset);
        limbEndPointIK = CreateLimbEndtObject(ikServos[mirrorServos.Count - 1], footOffset);

        memoryBridge.SetVector3(mirrorServos[mirrorServos.Count - 1].gameObject.name + "contactPoint", footOffset);
    }

    public void FindContactMeshPoint(LimbAxis limAxis)
    {
        var endServo = mirrorServos[mirrorServos.Count - 1].transform;
        var color = mirrorServos[mirrorServos.Count - 1].color;

        Vector3 footOffset = Vector3.zero;

        var worldVertPositions = new List<Vector3>();
        var offsets = new List<Vector3>();
        var childFilters = endServo.GetComponentsInChildren<MeshFilter>();
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
            var offset = endServo.InverseTransformPoint(worldPos);
            offsets.Add(offset);
        }
        float furthestY = 0;
        for (int i = 0; i < offsets.Count; i++)
        {
            if (offsets[i].y >= furthestY)
            {
                furthestY = offsets[i].y;
                footOffset = offsets[i];
            }
        }



        limbEndPointMirror = CreateLimbEndtObject(mirrorServos[mirrorServos.Count - 1], footOffset);
        limbEndPointIK = CreateLimbEndtObject(ikServos[mirrorServos.Count - 1], footOffset);


        // if (andWrite)
        memoryBridge.SetVector3(mirrorServos[mirrorServos.Count - 1].gameObject.name + "contactPoint", footOffset);

        //  targetOffset = (float)(Math.Atan2(footOffset.z, footOffset.y));

        //  targetOffset *= (float)(180 / Math.PI);



    }
    Transform CreateLimbEndtObject(Servo endServo, Vector3 offset)
    {
        var foot = Instantiate(Resources.Load("Foot", typeof(GameObject))) as GameObject;
        foot.name = "Limb End Point " + name;
        foot.GetComponent<MeshRenderer>().material.color = endServo.color;
        foot.transform.SetParent(endServo.transform);
        foot.transform.localPosition = offset;

        var footRenderer = foot.GetComponent<LineRenderer>();
        footRenderer.SetPosition(0, Vector3.zero);
        var wristOffset = foot.transform.InverseTransformPoint(endServo.transform.position);
        footRenderer.SetPosition(1, wristOffset);
        footRenderer.material.color = endServo.color;

        return foot.transform;
    }

    enum LimbType { IK, Mirror }
    List<Servo> BuildLimb(MemoryBridge memoryBridge, LimbType limbType)
    {
        var limbFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, name);
        float byteCount;
        float servoCount;
        var returnServos = new List<Servo>();
        using (Stream mapStream = limbFile.MapView(MapAccess.FileMapAllAccess, 0, 16))
        {
            var floatBuffer = new byte[4];
            mapStream.Read(floatBuffer, 0, 4);
            byteCount = BitConverter.ToSingle(floatBuffer, 0);
            mapStream.Read(floatBuffer, 0, 4);
            servoCount = BitConverter.ToSingle(floatBuffer, 0);
        }
        var servosMirror = new List<RoboticServoMirror>();
        using (Stream mapStream = limbFile.MapView(MapAccess.FileMapAllAccess, 0, (int)byteCount))
        {
            mapStream.Position = 16;

            for (int i = 0; i < servoCount; i++)
            {
                //servo name
                var floatBuffer = new byte[4];
                mapStream.Read(floatBuffer, 0, 4);
                var stringByteLength = BitConverter.ToSingle(floatBuffer, 0);

                var stringBuffer = new byte[(int)stringByteLength];
                mapStream.Read(stringBuffer, 0, stringBuffer.Length);
                string servoName = ASCIIEncoding.ASCII.GetString(stringBuffer);

                //servo parent id
                mapStream.Read(floatBuffer, 0, 4);
                var partID = BitConverter.ToSingle(floatBuffer, 0);
                mapStream.Read(floatBuffer, 0, 4);
                var parentID = BitConverter.ToSingle(floatBuffer, 0);

                foreach (var part in memoryBridge.vesselControl.vessel.parts)
                {
                    if (part.ID == partID)
                    {
                        if (limbType == LimbType.Mirror)
                        {
                            var newServo = part.gameObject.AddComponent<Servo>();
                            newServo.Initialize(servoName, this, (int)parentID, part, memoryBridge);

                            newServo.servoBase = new GameObject().transform;
                            newServo.servoBase.name = "Servo Base";
                            // newServo.servoBase.position = newServo.transform.position

                            newServo.servoBase.SetParent(newServo.transform);
                            newServo.servoBase.localPosition = Vector3.zero;
                            newServo.servoBase.localRotation = Quaternion.Inverse(memoryBridge.GetQuaternion(servoName + "servoLocalRot"));
                            newServo.servoBase.SetParent(newServo.transform.parent);
                            newServo.transform.SetParent(newServo.servoBase);
                            newServo.transform.localEulerAngles = Vector3.zero;



                            servos.Add(newServo);
                            returnServos.Add(newServo);
                        }
                        else
                        {
                            var newServoObject = new GameObject();
                            if (returnServos.Count == 0)
                            {
                                newServoObject.transform.SetParent(transform);
                            }
                            else
                            {
                                newServoObject.transform.SetParent(returnServos[returnServos.Count - 1].transform);
                            }
                            var newServo = newServoObject.AddComponent<Servo>();
                            foreach (var servo in mirrorServos)
                            {
                                if (servo.gameObject.name == servoName)
                                {
                                    Debug.Log("found servo");
                                    servo.partnerServo = newServo;
                                    newServo.partnerServo = servo;
                                    newServoObject.transform.position = servo.transform.position;
                                    newServoObject.transform.rotation = servo.transform.rotation;
                                }
                            }

                            //  newServoObject.transform.position = part.transform.position;
                            //  newServoObject.transform.rotation = part.transform.rotation;

                            newServo.Initialize(servoName, this, (int)parentID, part, memoryBridge);


                            newServo.servoBase = new GameObject().transform;
                            newServo.servoBase.name = "Servo Base";
                            // newServo.servoBase.position = newServo.transform.position

                            newServo.servoBase.SetParent(newServo.transform);
                            newServo.servoBase.localPosition = Vector3.zero;
                            newServo.servoBase.localRotation = Quaternion.identity;// Quaternion.Inverse(memoryBridge.GetQuaternion(servoName + "servoLocalRot"));
                            newServo.servoBase.SetParent(newServo.transform.parent);
                            newServo.transform.SetParent(newServo.servoBase);
                            newServo.transform.localEulerAngles = Vector3.zero;


                            servos.Add(newServo);
                            returnServos.Add(newServo);
                        }

                    }
                }
            }
        }
        //if (limbType == LimbType.Mirror)
        foreach (var servo in returnServos)
        {
            // servo.CreateBaseAnchor();
        }

        limbFile.Dispose();
        limbFile.Close();

        return returnServos;
    }
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < ikServos.Count; i++)
        {

            ikServos[i].groupOffsets = new Dictionary<GameObject, float>();

            var limbOffset = ikServos[i].servoBase.InverseTransformPoint(limbEndPointIK.position);
            var tempAngle = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
            tempAngle = (float)(tempAngle * 180 / Math.PI);
            // ikServos[v].targetOffset = tempAngle;
            ikServos[i].groupOffsets.Add(limbEndPointIK.gameObject, tempAngle);

            // servos.Add(servo);
            for (int v = 0; v < ikServos.Count; v++)
            {
                if (ikServos[i] != ikServos[v])
                {
                    limbOffset = ikServos[i].servoBase.InverseTransformPoint(ikServos[v].transform.position);
                    tempAngle = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
                    tempAngle = (float)(tempAngle * 180 / Math.PI);
                    // ikServos[v].targetOffset = tempAngle;
                    ikServos[i].groupOffsets.Add(ikServos[v].gameObject, tempAngle);
                }
                ////set the wrist dist
                //if (i == 0)
                //{
                //    if (servoGroup[i].limbAxis == LimbController.LimbAxis.Z)
                //    {
                //        servoGroup[i].targetOffset = 0;
                //    }
                //    else
                //    {
                //        servoGroup[i].limbLength = Vector3.Distance(servoGroup[i].transform.position, limbController.contactPointOffset);

                //        var limbOffset = servoGroup[i].servoBase.InverseTransformPoint(limbEnd.position);
                //        var tempAngle = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
                //        tempAngle = (float)(tempAngle * 180 / Math.PI);
                //        servoGroup[i].targetOffset = tempAngle;
                //    }
                //}
                //else
                //{
                //servoGroup[i].limbLength = Vector3.Distance(servoGroup[i].transform.position, servoGroup[i - 1].transform.position);
                //var limbOffset = servoGroup[i].servoBase.InverseTransformPoint(servoGroup[i - 1].transform.position);
                //var tempAngle = (float)(Math.Atan2(limbOffset.z, limbOffset.y));
                //tempAngle = (float)(tempAngle * 180 / Math.PI);
                //servoGroup[i].targetOffset = tempAngle;
                //  }
            }
        }

        foreach (var servo in servos)
        {
            servo.MirrorServoPos();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void MirrorServos()
    {
        foreach (var servo in mirrorServos)
        {
            servo.MirrorServoPos();
        }
    }
    public float rawClearance;
    public void SetGroundPos()
    {
        // torque = memoryBridge.GetVector3(limbMirror.servoWrist.servoName + "torque");
        // velocity = torque.magnitude;
        // explosionPotential = memoryBridge.GetFloat(limbMirror.servoWrist.servoName + "explosionPotential");
        // gExplodeChance = memoryBridge.GetFloat(limbMirror.servoWrist.servoName + "gExplodeChance");
        //  footActive = memoryBridge.GetBool(limbMirror.servoWrist.servoName + "active");
        //  hasExploded = memoryBridge.GetBool(limbMirror.servoWrist.servoName + "exploded");

        //if (hasExploded)
        //{
        //    CamUI.SetCamText(name + " has exploded");
        //    Debug.LogError("Exploded Leg : " + name + " Velocity : " + velocity + " Mode : " + limbIK.gaitSequenceMode.ToString() + " Percent : " + CalculateStridePercent());
        //}
        rawClearance = memoryBridge.GetFloat(mirrorServos[mirrorServos.Count - 1].gameObject.name + "KSPFootClearance");
        var groundContact = memoryBridge.GetBool(mirrorServos[mirrorServos.Count - 1].servoName + "GroundContact");
        var localPoint = limbEndPointMirror.localPosition - new Vector3(0, .4f, 0);
        groundPoint.position = mirrorServos[mirrorServos.Count - 1].transform.TransformPoint(localPoint) - new Vector3(0, rawClearance, 0);
    }

    public void CreateGait(bool createGround, AnimationCurve gaitCurve)
    {
        if (createGround)
        {
            groundPoint = Instantiate(GameObject.Find("Ground")).transform;
            groundPoint.position = limbEndPointMirror.position;
        }



        var newObject = Instantiate(Resources.Load("Gait", typeof(GameObject))) as GameObject;
        gait = newObject.AddComponent<Gait>();
        gait.Initialize(gaitCurve);

        gait.transform.SetParent(transform);//limbController.roboticController.directionTarget);

        gait.transform.position = limbEndPointIK.position; //transform.position + transform.up * limbController.roboticController.gaitDistance;
    }
}

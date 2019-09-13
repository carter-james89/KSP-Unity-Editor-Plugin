using System.Collections.Generic;
using Winterdom.IO.FileMap;
using UnityEngine;
using System.IO;
using System;
using System.Text;

public class ServoLimb : MonoBehaviour
{
    public List<Servo> servos;
    private List<Servo> mirrorServos;
    private List<Servo> ikServos;

    public enum LimbAxis { X, Y, Z }

    public void Initialize(RoboticAssembler roboAssembler, MemoryBridge memoryBridge)
    {       
        servos = new List<Servo>();
        mirrorServos = BuildLimb(memoryBridge);
        ikServos = BuildLimb(memoryBridge);

        transform.position = ikServos[0].transform.position;
        transform.SetParent(ikServos[0].transform.parent);
        ikServos[0].transform.SetParent(transform);

        foreach (var servo in ikServos)
        {
            servo.CalculateGroupAngle();
        }
    }

    List<Servo> BuildLimb(MemoryBridge memoryBridge)
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
                        var newServo = part.gameObject.AddComponent<Servo>();
                        newServo.Initialize(servoName, this, (int)parentID, memoryBridge);
                        servos.Add(newServo);
                        returnServos.Add(newServo);
                    }
                }
            }
        }
        foreach (var servo in returnServos)
        {
            servo.CreateBaseAnchor();
        }

        limbFile.Dispose();
        limbFile.Close();
        
        return returnServos;
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;

namespace MemoryBridgeServer
{
    class RoboticArm : MonoBehaviour
    {
        MemoryMappedFile LimbFile;
        List<RoboticServo> limbServos;
        public string limbName;
        public void CustomAwake(IRWrapper.IControlGroup limbGroup, IRWrapper.IServo servoBase, IRWrapper.IServo servoWrist, MemoryBridge memoryBridge)
        {
            limbName = limbGroup.Name + memoryBridge.fileName;
            limbServos = new List<RoboticServo>();
            //Add wrist servo
            var newServo = servoBase.HostPart.gameObject.AddComponent(typeof(RoboticServo)) as RoboticServo;
            newServo.CustomStart(servoBase, memoryBridge);
            limbServos.Add(newServo);

            Part tempPart = servoBase.HostPart;

            bool done = false;
            int count = 0;
            do
            {
                count++;
                var armParts = tempPart.children;

                armParts.Remove(tempPart);
                foreach (var part in armParts)
                {
                    Debug.Log(part.name + " is a child of " + tempPart.name);
                    IRWrapper.IServo testServo = null;
                    foreach (var servo in limbGroup.Servos)
                    {
                        if (part == servo.HostPart)
                        {
                            testServo = servo;
                            //break;
                        }
                    }
                    if (testServo != null)
                    {
                        tempPart = part;
                       // if (!testServo.Name.ToLower().Contains("skip"))
                       // {                    
                            Debug.Log("found servo on part");
                            
                            newServo = part.gameObject.AddComponent(typeof(RoboticServo)) as RoboticServo;
                            newServo.CustomStart(testServo, memoryBridge);
                            limbServos.Add(newServo);                          
                      //  }
                        if (testServo == servoWrist)
                            done = true;
                    }
                }
            } while (count < 30);

            //newServo = gameObject.AddComponent(typeof(RoboticServo)) as RoboticServo;
            //newServo.CustomStart(servoWrist);
            //limbServos.Add(newServo);

            int byteCount = 0;

            Debug.Log(limbGroup.Name + " servo hierarchy");
            for (int i = 0; i < limbServos.Count; i++)
            {
              //  Debug.Log(servo.servo.Name);
                string parentName = "null";
                if (i != 0)
                {
                    parentName = limbServos[i - 1].servoName;
                    limbServos[i].parentID = limbServos[i - 1].servo.HostPart.gameObject.GetInstanceID();
                    limbServos[i].ParentSet(limbServos[i - 1].servo.HostPart.transform);
                    
                }
                
                limbServos[i].parentServoName = parentName;

                byteCount += 8;
                //parent id
                byteCount += 4;
                byteCount += limbServos[i].servoName.Length;
               // byteCount += limbServos[i].parentServoName.Length;
            }
            byteCount += 16;
            LimbFile = MemoryMappedFile.Create(MapProtection.PageReadWrite, byteCount, limbName);

            using (Stream mapStream = LimbFile.MapView(MapAccess.FileMapAllAccess, 0, byteCount))
            {
                var bytePackage = BitConverter.GetBytes((float)byteCount);
                mapStream.Write(bytePackage, 0, 4);
                bytePackage = BitConverter.GetBytes((float)limbServos.Count);
                mapStream.Write(bytePackage, 0, 4);

                mapStream.Position = 16;

                for (int i = 0; i < limbServos.Count; i++)
                {
                    var nameBytePackage = Encoding.ASCII.GetBytes(limbServos[i].servoName);
                    bytePackage = BitConverter.GetBytes((float)nameBytePackage.Length);
                    mapStream.Write(bytePackage, 0, 4);
                    mapStream.Write(nameBytePackage, 0, nameBytePackage.Length);

                   // nameBytePackage = Encoding.ASCII.GetBytes(limbServos[i].parentServoName);
                    bytePackage = BitConverter.GetBytes((float)limbServos[i].servo.HostPart.gameObject.GetInstanceID());
                    mapStream.Write(bytePackage, 0, 4);
                    //  mapStream.Write(nameBytePackage, 0, nameBytePackage.Length);     
                    bytePackage = BitConverter.GetBytes((float)limbServos[i].parentID);
                    mapStream.Write(bytePackage, 0, 4);
                }
            }
        }

        public void CustomUpdate()
        {
            foreach (var servo in limbServos )
            {
                servo.CustomUpdate();
            }
        }
        void OnDestroy()
        {
            if (LimbFile != null)
                LimbFile.Close();
        }
    }

   
}

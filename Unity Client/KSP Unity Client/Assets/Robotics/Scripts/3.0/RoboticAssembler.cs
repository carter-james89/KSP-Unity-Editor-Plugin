using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using Winterdom.IO.FileMap;
using System.Linq;
using System;

public class RoboticAssembler : MonoBehaviour
{
    public MemoryBridge memoryBridge { get; private set; }
    List<ServoLimb> limbs;
   public void Assemble(MemoryBridge bridge)
    {
        memoryBridge = bridge;
        ReadLimbsFromBridge();
    }
    void ReadLimbsFromBridge()
    {
        var IRFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "IRFile" + memoryBridge.fileName);
        limbs = new List<ServoLimb>();
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

       // limbObjects = new List<GameObject>();
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
               // newLimbObject.transform.SetParent(memoryBridge.vesselControl.m);

               var newLimb =  newLimbObject.AddComponent<ServoLimb>();
                newLimb.Initialize(this, memoryBridge);

                limbs.Add(newLimb);
            }
        }


        IRFile.Dispose();
        IRFile.Close();
    }
}

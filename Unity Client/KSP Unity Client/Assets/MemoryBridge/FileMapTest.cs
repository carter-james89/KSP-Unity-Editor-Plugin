using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Winterdom.IO.FileMap;
using System.IO;
using System.Text;

public class FileMapTest : MonoBehaviour
{
    // Use this for initialization
    void Start1()
    {
        //MemoryMappedFile memMap = MemoryMappedFile.Create(MapProtection.PageReadWrite, 512, "TestMemMap3");
        //if (memMap == null)
        //    Debug.Log("streamwriter null");

        //Debug.Log(memMap);
        //using (Stream mapStream = memMap.MapView(MapAccess.FileMapAllAccess, 0, 512))
        //{
        //    Debug.Log(mapStream);
        //  //  StreamReader streamReader = new StreamReader(mapStream);
        //    StreamWriter streamWriter = new StreamWriter(mapStream);
        //    if (streamWriter == null)
        //        Debug.Log("streamwriter null");
        //    streamWriter.WriteLine("yoo");
        //    streamWriter.Flush();
        //    mapStream.Flush();
        //    Debug.Log("can write " + mapStream.CanWrite);
        //}
        MemoryMappedFile memFile = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "TestMemMap3");
        
            if (memFile == null)
                Debug.Log("memfile null");

            using (Stream mapStream1 = memFile.MapView(MapAccess.FileMapAllAccess, 0, 512))
            {
                Debug.Log("can read " + mapStream1.CanRead);
                StreamReader streamReader = new StreamReader(mapStream1);
                
                if (streamReader == null)
                    Debug.Log("streamReader null");
                Debug.Log("line " + streamReader.ReadToEnd());
                //var bytes = mapStream1.BeginRead()
            }

        memFile.Dispose();
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}

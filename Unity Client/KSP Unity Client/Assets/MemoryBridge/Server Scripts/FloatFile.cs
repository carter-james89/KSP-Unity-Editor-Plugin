using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Winterdom.IO.FileMap;
using System.Threading;
using System;
using System.Text;
using UnityEditor;

public class FloatFile : MonoBehaviour
{
    int byteCountKeyFloat = 0, byteCountKeyFloatNew = 0;
    MemoryMappedFile fileHeader, fileKeyFloat, fileValueFloat, fileKeyFloat1;
    Stream streamFloatValue;

    public float valuesListWrite, valuesListRead;
    public bool connected = false;

    public List<string> keysFloat;
    public Dictionary<string, int> valueAddressFloat, valueAddressClientFloat;
    Dictionary<string, float> keysToAdd;
    public List<float> serverValuesFloat, clientValuesFloat;

    MemoryBridge memoryBridge;

    public byte[] floatBuffer = new byte[4];
    // Use this for initialization
    public void ConnectToServer(MemoryBridge memoryBridge)
    {
        this.memoryBridge = memoryBridge;
        StartCoroutine("TryConnectToServer");
        //TryConnectToServer();
    }
    IEnumerator TryConnectToServer()
    {
        Application.runInBackground = true;
        keysFloat = new List<string>();
        valueAddressFloat = new Dictionary<string, int>();
        keysToAdd = new Dictionary<string, float>();

        do
        {
            //  fileHeader = null;
            //Debug.Log("checking for header file");
            fileHeader = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileHeader");

            if (fileHeader != null)
            {
                //Debug.Log("header file not null");


                //   fileKeyFloat = MemoryMappedFile.Create(MapProtection.PageReadWrite, byteCountKeyFloat + byteCountKeyFloatNew, "FileKeysFloat");
                //  fileKeyFloat = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileKeysFloat");
                fileValueFloat = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileValuesFloat3");
                connected = true;

                ////  if (fileKeyFloat == null)
                //      fileKeyFloat = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileKeysFloat");
                ReadHeaderFile();
                //var fileKeyFloat1 = MemoryMappedFile.Create(MapProtection.PageReadWrite, 50000, "FileKeysFloat1");
                //using (var keyWriter = fileKeyFloat1.MapView(MapAccess.FileMapAllAccess, 0, 50000))
                //{
                //    keyWriter.Position = 4000;
                //}
                //fileKeyFloat1.Close();
                yield break;
            }
            yield return null;

        } while (!connected);
    }
    void UpdateHeaderFile()
    {
        using (var headerStream = fileHeader.MapView(MapAccess.FileMapAllAccess, 0, 16))
        {
            // //Debug.Log("update header file");       
            headerStream.Write(BitConverter.GetBytes(memoryBridge.transmittingFloat), 0, 4);
            headerStream.Write(BitConverter.GetBytes((float)keysFloat.Count), 0, 4);
            headerStream.Write(BitConverter.GetBytes((float)byteCountKeyFloat), 0, 4);
        }
    }
    void ReadHeaderFile()
    {
        float keyCountFloat = 0;
        float byteCountFloat = 0;
        using (var headerStream = fileHeader.MapView(MapAccess.FileMapAllAccess, 0, 16))
        {
            headerStream.Position = 4;
            //read key count
            headerStream.Read(floatBuffer, 0, 4);
            keyCountFloat = BitConverter.ToSingle(floatBuffer, 0);
            //total readFile byte count
            headerStream.Read(floatBuffer, 0, 4);
            byteCountKeyFloat = (int)BitConverter.ToSingle(floatBuffer, 0);
        }
        if (keyCountFloat != keysFloat.Count)
            GetFloatKeys(keyCountFloat, byteCountKeyFloat);
    }

    int prevStreamPos = 0;
    void GetFloatKeys(float keysToRead, float byteCount)
    {
        Debug.Log("Get Float Keys");
        if (fileKeyFloat == null)
        {
            //Debug.Log("Open key file");
            fileKeyFloat = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileKeysFloat3");
        }

        keysFloat = new List<string>();
        valueAddressFloat = new Dictionary<string, int>();

        using (var keyReader = fileKeyFloat.MapView(MapAccess.FileMapAllAccess, 0, 100000))
        {
            for (int i = 0; i < keysToRead; i++)
            {
                keyReader.Read(floatBuffer, 0, 4);
                var stringLength = BitConverter.ToSingle(floatBuffer, 0);
                var stringBuffer = new Byte[(int)stringLength];
                keyReader.Read(stringBuffer, 0, (int)stringLength);
                var key = Encoding.ASCII.GetString(stringBuffer);

                //Debug.Log(key + " added from server");
                keysFloat.Add(key);
                valueAddressFloat.Add(key, keysFloat.Count - 1);
            }
        }
        streamFloatValue = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, (int)keysFloat.Count * 4);
        //  EditorApplication.isPaused = true;
    }

    //private void Update()
    //{

    //}

    public void StartUpdate()
    {
        ReadHeaderFile();
        streamFloatValue = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, (int)keysFloat.Count * 4);

        if (Input.GetKeyDown(KeyCode.Keypad7))
            using (var valueStream = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, keysFloat.Count * 4))
            {
                foreach (var key in keysFloat)
                {
                    valueStream.Read(floatBuffer, 0, 4);
                    //Debug.Log(BitConverter.ToSingle(floatBuffer, 0));

                }
            }
    }
    public void EndUpdate()
    {
        if (streamFloatValue != null)
        {
            streamFloatValue.Dispose();
            streamFloatValue.Close();
        }

        if (keysToAdd.Count != 0)
        {
            ReadHeaderFile();
            int newKeyCount = (keysFloat.Count + keysToAdd.Count);
            // fileKeyFloat.Dispose();
            //  if (fileKeyFloat != null)
            //     fileKeyFloat.Close();
            var byteCountNew = byteCountKeyFloat + byteCountKeyFloatNew;

            //if(fileKeyFloat1 == null)
            //     fileKeyFloat1 = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileKeysFloat3");
            //   using (var keyWriter = fileKeyFloat1.MapView(MapAccess.FileMapAllAccess, 0, 100000)) {
            //     keyWriter.Position = 3000; }
            //    fileKeyFloat = MemoryMappedFile.Create(MapProtection.PageReadWrite, byteCountNew, "FileKeysFloat");
            using (var keyWriter = fileKeyFloat.MapView(MapAccess.FileMapAllAccess, 0, 100000))
            {
                //Debug.Log("byteCountKeyFloat " + byteCountKeyFloat);

                //Debug.Log("byteCountKey + newKeybytecount " + byteCountNew);
                keyWriter.Position = byteCountKeyFloat;

                foreach (KeyValuePair<string, float> entry in keysToAdd)
                {
                    var stringBuffer = Encoding.ASCII.GetBytes(entry.Key);
                    // //Debug.Log("string byte length " + stringBuffer.Length);
                    keyWriter.Write(BitConverter.GetBytes((float)stringBuffer.Length), 0, 4);
                    // //Debug.Log("write '" + entry.Key + "' at new key at position " + keyWriter.Position);
                    keyWriter.Write(stringBuffer, 0, stringBuffer.Length);
                    // //Debug.Log("add to file " + entry.Key);

                    keysFloat.Add(entry.Key);
                    valueAddressFloat.Add(entry.Key, keysFloat.Count - 1);
                }
            }
            byteCountKeyFloat += byteCountKeyFloatNew;
            byteCountKeyFloatNew = 0;
            fileValueFloat = MemoryMappedFile.Create(MapProtection.PageReadWrite, newKeyCount * 4, "FileValuesFloat3");
            using (var valueStream = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, newKeyCount * 4))
            {
                valueStream.Position = keysFloat.Count * 4 - keysToAdd.Count * 4;
                foreach (KeyValuePair<string, float> entry in keysToAdd)
                {
                    ////Debug.Log("add value " + entry.Value);
                    valueStream.Write(BitConverter.GetBytes(entry.Value), 0, 4);
                }
            }

            UpdateHeaderFile();
            keysToAdd = new Dictionary<string, float>();

//            if (Input.GetKeyDown(KeyCode.Alpha5))
//            {
//                foreach(KeyValuePair<string,int> pair in valueAddressFloat)
//{
//                    Debug.Log(pair.Key);
//                }
//            }
        }
    }
    public float GetFloat(string key)
    {
        bool valueFound = true;
        float returnValue = 0;
        int address = 0;
        //check the server keys for the value
        try
        {
            address = valueAddressFloat[key];
        }
        catch { //Debug.Log("value " + key + " not found"); valueFound = false; 
        }

        if (valueFound)
        {
            streamFloatValue.Position = address * 4;
            streamFloatValue.Read(floatBuffer, 0, 4);
            returnValue = BitConverter.ToSingle(floatBuffer, 0);
        }
        return returnValue;
    }

    public void SetFloat(string key, float value)
    {
        int address = 0;
        bool valueFound = true;
        try
        {
            address = valueAddressFloat[key];

        }
        catch { valueFound = false; }

        if (valueFound)
        {
            streamFloatValue.Position = address * 4;
            streamFloatValue.Write(BitConverter.GetBytes(value), 0, 4);
        }

        if (!valueFound)
        {
            if (!keysToAdd.ContainsKey(key))
            {
                var stringBuffer = Encoding.ASCII.GetBytes(key);
                byteCountKeyFloatNew += stringBuffer.Length + 4;
                keysToAdd.Add(key, value);
            }
            else
            {
                keysToAdd[key] = value;
            }

        }

    }
    public void CustomOnDestroy()
    {
        UpdateHeaderFile();
        if (fileHeader != null)
            fileHeader.Close();
        if (streamFloatValue != null)
            streamFloatValue.Close();
        if (fileKeyFloat != null)
            fileKeyFloat.Close();
        if (fileValueFloat != null)
            fileValueFloat.Close();
    }
}

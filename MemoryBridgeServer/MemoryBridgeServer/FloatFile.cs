using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Winterdom.IO.FileMap;
using System.Threading;

namespace MemoryBridgeServer
{
    class FloatFile : MonoBehaviour
    {
        int byteCountKeyFloat = 0, byteCountKeyFloatNew = 0;
        MemoryMappedFile fileHeader, fileKeyFloat, fileValueFloat, fileKeyFloat1;
        Stream streamFloatValue;

        public float valuesListWrite, valuesListRead;

        MemoryBridge memoryBridge;

        public List<string> keysFloat;
        public Dictionary<string, int> valueAddressFloat; //valueAddressClientFloat;
        Dictionary<string, float> keysToAdd;
        public List<float> serverValuesFloat, clientValuesFloat;

        public byte[] floatBuffer = new byte[4];
        // Use this for initialization
        public void StartServer(MemoryBridge memoryBridge)
        {
            fileHeader = MemoryMappedFile.Create(MapProtection.PageReadWrite, 16, "FileHeader");
            fileValueFloat = MemoryMappedFile.Create(MapProtection.PageReadWrite, 16, "FileValuesFloat3");

            this.memoryBridge = memoryBridge;

            keysFloat = new List<string>();
            valueAddressFloat = new Dictionary<string, int>();
            keysToAdd = new Dictionary<string, float>();
            UpdateHeaderFile();

        }
        void UpdateHeaderFile()
        {
            //   Debug.Log("update header file " + keysFloat.Count);
            using (var headerStream = fileHeader.MapView(MapAccess.FileMapAllAccess, 0, 16))
            {
                headerStream.Write(BitConverter.GetBytes(memoryBridge.clientTransmittingFloat), 0, 4);
                headerStream.Write(BitConverter.GetBytes((float)keysFloat.Count), 0, 4);
                headerStream.Write(BitConverter.GetBytes((float)byteCountKeyFloat), 0, 4);
            }
        }
        void ReadHeaderFile()
        {
            //Debug.Log("start read late update");
            float keyCountFloat = 0;
            float byteCountFloat = 0;
            using (var headerStream = fileHeader.MapView(MapAccess.FileMapAllAccess, 0, 16))
            {
                headerStream.Read(floatBuffer, 0, 4);
                memoryBridge.clientTransmittingFloat = BitConverter.ToSingle(floatBuffer, 0);
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



        void GetFloatKeys(float keysToRead, float byteCount)
        {
            //  Debug.Log("Get float keys count  " + keysToRead);
          //  fileKeyFloat = MemoryMappedFile.Open(MapAccess.FileMapAllAccess, "FileKeysFloat");
            keysFloat = new List<string>();
            valueAddressFloat = new Dictionary<string, int>();

            using (var keyReader = fileKeyFloat.MapView(MapAccess.FileMapAllAccess, 0, 100000))// (int)byteCount))
            {
                for (int i = 0; i < keysToRead; i++)
                {
                    keyReader.Read(floatBuffer, 0, 4);
                    var stringLength = BitConverter.ToSingle(floatBuffer, 0);
                    //  Debug.Log(i + " string length " + stringLength);
                    var stringBuffer = new Byte[(int)stringLength];
                    keyReader.Read(stringBuffer, 0, (int)stringLength);
                    var key = Encoding.ASCII.GetString(stringBuffer);

                    Debug.Log("Add client key: " + key);
                    keysFloat.Add(key);
                    valueAddressFloat.Add(key, keysFloat.Count - 1);
                }
            }
        }

        public void StartUpdate()
        {
            ReadHeaderFile();
            streamFloatValue = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, (int)keysFloat.Count * 4);

            if (Input.GetKeyDown(KeyCode.Keypad9))
                foreach (KeyValuePair<string, int> entry in valueAddressFloat)
                {
                    //valueStream.Read(floatBuffer, 0, 4);
                    Debug.Log(entry.Key);

                }


            //if (Input.GetKeyDown(KeyCode.Keypad8))
            //    using (var valueStream = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, keysFloat.Count * 4))
            //    {
            //        foreach (var key in keysFloat)
            //        {
            //            valueStream.Read(floatBuffer, 0, 4);
            //            Debug.Log(BitConverter.ToSingle(floatBuffer, 0));

            //        }
            //    }

            if (Input.GetKeyDown(KeyCode.Keypad7))
                using (var valueStream = fileValueFloat.MapView(MapAccess.FileMapAllAccess, 0, keysFloat.Count * 4))
                {
                    foreach (var key in keysFloat)
                    {
                        valueStream.Read(floatBuffer, 0, 4);
                        Debug.Log(BitConverter.ToSingle(floatBuffer, 0));

                    }
                }
        }
        public void EndUpdate()
        {
         //   Debug.Log("start float late update");
            if(streamFloatValue != null)
            {
               // streamFloatValue.Dispose();
                //streamFloatValue.Close();
            }
            

            if (keysToAdd.Count != 0)
            {
                ReadHeaderFile();
                int newKeyCount = (keysFloat.Count + keysToAdd.Count);
                if(fileKeyFloat == null)
                    fileKeyFloat = MemoryMappedFile.Create(MapProtection.PageReadWrite, 100000, "FileKeysFloat3");

               // if (fileKeyFloat1 == null)
               //     fileKeyFloat1 = MemoryMappedFile.Create(MapProtection.PageReadWrite, 100000, "FileKeysFloat3");

                //using (var keyWriter1 = fileKeyFloat1.MapView(MapAccess.FileMapAllAccess, 0, 100000)) { }

                using (var keyWriter = fileKeyFloat.MapView(MapAccess.FileMapAllAccess, 0, 100000))
                {
                    keyWriter.Position = byteCountKeyFloat;

                    foreach (KeyValuePair<string, float> entry in keysToAdd)
                    {
                        var stringBuffer = Encoding.ASCII.GetBytes(entry.Key);
                        // Debug.Log("string byte length " + stringBuffer.Length);
                        keyWriter.Write(BitConverter.GetBytes((float)stringBuffer.Length), 0, 4);
                        //  Debug.Log("write '" + entry.Key + "' at new key at position " + keyWriter.Position);
                        keyWriter.Write(stringBuffer, 0, stringBuffer.Length);
                        Debug.Log("Add server key: " + entry.Key);

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
                        // Debug.Log("add value " + entry.Value);
                        valueStream.Write(BitConverter.GetBytes(entry.Value), 0, 4);
                    }
                }

                UpdateHeaderFile();
                keysToAdd = new Dictionary<string, float>();
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
            catch { valueFound = false; };// Debug.Log("Value not found, returning 0. Key: " + key + " " + Time.frameCount); }

            if (valueFound)
            {
               // Debug.Log("look for key " + key + " at position " + )
                streamFloatValue.Position = address * 4;
                streamFloatValue.Read(floatBuffer, 0, 4);
               // Debug.Log("successfully read from memory " + key);
                returnValue = BitConverter.ToSingle(floatBuffer, 0);
            }
           

            return returnValue;
        }

        public void SetFloat(string key, float value)
        {
            bool valueFound = true;
            try
            {
                var address = valueAddressFloat[key];
                streamFloatValue.Position = address * 4;
                streamFloatValue.Write(BitConverter.GetBytes(value), 0, 4);
            }
            catch { valueFound = false; }

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
            if (fileKeyFloat1 != null)
                fileKeyFloat1.Close();

            if (streamFloatValue != null)
                streamFloatValue.Close();
            if (fileHeader != null)
                fileHeader.Close();
            if (fileKeyFloat != null)
                fileValueFloat.Close();

        }

        void OnGUI()
        {
          //  GUI.Window(GetInstanceID(), new Rect(5f, 40f, 500f, 400f), DrawGUI, "Window");
            GUI.Label(new Rect(0, 0, 100, 50), "This is the text string for a Label Control");

            // Make a background box
            //  string title = vessel.GetObtVelocity().ToString();
            GUI.Box(new Rect(100, 50, 350, 600), "Memory Bridge");
            GUI.Label(new Rect(150, 70, 250, 20), new GUIContent("Float Key Count"));
            GUI.Label(new Rect(300, 70, 400, 20), new GUIContent(keysFloat.Count.ToString()));
            GUI.Label(new Rect(150, 90, 250, 20), new GUIContent("Float Key Address Count"));
            GUI.Label(new Rect(300, 90, 250, 20), new GUIContent(valueAddressFloat.Count.ToString()));
            GUI.Label(new Rect(150, 110, 250, 20), new GUIContent("Bridge connected"));
            GUI.Label(new Rect(300, 110, 250, 20), new GUIContent(memoryBridge.bridgeConnected.ToString()));
            //GUI.Label(new Rect(150, 130, 250, 20), new GUIContent("Turn Status"));
            //GUI.Label(new Rect(300, 130, 250, 20), new GUIContent(Main.turnStatus.ToString()));
        }
    }
}


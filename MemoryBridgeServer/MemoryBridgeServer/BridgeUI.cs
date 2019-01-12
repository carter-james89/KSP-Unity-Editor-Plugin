using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Text;
using UnityEngine;

namespace MemoryBridgeServer
{
    class BridgeUI : MonoBehaviour
    {
        void OnGUI()
        {
            //GUI.Window(GetInstanceID(), new Rect(5f, 40f, 500f, 400f), DrawGUI, "Window");
            // GUI.Label(new Rect(0, 0, 100, 50), "This is the text string for a Label Control");


            // Make a background box
            //  string title = vessel.GetObtVelocity().ToString();
            //GUI.Box(new Rect(100, 50, 350, 600), "AI_Drone");
            //GUI.Label(new Rect(150, 70, 250, 20), new GUIContent("Float Key Count"));
            //GUI.Label(new Rect(300, 70, 400, 20), new GUIContent(Main.trgtGate.gateObject.name.ToString()));
            //GUI.Label(new Rect(150, 90, 250, 20), new GUIContent("Trgt Vector"));
            //GUI.Label(new Rect(300, 90, 250, 20), new GUIContent(VesselControl.trgtVector.trans.name.ToString()));
            //GUI.Label(new Rect(150, 110, 250, 20), new GUIContent("Trgt Vector Dist"));
            //GUI.Label(new Rect(300, 110, 250, 20), new GUIContent(VesselControl.trgtDist.ToString()));
            //GUI.Label(new Rect(150, 130, 250, 20), new GUIContent("Turn Status"));
            //GUI.Label(new Rect(300, 130, 250, 20), new GUIContent(Main.turnStatus.ToString()));
        }
    }
}

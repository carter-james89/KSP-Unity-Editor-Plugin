using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoboticController), true)]
public class RoboticControllerEditor : Editor
{
   

    public override void OnInspectorGUI()
    {
        RoboticController roboController = (RoboticController)target;

        //// roboController.curveX = AnimationCurve.EaseInOut(0, 0, roboController.strideLength, 0);


        //roboController.curveX = EditorGUILayout.CurveField("Animation on X", roboController.curveX);
       // var newStride = EditorGUILayout.FloatField("Stride Length", roboController.strideLength);
        //if(newStride != roboController.strideLength)
        //{
        //    Debug.Log("set new curve");
        //    roboController.strideLength = newStride;
        //    roboController.gaitCurve = AnimationCurve.Linear(0, 0, roboController.strideLength, roboController.strideLength);
        //   // roboController.gaitCurve.
        //}


        base.DrawDefaultInspector();
    }
}

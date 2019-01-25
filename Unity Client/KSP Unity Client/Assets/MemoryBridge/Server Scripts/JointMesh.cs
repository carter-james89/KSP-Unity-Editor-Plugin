using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



public class JointMesh : MonoBehaviour
{
    //public void On
    public void OnDrawGizmosSelected()
    {
       // Debug.Log(this.gameObject.name + " was selected");

        //var tempServoIK = transform.parent.GetComponent<RoboticServoIK>();
        //if (tempServoIK)
        //    Selection.activeGameObject = tempServoIK.gameObject;
        //else
        //{
        //    var tempMirrorServo = transform.parent.GetComponent<RoboticServoMirror>();
        //    if(tempMirrorServo)
        //        Selection.activeGameObject = tempMirrorServo.gameObject;
        //}
    }

    private void Update()
    {
       // gameObject.hideFlags = HideFlags.HideInHierarchy;
       if(Selection.activeGameObject == gameObject)
        {
            var tempServoIK = transform.parent.GetComponent<RoboticServoIK>();
            if (tempServoIK)
                Selection.activeGameObject = tempServoIK.gameObject;
            else
            {
                var tempMirrorServo = transform.parent.GetComponent<RoboticServoMirror>();
                if (tempMirrorServo)
                    Selection.activeGameObject = tempMirrorServo.gameObject;
            }
        }
    }

}

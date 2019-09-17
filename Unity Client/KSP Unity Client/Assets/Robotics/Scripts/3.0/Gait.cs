using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gait : MonoBehaviour
{
    public enum GaitTarget { MidPoint, FrontPoint, BackPoint }
    public Transform gait { get; private set; }
    public Transform pointMid { get; private set; }
    public Transform pointFront { get; private set; }
    public Transform pointBack { get; private set; }
    public Transform IKtargetTransform, rotAxis;

    public enum GaitMode { Arc, Curve }
    public GaitMode gaitMode = GaitMode.Arc;

    public AnimationCurve gaitCurve;

    private void Awake()
    {
        var obj = Instantiate(Resources.Load("Limb Target", typeof(GameObject))) as GameObject;
        IKtargetTransform = obj.transform;

        rotAxis = transform.Find("Axis Point");
        IKtargetTransform.SetParent(rotAxis);
        pointMid = transform.Find("Point Mid");
        pointFront = transform.Find("Point Front");
        pointBack = transform.Find("Point Back");
        //gait.position = limbController.limbMirror.limbEnd.position;
    }
    public void Initialize(AnimationCurve gaitCurve)
    {
        this.gaitCurve = gaitCurve;
        DefaultStrideLength();
    }

    public void DefaultStrideLength()
    {
        // pointFront.localPosition = new Vector3(0, 0, strideLength / 2);

        if (gaitMode == GaitMode.Arc)
        {
           // pointFront.localPosition = new Vector3(0, 0, strideLength / 2);
           // pointBack.localPosition = new Vector3(0, 0, (strideLength / -2));
        }
        else
        {

            pointBack.localPosition = new Vector3(0, 0, -gaitCurve.keys[gaitCurve.keys.Length - 1].time);// gaitCurve.keys[0].time);
            pointFront.localPosition = new Vector3(0, 0, gaitCurve.keys[gaitCurve.keys.Length - 1].time);
        }

        // pointBack.localPosition = new Vector3(0, 0, -strideLength / 2);
        // adjustedStride = false;
    }
}

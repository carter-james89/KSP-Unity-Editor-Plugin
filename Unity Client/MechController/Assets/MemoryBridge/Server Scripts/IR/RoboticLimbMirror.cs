using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoboticLimbMirror : RoboticLimb {
    public List<RoboticServoMirror> servosMirror;

    public void MirrorServos()
    {
        foreach (var servo in servos)
        {
            servo.MirrorServoPos();
        }
    }

}

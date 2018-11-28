using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SmoothLerp
{
    public static float Float( float previousValue, float previousTargetValue, float targetValue, float speed, float time )
    {
        float T = time * speed;
        float V = (targetValue - previousTargetValue) / T;
        float F = previousValue - previousTargetValue + V;

        return targetValue - V + F * Mathf.Exp( -T );
    }

    public static Vector3 Vector3( Vector3 prevPosition, Vector3 prevTargetPosition, Vector3 targetPosition, float speed, float time )
    {
        float X = Float( prevPosition.x, prevTargetPosition.x, targetPosition.x, speed, time );
        float Y = Float( prevPosition.y, prevTargetPosition.y, targetPosition.y, speed, time );
        float Z = Float( prevPosition.z, prevTargetPosition.z, targetPosition.z, speed, time );

        return new Vector3( X, Y, Z );
    }
}

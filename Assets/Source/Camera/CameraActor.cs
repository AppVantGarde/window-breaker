using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraActor : MonoBehaviour
{
    [Header("Camera")]
    public CameraOperator cameraOperator;

    public Transform cameraAnchor;

    public Transform cameraTarget;

    private Transform _transform;
    

    public virtual void Awake( )
    {
        _transform = GetComponent<Transform>( );
    }

    public virtual void UpdateCamera( float deltaTime )
    {
        cameraOperator.UpdateCamera( deltaTime );
    }

    public virtual void ProcessViewRotation( ref Quaternion viewRotation, ref Quaternion deltaRotation )
    {

    }

    public virtual Vector3 GetPointOfViewPosition( )
    {
        return cameraAnchor.position;
    }

    public virtual Quaternion GetPointOfViewRotation( )
    {
        Vector3 euler = cameraTarget.rotation.eulerAngles;
        euler.z = 0;

        cameraTarget.eulerAngles = euler;

        return cameraTarget.rotation;
    }
}

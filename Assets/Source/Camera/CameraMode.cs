/////////////////////////////////////////////////////////////////
// CameraMode.cs
//
// Author: Keith Wolf
/////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all of the information needed to define a camera mode.
/// </summary>
[CreateAssetMenu] public class CameraMode : ScriptableObject
{
    [Space( 10 )]
    [NonSerialized] public CameraController cameraController; // Reference to the camera controller that owns this mode.
    public float interpolationBlendTime; //The blend time to use when changing to and from this view mode.
    public Vector2 viewYawMinMax;
    public Vector2 viewPitchMinMax;
    
    [Space( 10 )][Header( "Field of View" )]
    public float fieldOfView; // The field of view for the camera to use.
    public float fovInterpSpeed;

    [Space( 10 )][Header( "Position" )]
    public bool interpolatePosition; // True means the camera will attempt to smoothly interpolate to it's new position. False will snap it to it's new position.
    public float positionInterpSpeed; // Controls the interpolation speed of position for camera origin. Ignored if 'interpolatePosition' is false.
    public bool usePerAxisPositionInterpolation; // This is a special case of origin position interpolation. If true, interpolation will be done on each axis independently, with the specified speeds. Ignored if 'interpolatePosition' is false.
    public Vector3 perAxisPositionInterpSpeed; // How fast the camera will interpolate to the camera's origin position.

    [Space( 10 )][Header( "Rotation" )]
    public bool interpolateRotation; // True means the camera will attempt to smoothly interpolate to it's new rotation. False will snap it to it's new rotation.
    public float rotationInterpSpeed; // Controls the interpolation speed of rotation for the camera origin. Ignored if 'interpolateRotation' is false.
    public bool usePerAxisRotationInterpolation; // This is a special case of origin position interpolation. If true, interpolation will be done on each axis independently, with the specified speeds. Ignored if 'interpolatePosition' is false.
    public Vector3 perAxisRotationInterpSpeed; // How fast the camera will interpolate to the camera's origin position.

    [Space( 10 )][Header( "Offsets" )]
    public bool applyDeltaViewOffsets; // Whether delta or actual view offset should be applied to the camera position.
    public bool interpViewOffsetOnlyForCamTransition; // If true pitch view offsets will only be interpolated between camera mode transitions and then be instantaneous.
    public float viewOffsetInterpSpeed; // We optionally interpolate the results of 'AdjustViewOffset( )' to prevent pops when a camera mode changes it's adjustment suddenly.

    /// <summary>
    /// An offset from the position of the view target, in the view target's local space. 
    /// Used to calculate the "worst case" camera position, which is where the camera should retreat to if tightly obstructed.
    /// </summary>
    public Vector3 worstPositionOffset;
    public Vector3 targetRelativeCameraOriginOffset; // Offset, in the camera's view target's local space, from the camera target to the camera's origin.
    public Vector3 viewOffsetHigh; // View point offset for high camera view pitch.
    public Vector3 viewOffsetMid; // View point offset for medium (horizon) camera view pitch.
    public Vector3 viewOffsetLow; // View point offset for low camera view pitch.
    
    private float _viewOffsetInterp; // Keeps track of our pitch view offset interpolation factor.
    private Vector3 _viewOffsetAdjustment;
    private Vector3 _viewOffsetAdjustmentPrevDelta; 

    /// <summary>
    /// 
    /// </summary>
    public virtual void InitializeCameraMode( )
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="previousMode"></param>
    public virtual void OnBecameActive( CameraActor viewedActor, CameraMode previousMode )
    {
        // Setup the interpolation blend time for the view offsets.
        if(interpolationBlendTime > 0.0f)
        {
            _viewOffsetInterp = 1.0f / interpolationBlendTime;
        }
        else
        {
            _viewOffsetInterp = 0.0f;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="newMode"></param>
    public virtual void OnBecameInactive( CameraActor viewedActor, CameraMode newMode )
    {

    }

    /// <summary>
    /// Interpolates from previous position and rotation towards the desired position and rotation
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="deltaTime"></param>
    /// <param name="outActualCamOriginPos"></param>
    /// <param name="outIdealCamOriginPos"></param>
    /// <param name="outActualCamOriginRot"></param>
    /// <param name="outIdealCamOriginRot"></param>
    public virtual void InterpolateCameraOrigin( CameraActor viewedActor, float deltaTime, Vector3 idealCamOriginPos, Quaternion idealCamOriginRot, out Vector3 outActualCamOriginPos, out Quaternion outActualCamOriginRot )
    {
        // This is the point in world space where camera offsets are applied. We apply lazy camera on this position, 
        // so we can have a smooth and slow interpolation speed here and use different speeds for offsets.

        // First, update the camera origin.
        if(cameraController.resetCameraInterpolation)
        {
            // No interpolation this time, snap to ideal.
            outActualCamOriginPos = idealCamOriginPos;
        }
        else
        {
            outActualCamOriginPos = InterpolateCameraOriginPos( viewedActor, viewedActor.transform.rotation, cameraController.lastActualCameraOringPos, idealCamOriginPos, deltaTime );
        }

        // Smooth out the camera's origin rotation if necessary.
        if(cameraController.resetCameraInterpolation)
        {
            // No interpolation this time, snap to ideal.
            outActualCamOriginRot = idealCamOriginRot;
        }
        else
        {
            outActualCamOriginRot = InterpolateCameraOriginRot( viewedActor, cameraController.lastActualCameraOriginRot, idealCamOriginRot, deltaTime );
        }
    }

    /// <summary>
    /// Interpolates the camera's origin from the previous position to a new ideal position.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <param name="targetRotation">The camera's target rotation. Used to create a reference frame for interpolation.</param>
    /// <param name="previousPosition">The previous position of the camera.</param>
    /// <param name="idealPosition">The ideal position for the camera this frame.</param>
    /// <param name="deltaTime">The change in time since the last frame.</param>
    /// <returns>If 'interpolatePosition' is false the ideal position is returned, otherwise an interpolation vector relative to the ideal position is returned.</returns>
    public virtual Vector3 InterpolateCameraOriginPos( CameraActor viewedActor, Quaternion targetRotation, Vector3 previousPosition, Vector3 idealPosition, float deltaTime )
    {
        if(!interpolatePosition)
        {
            // Choosing to not do any interpolation, return ideal.
            return idealPosition;
        }
        else
        {
            if(usePerAxisPositionInterpolation)
            {
                // Interpolate per-axis with per-axis speeds.
                Vector3 previousIdealPosition = cameraController.lastIdealCameraOriginPos;
                float X = SmoothLerp.Float( previousPosition.x, previousIdealPosition.x, idealPosition.x, perAxisPositionInterpSpeed.x, deltaTime );
                float Y = SmoothLerp.Float( previousPosition.y, previousIdealPosition.y, idealPosition.y, perAxisPositionInterpSpeed.z, deltaTime );
                float Z = SmoothLerp.Float( previousPosition.z, previousIdealPosition.z, idealPosition.z, perAxisPositionInterpSpeed.z, deltaTime );

                return new Vector3( X, Y, Z );
            }
            else
            {

                // Apply lazy camera effect to the camera origin position.
                Vector3 previousIdealPosition = cameraController.lastIdealCameraOriginPos;
                float X = SmoothLerp.Float( previousPosition.x, previousIdealPosition.x, idealPosition.x, positionInterpSpeed, deltaTime );
                float Y = SmoothLerp.Float( previousPosition.y, previousIdealPosition.y, idealPosition.y, positionInterpSpeed, deltaTime );
                float Z = SmoothLerp.Float( previousPosition.z, previousIdealPosition.z, idealPosition.z, positionInterpSpeed, deltaTime );

                return new Vector3( X, Y, Z );
            }
        }
    }

    /// <summary>
    /// Interpolates the camera's origin from the previous rotation to a new ideal rotation.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <param name="previousRotation">The previous rotation of the camera.</param>
    /// <param name="idealRotation">The ideal rotation for the camera this frame.</param>
    /// <param name="deltaTime">The change in time since the last frame.</param>
    /// <returns>If 'interpolateRotation' is false the ideal rotation is returned, otherwise an interpolated rotation relative to the ideal rotation is returned.</returns>
    public virtual Quaternion InterpolateCameraOriginRot( CameraActor viewedActor, Quaternion previousRotation, Quaternion idealRotation, float deltaTime )
    {
        if(!interpolateRotation)
        {
            // Choosing not to do any interpolation, return ideal.
            return idealRotation;
        }
        else
        {
            if(usePerAxisRotationInterpolation)
            {
                float X = Mathf.LerpAngle( previousRotation.eulerAngles.x, idealRotation.eulerAngles.x, perAxisRotationInterpSpeed.x * deltaTime );
                float Y = Mathf.LerpAngle( previousRotation.eulerAngles.y, idealRotation.eulerAngles.y, perAxisRotationInterpSpeed.y * deltaTime );
                float Z = Mathf.LerpAngle( previousRotation.eulerAngles.z, idealRotation.eulerAngles.z, perAxisRotationInterpSpeed.z * deltaTime );

                return Quaternion.Euler( X, Y, Z );
            }
            else
            {
                float X = Mathf.LerpAngle( previousRotation.eulerAngles.x, idealRotation.eulerAngles.x, rotationInterpSpeed * deltaTime );
                float Y = Mathf.LerpAngle( previousRotation.eulerAngles.y, idealRotation.eulerAngles.y, rotationInterpSpeed * deltaTime );
                float Z = Mathf.LerpAngle( previousRotation.eulerAngles.z, idealRotation.eulerAngles.z, rotationInterpSpeed * deltaTime );

                return Quaternion.Euler( X, Y, Z );
            }
        }
    }

    /// <summary>
    /// Allows the camera mode to make any final situational adjustments to the base view offset.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <param name="offset">The camera's base offset.</param>
    /// <returns>The adjust offset.</returns>
    public virtual Vector3 AdjustViewOffset( CameraActor viewedActor, Vector3 offset )
    {
        // We don't make any adjustments by default.
        return offset;
    }

    /// <summary>
    /// Allows the camera mode to make any final situational adjustments to the base view offset.
    /// </summary>
    /// <param name="viewTarget">The camera's view target</param>
    /// <param name="cameraOrigin"></param>
    /// <param name="actualViewOffset"></param>
    /// <param name="deltaViewOffset"></param>
    /// <returns></returns>
    public virtual Vector3 ApplyViewOffset( CameraActor viewedActor, Vector3 cameraOrigin, Vector3 actualViewOffset, Vector3 deltaViewOffset )
    {
        if(applyDeltaViewOffsets)
        {
            return cameraOrigin + deltaViewOffset;
        }
        else
        {
            return cameraOrigin + actualViewOffset;
        }
    }

    /// <summary>
    /// Allows the camera mode to modify the view rotation before it's applied to the camera.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <param name="viewRotation">The current view rotation.</param>
    /// <param name="deltaRotation">The change in rotation being applied this frame.</param>
    /// <param name="deltaTime">Change in time since the last frame.</param>
    public virtual void ProcessViewRotation( CameraActor viewedActor, ref Quaternion viewRotation, ref Quaternion deltaRotation, float deltaTime )
    {
        deltaRotation.z = 0;
        //deltaRotation = Quaternion.identity;

        //viewRotation *= deltaRotation;
        viewRotation = deltaRotation;

        float P = viewRotation.eulerAngles.x;
        float Y = viewRotation.eulerAngles.y;
        float R = viewRotation.eulerAngles.z;

        P = ClampAngle( P, viewPitchMinMax.x, viewPitchMinMax.y );
        Y = ClampAngle( Y, viewYawMinMax.x, viewYawMinMax.y );

        viewRotation = Quaternion.Euler( P, Y, R );
    }

    /// <summary>
    /// Gets the "worst-case" camera position for this camera mode. This is the position that the camera
    /// penetration avoidance is based off of, so it should be a guaranteed safe place to put the camera.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <returns>Safe position to place the camera in a "worst-case" scenario.</returns>
    public virtual Vector3 GetCameraWorstCasePos( CameraActor viewedActor, ref ViewTarget viewTarget )
    {
        return Vector3.zero;//viewedActor.GetViewLocation( ) + (viewedActor.transform.rotation * worstPositionOffset);//new Vector3(worstPositionOffsetX, worstPositionOffsetY, worstPositionOffsetZ) );
    }

    /// <summary>
    /// Gets the field of view this camera mode desires.
    /// </summary>
    /// <returns></returns>
    public virtual float GetDesiredFieldOfView( )
    {
        return fieldOfView;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="viewRotation"></param>
    /// <returns></returns>
    protected virtual float GetViewPitch( CameraActor viewedActor, Quaternion viewRotation )
    {
        return viewRotation.eulerAngles.x;
    }

    /// <summary>
    /// Calculates and returns the ideal view offset for the specified camera mode. The offset is relative to the
    /// Camera's position and rotation, and calculated by interpolating 2 ideal view points based on the targets view pitch.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <param name="deltaTime">Change in time since last frame.</param>
    /// <param name="viewOrigin">Position of the camera.</param>
    /// <param name="viewRotation">Rotation of the camera.</param>
    /// <returns></returns>
    public virtual Vector3 GetViewOffset( CameraActor viewedActor, float deltaTime, Vector3 viewOrigin, Quaternion viewRotation )
    {
        Vector3 outOffset = Vector3.zero;
        Vector3 highOffset = Vector3.zero;
        Vector3 midOffset = Vector3.zero;
        Vector3 lowOffset = Vector3.zero;

        // Get our three view offsets.
        GetBaseViewOffsets( viewedActor, deltaTime, out highOffset, out midOffset, out lowOffset );

        Vector3 viewPosition = viewedActor.GetPointOfViewPosition( );
        Vector3 relativeOffset = GetTargetRelativeOriginOffset( viewedActor );
        Quaternion actorRotation = viewedActor.GetPointOfViewRotation( );
        

        debugSpheres.Add( new DebugSphereInfo( ) { position = viewPosition + viewRotation * (relativeOffset + highOffset), radius = 0.075f, color = Color.black } );
        debugSpheres.Add( new DebugSphereInfo( ) { position = viewPosition + viewRotation * (relativeOffset + midOffset), radius = 0.075f, color = Color.black } );
        debugSpheres.Add( new DebugSphereInfo( ) { position = viewPosition + viewRotation * (relativeOffset + lowOffset), radius = 0.075f, color = Color.black } );

        // Calculate final offset based on camera pitch.
        float pitch = GetViewPitch( viewedActor, viewRotation );

        if(pitch < (350 - Mathf.Abs( viewPitchMinMax.x )))
        {
            float percentage = pitch / viewPitchMinMax.y;
            outOffset = Vector3.Lerp( midOffset, highOffset, percentage );
        }
        else
        {
            float percentage = (360 - pitch) / Mathf.Abs( viewPitchMinMax.x );
            outOffset = Vector3.Lerp( midOffset, lowOffset, percentage );
        }

        // Give the camera mode a chance to make any situational offset adjustment.
        Vector3 offsetPreAdjustment = outOffset;

        outOffset = AdjustViewOffset( viewedActor, outOffset );

        Vector3 adjustmentDelta = outOffset - offsetPreAdjustment;

        // Are we doing a seamless pivot transition?
        if(!cameraController.resetCameraInterpolation && cameraController.doSeamlessPivotTransition)
        {
            _viewOffsetAdjustment.x = SmoothLerp.Float( _viewOffsetAdjustment.x, _viewOffsetAdjustmentPrevDelta.x, adjustmentDelta.x, viewOffsetInterpSpeed, deltaTime );
            _viewOffsetAdjustment.y = SmoothLerp.Float( _viewOffsetAdjustment.y, _viewOffsetAdjustmentPrevDelta.y, adjustmentDelta.y, viewOffsetInterpSpeed, deltaTime );
            _viewOffsetAdjustment.z = SmoothLerp.Float( _viewOffsetAdjustment.z, _viewOffsetAdjustmentPrevDelta.z, adjustmentDelta.z, viewOffsetInterpSpeed, deltaTime );

            // Cache the previous target adjustment delta.
            _viewOffsetAdjustmentPrevDelta = adjustmentDelta;
        }
        else
        {
            _viewOffsetAdjustment = adjustmentDelta;
        }

        // Finalize the offset.
        outOffset = offsetPreAdjustment + _viewOffsetAdjustment;

        debugSpheres.Add( new DebugSphereInfo( ) { position = outOffset, radius = 0.1f, color = Color.white } );

        return outOffset;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <returns></returns>
    public virtual Quaternion GetViewOffsetRotationBase( CameraActor viewedActor )
    {
        return viewedActor.transform.rotation;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    public float GetViewOffsetInterpSpeed( CameraActor viewedActor, float deltaTime )
    {
        float result = 0.0f;

        if(viewedActor != null)
        {
            float blendTime = GetInterpolationBlendTime( );
            if(blendTime > 0.0f)
            {
                result = 1.0f / blendTime;
            }
        }

        // If we interpolate view offsets only for camera transitions, ramp up the interpolation
        // factor over time, so it eventually doesn't interpolate anymore.
        if(interpViewOffsetOnlyForCamTransition && result > 0.0f)
        {
            _viewOffsetInterp += result * deltaTime;
            _viewOffsetInterp = Math.Min( _viewOffsetInterp, 10000.0f );
            return _viewOffsetInterp;
        }

        // No interpolation.
        return result;
    }

    /// <summary>
    /// Gets the pitch view offsets.
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="deltaTime"></param>
    /// <param name="outHigh"></param>
    /// <param name="outMid"></param>
    /// <param name="outLow"></param>
    public virtual void GetBaseViewOffsets( CameraActor viewedActor, float deltaTime, out Vector3 outHigh, out Vector3 outMid, out Vector3 outLow )
    {
        //Vector3 runOffsetDelta = Vector3.zero;
        //Vector3 strafeOffsetDelta = Vector3.zero;
        //float velocityMag = 0;//viewedActor.GetVelocity( ).magnitude;

        //if(velocityMag > 0.0f)
        //{
        //    Vector3 X = viewedActor.transform.rotation * Vector3.right;
        //    Vector3 Y = viewedActor.transform.rotation * Vector3.up;
        //    Vector3 Z = viewedActor.transform.rotation * Vector3.forward;
        //    Vector3 normalVel = Vector3.zero;//viewedActor.GetVelocity( ) / velocityMag;

        //    if(strafeOffsetScalingThreshold > 0.0f)
        //    {
        //        float XDot = Vector3.Dot( X, normalVel );
        //        strafeOffsetDelta = XDot < 0.0f ? new Vector3( strafeOffset.x, 0, 0 ) * -XDot : new Vector3( strafeOffset.y, 0, 0 ) * XDot;
        //        strafeOffsetDelta *= Mathf.Clamp( velocityMag / strafeOffsetScalingThreshold, 0.0f, 1.0f );
        //    }

        //    if(runOffsetScalingThreshold > 0.0f)
        //    {
        //        float ZDot = Vector3.Dot( Z, normalVel );
        //        runOffsetDelta = ZDot < 0.0f ? new Vector3( 0, 0, runOffset.y ) * -ZDot : new Vector3( 0, 0, runOffset.x ) * ZDot;
        //        runOffsetDelta *= Mathf.Clamp( velocityMag / runOffsetScalingThreshold, 0.0f, 1.0f );
        //    }
        //}

        //float strafeInterpSpeed = strafeOffsetDelta == Vector3.zero ? strafeOffsetInterpSpeed.y : strafeOffsetInterpSpeed.x;
        //_strafeOffset.x = FSmoothLerp( _strafeOffset.x, _strageOffsetPrevDelta.x, strafeOffsetDelta.x, strafeInterpSpeed, deltaTime );
        //_strafeOffset.y = FSmoothLerp( _strafeOffset.y, _strageOffsetPrevDelta.y, strafeOffsetDelta.y, strafeInterpSpeed, deltaTime );
        //_strafeOffset.z = FSmoothLerp( _strafeOffset.z, _strageOffsetPrevDelta.y, strafeOffsetDelta.z, strafeInterpSpeed, deltaTime );
        //_strageOffsetPrevDelta = strafeOffsetDelta;

        //float runInterpSpeed = runOffsetDelta == Vector3.zero ? runOffsetInterpSpeed.y : runOffsetInterpSpeed.x;
        //_runOffset.x = FSmoothLerp( _runOffset.x, _runOffsetPrevDelta.x, runOffsetDelta.x, runInterpSpeed, deltaTime );
        //_runOffset.y = FSmoothLerp( _runOffset.y, _runOffsetPrevDelta.y, runOffsetDelta.y, runInterpSpeed, deltaTime );
        //_runOffset.z = FSmoothLerp( _runOffset.z, _runOffsetPrevDelta.y, runOffsetDelta.z, runInterpSpeed, deltaTime );
        //_runOffsetPrevDelta = runOffsetDelta;

        //Vector3 totalOffset = Vector3.zero;//_strafeOffset + _runOffset;
        //Quaternion cameraRotation = viewedActor.transform.rotation;



        //// Apply the offsets
        //outHigh = (cameraRotation * (viewOffsetHigh + totalOffset));
        //outMid = (cameraRotation * (viewOffsetMid + totalOffset));
        //outLow = (cameraRotation * (viewOffsetLow + totalOffset));

        //Vector3 viewPosition = viewedActor.GetPointOfViewPosition( );
        //Vector3 relativeOffset = GetTargetRelativeOriginOffset( viewedActor );

        Quaternion viewRotation = viewedActor.GetPointOfViewRotation( );

        //viewPosition += viewRotation * relativeOffset;

        outHigh = viewOffsetHigh;
        outMid = viewOffsetMid;
        outLow = viewOffsetLow;
    }

    /// <summary>
    /// Gets the offset the view target wants applied to the camera origin.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <returns></returns>
    public virtual Vector3 GetTargetRelativeOriginOffset( CameraActor viewedActor )
    {
        return targetRelativeCameraOriginOffset;
    }

    /// <summary>
    /// Get the position and rotation in world space of the camera's base point. The camera will rotate around this point.
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="outCameraOriginPos"></param>
    /// <param name="outCameraOriginRot"></param>
    public virtual void GetCameraOrigin( CameraActor viewedActor, out Vector3 outCameraOriginPos, out Quaternion outCameraOriginRot )
    {
        // Rotation.
        if(viewedActor != null)// &&cameraController.resetCameraInterpolation || LockedToViewTarget( viewedActor ))
        {
            //outCameraOriginRot = viewedActor.GetViewRotation( );
            //Quaternion strippedRotation = viewedActor.GetViewRotation( );
            //strippedRotation.y = 0;
            //strippedRotation.z = 0;

            outCameraOriginRot = viewedActor.GetPointOfViewRotation( );
            //outCameraOriginRot = Quaternion.Euler( outCameraOriginRot.eulerAngles.x, 0, 0 );
            //outCameraOriginRot.eulerAngles = strippedRotation.eulerAngles;     
        }
        else
        {
            // Use the camera's rotation.
            outCameraOriginRot = cameraController.transform.rotation;
        }

        // Get the origin position for this view target.
        outCameraOriginPos = viewedActor.GetPointOfViewPosition( );

        // Apply any position offset.
        outCameraOriginPos += outCameraOriginRot * GetTargetRelativeOriginOffset( viewedActor );
    }

    /// <summary>
    /// Gets the time to interpolate position and rotation changes.
    /// </summary>
    /// <returns></returns>
    public virtual float GetInterpolationBlendTime( )
    {
        return interpolationBlendTime;
    }

    /// <summary>
    /// Gets the time to interpolate field of view changes.
    /// </summary>
    /// <returns></returns>
    public virtual float GetFieldOfViewBlendTime( )
    {
        return fovInterpSpeed > 0.0f ? fovInterpSpeed : 1.0f;
    }

    public void OnDrawGizmos( )
    {
        for(int i = 0; i < debugSpheres.Count; i++)
        {
            Gizmos.color = debugSpheres[i].color;
            Gizmos.DrawWireSphere( debugSpheres[i].position, debugSpheres[i].radius );
        }

        debugSpheres.Clear( );
    }

    private List<DebugSphereInfo> debugSpheres = new List<DebugSphereInfo>( );
    private struct DebugSphereInfo
    {
        public float radius;
        public Vector3 position;
        public Color color;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public float ClampAngle( float angle, float min, float max )
    {
        angle = Mathf.Repeat( angle, 360 );
        min = Mathf.Repeat( min, 360 );
        max = Mathf.Repeat( max, 360 );
        bool inverse = false;
        var tmin = min;
        var tangle = angle;
        if(min > 180)
        {
            inverse = !inverse;
            tmin -= 180;
        }
        if(angle > 180)
        {
            inverse = !inverse;
            tangle -= 180;
        }
        var result = !inverse ? tangle > tmin : tangle < tmin;
        if(!result)
            angle = min;

        inverse = false;
        tangle = angle;
        var tmax = max;
        if(angle > 180)
        {
            inverse = !inverse;
            tangle -= 180;
        }
        if(max > 180)
        {
            inverse = !inverse;
            tmax -= 180;
        }

        result = !inverse ? tangle < tmax : tangle > tmax;
        if(!result)
            angle = max;
        return angle;
    }
}
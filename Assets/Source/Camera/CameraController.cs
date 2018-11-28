/////////////////////////////////////////////////////////////////
// CameraController.cs
//
// Author: Keith Wolf
/////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for defining specific camera algorithms.
/// </summary>
public class CameraController : MonoBehaviour
{
    public CameraMode idleCameraMode;

    [NonSerialized] public CameraMode currentCameraMode; // The current camera mode.
    public float originOffsetInterpSpeed; // Origin offset interpolation speed.
    public bool doSeamlessPivotTransition; // Set to true if you want to keep the camera in place across a transition when the pivot makes a big jump.
    public bool forceWorstPosition;
    [NonSerialized] public bool resetCameraInterpolation; // Reset the camera's interpolation. Set on the first frame and teleports to prevent long distance or wrong camera interpolation.

    /// <summary>
    /// Last view target's relative offset, for slow offsets interpolation.
    /// This is because this offset is relative to the view target's rotation, which can change abruptly.
    /// This is used to adjust the camera origin in case of evading, leaning, popping up from cover, reloading, etc.
    /// </summary>
    [NonSerialized] public Vector3 lastActualOriginPosOffset;

    /// <summary>
    /// Last actual camera origin position, for lazy camera interpolation. This is only applied to the 
    /// view target's origin, not view offsets, for faster and smoother responses.
    /// </summary>
    [NonSerialized] public Vector3 lastActualCameraOringPos;

    /// <summary>
    /// Last actual camera origin rotation, for lazy camera interpolation. This is only applied to the 
    /// view target's origin, not view offsets, for faster and smoother responses.
    /// </summary>
    [NonSerialized] public Quaternion lastActualCameraOriginRot;

    [NonSerialized] public Vector3 lastViewOffset; // The relative view offset. This offset is relative to the view target's rotation, mainly used for pitch positioning.
    [NonSerialized] public Vector3 lastIdealCameraOriginPos; // Last ideal camera origin position.
    [NonSerialized] public Quaternion lastIdealCameraOriginRot; // Last ideal camera origin rotation.
    [NonSerialized] public float lastCameraFieldOfView; // The last field of view the camera used.
    [NonSerialized] public float lastPitchAdjustment; // Last adjusted pitch, for smooth blend out.
    [NonSerialized] public float lastYawAdjustment; // Last adjusted yaw, for smooth blend out.

    private float _leftOverPitchAdjustment; // Pitch adjustment when keeping target is done in 2 parts.
    protected Vector3 lastPreModifierCameraPos; // Last position of the camera, before the camera modifiers are applied.
    protected Quaternion lastPreModifiedCameraRot; // Last rotation of the camera, before the camera modifiers are applied.

    /// <summary>
    /// 
    /// </summary>
    public CameraController( )
    {
        originOffsetInterpSpeed = 1.0f;
        resetCameraInterpolation = true;
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual void Initialize( )
    {

    }

    public void OnDrawGizmos( )
    {
        if( Application.isPlaying)
            currentCameraMode.OnDrawGizmos( );
    }

    ///// <summary>
    ///// Called when this camera controller becomes active.
    ///// </summary>
    ///// <param name="oldCameraController">The camera controller that was active before this one.</param>
    //public virtual void OnBecameActive( FBCameraController oldCameraController )
    //{
    //    if(!gameplayCamera.interpolateCameraSwitching)
    //    {
    //        ResetInterpolation( );
    //    }
    //}

    ///// <summary>
    ///// Called when this camera controller becomes inactive.
    ///// </summary>
    ///// <param name="newCameraController">The camera controller that is now active.</param>
    //public virtual void OnBecameInactive( FBCameraController newCameraController )
    //{

    //}

    /// <summary>
    /// Updates the controller's point of view. (Position, Rotation, FieldOfView)
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="deltaTime">Change in time since the last frame.</param>
    public void UpdateCamera( CameraActor viewedActor, ref ViewTarget viewTarget, float deltaTime )
    {
        // Update the current camera mode.
        UpdateCameraMode( viewedActor );

        if(currentCameraMode == null)
        {
            // No camera mode!!!
            return;
        }

        Vector3 idealCameraOriginPosition;
        Quaternion idealCameraOriginRotation;
        currentCameraMode.GetCameraOrigin( viewedActor, out idealCameraOriginPosition, out idealCameraOriginRotation );

        Quaternion actualCameraOriginRotation;
        Vector3 actualCameraOriginPosition;
        InterpolateCameraOrigin( viewedActor, deltaTime, idealCameraOriginPosition, idealCameraOriginRotation, out actualCameraOriginPosition, out actualCameraOriginRotation );

        lastIdealCameraOriginPos = idealCameraOriginPosition;
        lastIdealCameraOriginRot = idealCameraOriginRotation;
        lastActualCameraOringPos = actualCameraOriginPosition;
        lastActualCameraOriginRot = actualCameraOriginRotation;

        // Add any extra offset that needs to be applied after the interpolation.
        actualCameraOriginPosition += GetPostInterpCameraOriginPositionOffset( viewTarget );
        actualCameraOriginRotation *= GetPostInterpCameraOriginRotationOffset( viewTarget );

        // Get the camera-space offset from the camera origin.
        Vector3 idealViewOffset = currentCameraMode.GetViewOffset( viewedActor, deltaTime, actualCameraOriginPosition, actualCameraOriginRotation );

        //
        viewTarget.pointOfView.rotation = actualCameraOriginRotation;

        // Get the desired field of view.
        if(!resetCameraInterpolation)
        {
            float fovDelta = GetDesiredFieldOfView( );
            float fovInterpSpeed = currentCameraMode.GetFieldOfViewBlendTime( );

            //actualFieldOfView = FSmoothLerp( actualFieldOfView, actualFieldOfViewPrevDelta, fovDelta, fovInterpSpeed, deltaTime );

            actualFieldOfViewPrevDelta = fovDelta;
            viewTarget.pointOfView.fieldOfView = actualFieldOfView;
        }
        else
        {
            float fovDelta = GetDesiredFieldOfView( );

            actualFieldOfView = fovDelta;
            actualFieldOfViewPrevDelta = fovDelta;
            viewTarget.pointOfView.fieldOfView = fovDelta;
        }

        // View relative offset.
        Vector3 deltaViewOffset;
        {
            if(doSeamlessPivotTransition)
            {
                // When pivot makes a big jump and we want to keep the camera in-place, we need to re-base
                // the lastViewOffset from the old pivot to the new pivot.

                lastViewOffset = transform.position - actualCameraOriginPosition;
            }

            float interpSpeed = currentCameraMode.interpolationBlendTime;//GetViewOffsetInterpSpeed( viewedActor, deltaTime );
            if(interpSpeed > 0.0f)//!resetCameraInterpolation )//&& interpSpeed > 0.0f)
            {
                // Interpolation might feel better for big swings.
                actualViewOffset.x = SmoothLerp.Float( lastViewOffset.x, actualViewOffsetPrevDelta.x, idealViewOffset.x, deltaTime, interpSpeed );
                actualViewOffset.y = SmoothLerp.Float( lastViewOffset.y, actualViewOffsetPrevDelta.y, idealViewOffset.y, deltaTime, interpSpeed );
                actualViewOffset.z = SmoothLerp.Float( lastViewOffset.z, actualViewOffsetPrevDelta.z, idealViewOffset.z, deltaTime, interpSpeed );

                actualViewOffsetPrevDelta = idealViewOffset;
            }
            else
            {
                actualViewOffset = idealViewOffset;
            }

            deltaViewOffset = (actualViewOffset - lastViewOffset);
            lastViewOffset = actualViewOffset;
        }



        // Apply view offsets.
        Vector3 desiredCamPos = actualCameraOriginPosition + actualCameraOriginRotation * actualViewOffset;//currentCameraMode.ApplyViewOffset( viewedActor, actualCameraOriginPosition, actualViewOffset, deltaViewOffset );

        // Set the new camera position.
        viewTarget.pointOfView.position = desiredCamPos;

        // Cache this for potential use later.
        lastPreModifierCameraPos = viewTarget.pointOfView.position;
        lastPreModifiedCameraRot = viewTarget.pointOfView.rotation;

        /////////////////////////////////////////
        // Apply camera modifiers.
        /////////////////////////////////////////

        // Are we skipping collision?
        //if(!currentCameraMode.skipCameraCollision)
        //{
        //    // Get the worst possible position for this camera.
        //    Vector3 worstPosition = currentCameraMode.GetCameraWorstCasePos( viewedActor, ref viewTarget );

        //    // We can predict camera collision by shooting multiple arrays.
        //    bool singleRayPenetrationCheck = !ShouldDoPredictivePenetrationAvoidance( );

        //    // 
        //    PreventCameraPenetration( viewTarget, worstPosition, ref viewTarget.pointOfView.position, deltaTime, singleRayPenetrationCheck );

        //    if(forceWorstPosition)
        //        viewTarget.pointOfView.position = worstPosition;
        //}

        // If we had to reset camera interpolation, then turn off flag once it's been processed.
        resetCameraInterpolation = false;
    }

    Vector3 actualViewOffset;
    Vector3 actualViewOffsetPrevDelta;

    float actualFieldOfView;
    float actualFieldOfViewPrevDelta;

    /// <summary>
    /// Interpolates from previous position and rotation towards the desired position and rotation
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="deltaTime"></param>
    /// <param name="idealCamOriginPos"></param>
    /// <param name="idealCamOriginRot"></param>
    /// <param name="outActualCamOriginPos"></param>
    /// <param name="outActualCamOriginRot"></param>
    public virtual void InterpolateCameraOrigin( CameraActor viewedActor, float deltaTime, Vector3 idealCamOriginPos, Quaternion idealCamOriginRot, out Vector3 outActualCamOriginPos, out Quaternion outActualCamOriginRot )
    {
        currentCameraMode.InterpolateCameraOrigin( viewedActor, deltaTime, idealCamOriginPos, idealCamOriginRot, out outActualCamOriginPos, out outActualCamOriginRot );
    }

    /// <summary>
    /// Get the focus position, adjusted to compensate for camera offsets.
    /// </summary>
    /// <param name="cameraPosition"></param>
    /// <param name="focusPosition"></param>
    /// <param name="viewOffset"></param>
    /// <returns></returns>
    public Vector3 GetEffectiveFocusPos( Vector3 cameraPosition, Vector3 focusPosition, Vector3 viewOffset )
    {
        float yawDelta = 0.0f;
        float yawDist = 0.0f;
        float pitchDelta = 0.0f;
        float pitchDist = 0.0f;

        Vector3 camToFocus = focusPosition - cameraPosition;
        float camToFocusSize = camToFocus.magnitude;

        // Yaw
        {
            Vector3 viewOffset3DNorm = viewOffset;
            viewOffset3DNorm.y = 0.0f;
            float viewOffset3DSize = viewOffset3DNorm.magnitude;
            viewOffset3DNorm /= viewOffset3DSize;

            float dotProd = Vector3.Dot( viewOffset3DNorm, Vector3.forward );
            if(dotProd < 0.999f && dotProd > -0.999f)
            {
                float alpha = Mathf.PI - Mathf.Acos( dotProd );
                float sinTheta = viewOffset3DSize * Mathf.Sin( alpha ) / camToFocusSize;
                float theta = Mathf.Asin( sinTheta );

                yawDelta = theta;
                if(viewOffset.x > 0.0f)
                {
                    yawDelta = -yawDelta;
                }

                float phi = Mathf.PI - theta - alpha;
                yawDist = viewOffset3DSize * Mathf.Sin( phi ) / sinTheta - camToFocusSize;
            }
        }

        // Pitch
        {
            Vector3 viewOffset3DNorm = viewOffset;
            viewOffset3DNorm.x = 0.0f;
            float viewOffset3DSize = viewOffset3DNorm.magnitude;
            viewOffset3DNorm /= viewOffset3DSize;

            float dotProd = Vector3.Dot( viewOffset3DNorm, Vector3.forward );
            if(dotProd < 0.999f && dotProd > -0.999f)
            {
                float alpha = Mathf.PI - Mathf.Acos( dotProd );
                float sinTheta = viewOffset3DSize * Mathf.Sin( alpha ) / camToFocusSize;
                float theta = Mathf.Asin( sinTheta );

                pitchDelta = theta;
                if(viewOffset.y > 0.0f)
                {
                    pitchDelta = -pitchDelta;
                }

                float phi = Mathf.PI - theta - alpha;
                pitchDist = viewOffset3DSize * Mathf.Sin( phi ) / Mathf.Sin( theta ) - camToFocusSize;
            }
        }

        float dist = camToFocusSize + pitchDist + yawDist;
        Quaternion camToFocusRot = Quaternion.LookRotation( camToFocus );

        Vector3 pitchAxis = camToFocusRot * Vector3.right;
        Vector3 yawAxis = camToFocusRot * Vector3.up;

        Vector3 adjustedCamVec = Quaternion.AngleAxis( yawDelta, yawAxis ) * camToFocus;
        adjustedCamVec = Quaternion.AngleAxis( -pitchDelta, pitchAxis ) * adjustedCamVec;
        adjustedCamVec.Normalize( );

        return cameraPosition + adjustedCamVec * dist;
    }

    /// <summary>
    /// Gets the difference in world space angles in [-PI,PI] range
    /// </summary>
    /// <param name="angle1">Angle One.</param>
    /// <param name="angle2">Angle Two</param>
    /// <returns></returns>
    private float FindDeltaAngle( float angle1, float angle2 )
    {
        float delta;

        // Find the difference
        delta = angle2 - angle1;
        // If change is larger than PI
        if(delta > Mathf.PI)
        {
            // Flip to negative equivalent
            delta = delta - (Mathf.PI * 2.0f);
        }
        else if(delta < -Mathf.PI)
        {
            // Otherwise, if change is smaller than -PI
            // Flip to positive equivalent
            delta = delta + (Mathf.PI * 2.0f);
        }

        // Return delta in [-PI,PI] range
        return delta;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private float GetHeadingAngle( Vector3 direction )
    {
        float angle = Mathf.Acos( Mathf.Clamp( direction.z, -1.0f, 1.0f ) );
        if(direction.x < 0.0f)
        {
            angle *= -1.0f;
        }

        return angle;
    }

    // Unreal (X = Roll/Forward,  Y = Pitch/Right, Z = Yaw/Up)
    // Unity  (X = Pitch/Right, Y = Yaw/Up,   Z = Roll/Forward)

    /// <summary>
    /// Handles traces to make sure the camera does not penetrate geometry and tries to find the best position for the camera.
    /// Also interpolation back to the ideal or desired position is handled here.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <param name="worstPosition">Worst position. (Start Trace)</param>
    /// <param name="desiredPosition">Desired or ideal position for the camera. (End Trace)</param>
    /// <param name="deltaTime">The change in time since the last frame.</param>    /// <param name="cameraExtentScale">Scale camera extent. Used for box collision.</param>
    /// <param name="singleRayOnly">Only fire a single ray. Do not send out extra predicitve feelers.</param>
    public void PreventCameraPenetration( ViewTarget viewTarget, Vector3 worstPosition, ref Vector3 desiredPosition, float deltaTime, bool singleRayOnly )
    {
//        float hardBlockPct = _penetrationBlockPctPrevFrame;
//        float softBlockPct = _penetrationBlockPctPrevFrame;
//        float distBlockedPctThisFrame = 1.0f;




//        //List<FBPenetrationAvoidanceFeeler> tempFeelers = new List<FBPenetrationAvoidanceFeeler>( );
//        //tempFeelers.Add( new FBPenetrationAvoidanceFeeler( ) { direction = -Vector3.forward, extent = 3 } );
//        //tempFeelers.Add( new FBPenetrationAvoidanceFeeler( ) { direction = -Vector3.right, rotation = new Vector3( 0, -35, 0 ), extent = 3 } );
//        //tempFeelers.Add( new FBPenetrationAvoidanceFeeler( ) { direction = -Vector3.right, rotation = new Vector3( 0, -65, 0 ), extent = 3 } );
//        //tempFeelers.Add( new FBPenetrationAvoidanceFeeler( ) { direction = Vector3.right, rotation = new Vector3( 0, 35, 0 ), extent = 3 } );
//        //tempFeelers.Add( new FBPenetrationAvoidanceFeeler( ) { direction = Vector3.right, rotation = new Vector3( 0, 65, 0 ), extent = 3 } );
//        //penetrationFeelers = tempFeelers;

//        int numRaysToShoot = penetrationFeelers.Count;//singleRayOnly ? Mathf.Min(1, penetrationFeelers.Count) : penetrationFeelers.Count;
//        for(int i = 0; i < numRaysToShoot; i++)
//        {
//            FBPenetrationAvoidanceFeeler feeler = penetrationFeelers[i];//penetrationFeelers[i];
//            if(feeler.framesUntilNextRayCast <= 0)
//            {
//                // Reset the interval for this feeler.
//                feeler.framesUntilNextRayCast = feeler.rayCastInterval;

//                // Grab some info from the camera.
//                Vector3 camPosition = worstPosition;//gameplayCamera.transform.position;
//                Quaternion camRotation = gameplayCamera.transform.rotation;

//                // Create the direction for the ray cast.
//                Vector3 rayDirection = Quaternion.Euler( feeler.rotation ) * camRotation * feeler.direction;
//#if UNITY_EDITOR        
//                Debug.DrawRay( camPosition, rayDirection, Color.white );
//#endif
//                RaycastHit hitInfo;
//                if(Physics.Raycast( camPosition, rayDirection, out hitInfo, feeler.extent ))//, 1 << LayerMask.NameToLayer( "Default" ) ))
//                {
//                    if(hitInfo.collider != null && !hitInfo.collider.isTrigger && hitInfo.collider.tag != "Player")
//                    {
//                        // This feeler got a hit, so do another ray cast next frame.
//                        feeler.framesUntilNextRayCast = 0;
//#if UNITY_EDITOR
//                        Debug.DrawLine( camPosition, hitInfo.point, Color.red );
//#endif
//                        float newBlockPct = hitInfo.distance / (desiredPosition - worstPosition).magnitude;

//                        distBlockedPctThisFrame = Mathf.Min( newBlockPct, distBlockedPctThisFrame );
//                    }
//                }

//                // If the main ray collided, then snap!
//                if(i == 0)
//                {
//                    hardBlockPct = distBlockedPctThisFrame;
//                }
//                else softBlockPct = distBlockedPctThisFrame;
//            }
//            else feeler.framesUntilNextRayCast--;
//        }

//        if(_penetrationBlockPctPrevFrame < distBlockedPctThisFrame)
//        {
//            _penetrationBlockPct = FSmoothLerp( _penetrationBlockPct, _penetrationBlockPctPrevDelta, distBlockedPctThisFrame, penetrationBlendOutSpeed, deltaTime );
//            _penetrationBlockPctPrevDelta = distBlockedPctThisFrame;
//        }
//        else if(_penetrationBlockPctPrevFrame > hardBlockPct)
//        {
//            _penetrationBlockPct = FSmoothLerp( _penetrationBlockPct, _penetrationBlockPctPrevDelta, hardBlockPct, penetrationBlendInHardSpeed, deltaTime );
//            _penetrationBlockPctPrevDelta = hardBlockPct;
//        }
//        else if(_penetrationBlockPctPrevFrame > softBlockPct)
//        {
//            //float softSpeed = penetrationBlendInSoftSpeed * (1 - _penetrationBlockPct);
//            //Debug.Log( "Pct: " + (1 - _penetrationBlockPct) + " Speed: " + softSpeed );
//            _penetrationBlockPct = FSmoothLerp( _penetrationBlockPct, _penetrationBlockPctPrevDelta, softBlockPct, penetrationBlendInSoftSpeed, deltaTime );
//            _penetrationBlockPctPrevDelta = softBlockPct;
//        }

//        _penetrationBlockPct = Mathf.Clamp( _penetrationBlockPct, 0.0f, 1.0f );
//        if(_penetrationBlockPct < 0.0001f)
//        {
//            _penetrationBlockPct = 0.0f;
//        }

//        if(_penetrationBlockPct < 1.0f)
//        {
//            desiredPosition = worstPosition + (desiredPosition - worstPosition) * _penetrationBlockPct;
//        }
//        _penetrationBlockPctPrevFrame = _penetrationBlockPct;

//        /*
//        //worstPosition = worstPosition + viewTarget.pointOfView.rotation * new Vector3( 0, 0, 0.25f );
//        Vector3 baseRay                 = desiredPosition - worstPosition;
//        Quaternion baseRayRotation      = Quaternion.LookRotation( baseRay );
//        Vector3 baseRayLocalUp          = baseRayRotation * Vector3.up;
//        Vector3 baseRayLocalFwd         = baseRayRotation * Vector3.forward;
//        Vector3 baseRayLocalRight       = baseRayRotation * Vector3.right;
//        float checkDist                 = baseRay.magnitude;
//        float hardBlockPct              = distBlockedPct;
//        float softBlockPct              = distBlockedPct;
//        float distBlockedPctThisFrame   = 1.0f;
//        int numRaysToShoot              = singleRayOnly ? Mathf.Min(1, penetrationAvoidanceFeelers.Length) : penetrationAvoidanceFeelers.Length;
//        for(int rayIdx = 0; rayIdx < numRaysToShoot; rayIdx++)
//        {
//            FBPenetrationAvoidanceFeeler feeler = penetrationAvoidanceFeelers[rayIdx];
//            if(feeler.framesUntilNextTrace <= 0)
//            {
//                // Calculate ray target.
//                Vector3 rayTarget = worstPosition + feeler.adjustmentRotation * baseRay * cameraExtentScale;
//                //Vector3 rayTargetToWorst = rayTarget - worstPosition;
//                RaycastHit hitInfo;
//                Physics.Linecast( worstPosition, rayTarget, out hitInfo, 1 << LayerMask.NameToLayer("Default") );
//                //
                
//                feeler.framesUntilNextTrace = feeler.traceInterval;
//                if(hitInfo.collider != null && !hitInfo.collider.isTrigger && hitInfo.collider.tag != "Player" )
//                {
//                    FBGameObject viewedActor = hitInfo.collider.transform.GetComponent<FBGameObject>( );
//                    if(viewedActor == null || viewedActor != viewTarget.target)
//                    {
//                        float newBlockPct = hitInfo.distance / baseRay.magnitude;
//                        distBlockedPctThisFrame = Mathf.Min( newBlockPct, distBlockedPctThisFrame );
//                       //This feeler got a hit, so do another trace next frame.
//                       feeler.framesUntilNextTrace = 0;
//                        Debug.DrawLine( worstPosition, hitInfo.point, Color.red );
//                    }
//                    else
//                    {
//                        Debug.DrawLine( worstPosition, rayTarget, Color.green );
//                    }
//                }
//                else
//                {
//                    Debug.DrawLine( worstPosition, rayTarget, Color.green );
//                }
//                hardBlockPct = distBlockedPctThisFrame;
//                //if(rayIdx == 0)
//                //{
//                //    //Don't interpolate toward this one, snap to it.
//                //    hardBlockPct = distBlockedPctThisFrame;
//                //}
//                //else
//                //{
//                //    softBlockPct = distBlockedPctThisFrame;
//                //}
//            }
//            else
//            {
//                --feeler.framesUntilNextTrace;
//            }
//        }
//        if(distBlockedPct < distBlockedPctThisFrame)
//        {
//            // Interpolate smoothly out.
//            if(penetrationBlendOutTime > deltaTime)
//            {
//                distBlockedPct = distBlockedPct + deltaTime / penetrationBlendOutTime * (distBlockedPctThisFrame - distBlockedPct);
//            }
//            else
//            {
//                distBlockedPct = distBlockedPctThisFrame;
//            }
//        }
//        else
//        {
//            if(distBlockedPct > hardBlockPct)
//            {
//                distBlockedPct = hardBlockPct;
//            }
//            else if(distBlockedPct > softBlockPct)
//            {
//                // Interpolate smoothly in.
//                if(penetrationBlendInTime > deltaTime)
//                {
//                    distBlockedPct = distBlockedPct - deltaTime / penetrationBlendInTime * (distBlockedPct - softBlockPct);
//                }
//                else
//                {
//                    distBlockedPct = softBlockPct;
//                }
//            }
//        }
//        distBlockedPct = Mathf.Clamp( distBlockedPct, 0.0f, 1.0f );
//        if(distBlockedPct < 0.0001f)
//        {
//            distBlockedPct = 0.0f;
//        }
//        if(distBlockedPct < 1.0f)
//        {
//            desiredPosition = worstPosition + (desiredPosition - worstPosition) * distBlockedPct;
//        }
//        */
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <returns></returns>
    public virtual bool ShouldDoPredictivePenetrationAvoidance( )
    {
        return false;//currentCameraMode.doPredictiveAvoidance;
    }

    /// <summary>
    /// Gets the desired camera position offset that should be applied AFTER the interpolation, if any.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <returns></returns>
    public virtual Vector3 GetPostInterpCameraOriginPositionOffset( ViewTarget viewTarget )
    {
        return Vector3.zero;
    }

    /// <summary>
    /// Gets the desired camera rotation offset that should be applied AFTER the interpolation, if any.
    /// </summary>
    /// <param name="viewTarget">The camera's view target.</param>
    /// <returns></returns>
    public virtual Quaternion GetPostInterpCameraOriginRotationOffset( ViewTarget viewTarget )
    {
        return Quaternion.identity;
    }

    /// <summary>
    /// Evaluates the game state and returns the proper camera mode.
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <returns>The new camera mode to use.</returns>
    protected virtual CameraMode FindBestCameraMode( CameraActor viewedActor )
    {
        //FBCameraMode newCameraMode = null;
        //FBPlayableCharacter player       = viewedActor as FBPlayableCharacter;

        //if( player != null )
        //{
        //    //var state = (FBCommon.AnimatorParams.StateType)player.animator.GetInteger( FBCommon.AnimatorParams.State );
        //    //switch( state )
        //    //{
        //    //    case FBCommon.AnimatorParams.StateType.Standing:
        //    //        newCameraMode = gameCamDefault;
        //    //        break;
        //    //    case FBCommon.AnimatorParams.StateType.Crouched:
        //    //        newCameraMode = gameCamCrouch;
        //    //        break;
        //    //    case FBCommon.AnimatorParams.StateType.LowCover:
        //    //    case FBCommon.AnimatorParams.StateType.HighCover:
        //    //        newCameraMode = gameCamCover;
        //    //        break;
        //    //    case FBCommon.AnimatorParams.StateType.StandingAim:
        //    //        newCameraMode = gameCamStandingAim;
        //    //        break;
        //    //    case FBCommon.AnimatorParams.StateType.CrouchedAim:
        //    //        newCameraMode = gameCamCrouchedAim;
        //    //        break;
        //    //    case FBCommon.AnimatorParams.StateType.LightMeleeWeapon:
        //    //    case FBCommon.AnimatorParams.StateType.HeavyMeleeWeapon:
        //    //        newCameraMode = gameCamMelee;
        //    //        break;
        //    //    default:
        //    //        newCameraMode = gameCamDefault;
        //    //        break;
        //    //}

        //    if(gameplayCamera.cameraVolume != null && gameplayCamera.cameraVolume.canOverride )
        //    {
        //        newCameraMode = gameplayCamera.cameraVolume.cameraMode;              
        //    }      
        //}

        idleCameraMode.cameraController = this;

        return idleCameraMode;//newCameraMode;
    }

    bool IsLookingAtObject( Transform looker, Vector3 targetPos, float FOVAngle )
    {

        Vector3 direction = targetPos - looker.position;
        float ang = Mathf.Atan2( direction.y, direction.x ) * Mathf.Rad2Deg;
        float lookerAngle = looker.eulerAngles.z;
        float checkAngle = 0f;

        if(ang >= 0f)
            checkAngle = ang - lookerAngle - 90f;
        else if(ang < 0f)
            checkAngle = ang - lookerAngle + 270f;

        if(checkAngle < -180f)
            checkAngle = checkAngle + 360f;

        if(checkAngle <= FOVAngle * .5f && checkAngle >= -FOVAngle * .5f)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Update current camera mode. Finds the best mode to use and handles transitions.
    /// </summary>
    /// <param name="viewTarget"></param>
    protected void UpdateCameraMode( CameraActor viewedActor )
    {
        // Find the most suitable camera mode.
        CameraMode newCamMode = FindBestCameraMode( viewedActor );

        if(newCamMode != currentCameraMode)
        {
            // Handle changing camera modes.
            if(currentCameraMode != null)
            {
                currentCameraMode.OnBecameInactive( viewedActor, newCamMode );
            }

            if(newCamMode != null)
            {
                newCamMode.OnBecameActive( viewedActor, currentCameraMode );
            }

            currentCameraMode = newCamMode;
        }
    }

    /// <summary>
    /// Gives camera modes a chance to change the view rotation.
    /// </summary>
    /// <param name="viewTarget"></param>
    /// <param name="viewRotation"></param>
    /// <param name="deltaRotation"></param>
    /// <param name="deltaTime"></param>
    public virtual void ProcessViewRotation( CameraActor viewedActor, ref Quaternion viewRotation, ref Quaternion deltaRotation, float deltaTime )
    {
        // See if the camera mode wants to manipulate the view rotation.
        if(currentCameraMode != null)
        {
            currentCameraMode.ProcessViewRotation( viewedActor, ref viewRotation, ref deltaRotation, deltaTime );
        }
    }

    /// <summary>
    /// Resets or cancels the camera's interpolation. If canceling the next update will skip interpolation and snap to desired view.
    /// </summary>
    public void ResetInterpolation( )
    {
        lastYawAdjustment = 0.0f;
        lastPitchAdjustment = 0.0f;
        _leftOverPitchAdjustment = 0.0f;
        resetCameraInterpolation = true;
    }

    /// <summary>
    /// Gets the camera modes desired field of view.
    /// </summary>
    /// <returns></returns>
    public float GetDesiredFieldOfView( )
    {
        return currentCameraMode.GetDesiredFieldOfView( );
    }
}

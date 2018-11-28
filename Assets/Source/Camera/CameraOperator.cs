using UnityEngine;

/// <summary>
/// Contains all of the information needed to define a camera mode.
/// </summary>
public class CameraOperator : MonoBehaviour
{
    public CameraController cameraController;
    public Transform aimTarget;
    public CameraActor actor;
    public CameraCache cameraCache, lastFrameCameraCache;

    public ViewTarget currentViewTarget; // The current view target we are looking at.
    public ViewTarget pendingViewTarget; // The view target that we are blending to.
    public FBViewTargetTransitionParams viewTargetBlendParams;
    public bool interpolateCameraSwitching;
    private PointOfView _lastBlendPointOfView;

    private bool _resetInterpolation; // Indicates if we should reset interpolation on whichever active camera controller process next.
    private Transform _cameraTransform; // Reference to the camera's transform.
    private float _blendTimeToGo; // The amount of time remaining when blending to a pending view target.
    private bool _hasBeenInitailized;

    #region Unity Engine Callbacks

    private void Awake( )
    {
        _cameraTransform = transform;
    }

    public void Start( )
    {
        Initialize( actor );
    }

    private void LateUpdate( )
    {
        float deltaTime = Time.deltaTime;

        if(deltaTime < 0.0001f || !_hasBeenInitailized)
        {
            return;
        }

        UpdateCamera( Time.deltaTime );
    }

    private void FixedUpdate( )
    {

    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inViewTarget"></param>
    public void Initialize( CameraActor inViewTarget )
    {
        _hasBeenInitailized = true;

        SetViewTarget( inViewTarget );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deltaTime"></param>
    public void UpdateCamera( float deltaTime )
    {
        PointOfView newPointOfView;

        // Don't update the outgoing view target during an interpolation when lockOutgoing is set.
        if(pendingViewTarget.actor == null || !viewTargetBlendParams.lockOutgoing)
        {
            // Update the current view target.
            UpdateViewTarget( ref currentViewTarget, deltaTime );
        }

        // Our camera is there now, so view there.
        newPointOfView = currentViewTarget.pointOfView;

        // If we have a pending view target, then perform the transition from one to another.
        if(pendingViewTarget.actor != null)
        {
            _blendTimeToGo -= deltaTime;

            // Update the pending view target.
            UpdateViewTarget( ref pendingViewTarget, deltaTime );

            // Do some blending...
            if(_blendTimeToGo > 0.0f)
            {
                float blendingPct = 0.0f;
                float durationPct = (viewTargetBlendParams.totalBlendTime - _blendTimeToGo) / viewTargetBlendParams.totalBlendTime;

                switch(viewTargetBlendParams.blendType)
                {
                    case ViewTargetBlendType.Linear:
                        blendingPct = Mathf.Lerp( 0.0f, 1.0f, durationPct );
                        break;
                    case ViewTargetBlendType.Cubic:
                        {
                            float A2 = durationPct * durationPct;
                            float A3 = A2 * durationPct;
                            float P0 = 0;
                            float P1 = 1;
                            float T0 = 0;
                            float T1 = 1;

                            blendingPct = (((2 * A3) - (3 * A2) + 1) * P0) + ((A3 - (2 * A2) + durationPct) * T0) + ((A3 - A2) * T1) + (((-2 * A3) + (3 * A2)) * P1);
                        }
                        break;
                    case ViewTargetBlendType.EaseIn:
                        blendingPct = Mathf.Lerp( 0.0f, 1.0f, (durationPct * viewTargetBlendParams.blendExponent) );
                        break;
                    case ViewTargetBlendType.EaseOut:
                        blendingPct = Mathf.Lerp( 0.0f, 1.0f, (durationPct * (1.0f / viewTargetBlendParams.blendExponent)) );
                        break;
                    case ViewTargetBlendType.EaseInOut:
                        {
                            float modifiedPct = durationPct < 0.5f ? 0.5f * Mathf.Pow( 2.0f * durationPct, viewTargetBlendParams.blendExponent ) : 1.0f - 0.5f * Mathf.Pow( 2.0f * (1.0f - durationPct), viewTargetBlendParams.blendExponent );
                            blendingPct = Mathf.Lerp( 0.0f, 1.0f, modifiedPct );
                        }
                        break;
                }

                // Blend the current view target with the pending view target.
                newPointOfView = BlendViewTargets( currentViewTarget, pendingViewTarget, blendingPct );
            }
            else
            {
                // We're done blending, set the new view target.
                currentViewTarget = pendingViewTarget;

                // Clear the pending view target.
                _blendTimeToGo = 0.0f;
                pendingViewTarget.actor = null;

                // Our camera is now viewing the new view target.
                newPointOfView = pendingViewTarget.pointOfView;
            }
        }

        // Cache the results.
        FillCameraCache( newPointOfView );

        //_cameraTransform.position = Vector3.Lerp( _cameraTransform.position, newPointOfView.position, deltaTime * 1.0f );
        //_cameraTransform.rotation = Quaternion.Lerp( _cameraTransform.rotation, newPointOfView.rotation, deltaTime * 1.0f );
        _cameraTransform.position = newPointOfView.position;
        _cameraTransform.rotation = newPointOfView.rotation;

        Camera.main.transform.position = _cameraTransform.position;
        Camera.main.transform.rotation = _cameraTransform.rotation;
        Camera.main.fieldOfView = newPointOfView.fieldOfView;

        //UpdateCameraShake( );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="outViewTarget"></param>
    /// <param name="deltaTime"></param>
    public void UpdateViewTarget( ref ViewTarget viewTarget, float deltaTime )
    {
        // Don't update outgoing view target during an interpolation.
        if(pendingViewTarget.actor != null && viewTarget.actor == currentViewTarget.actor && viewTargetBlendParams.lockOutgoing)
        {
            return;
        }

        CameraActor viewedActor = viewTarget.actor;

        // Decide which camera controller to use.
        CameraController newController = FindBestCameraController( viewTarget.actor );

        // Handle a switch if necessary.
        if(cameraController != newController)
        {
            //if(cameraController != null)
            //{
            //    cameraController.OnBecameInactive( newController );
            //}

            //if(newController != null)
            //{
            //    newController.OnBecameActive( cameraController );
            //}

            cameraController = newController;
        }

        // Update the current controller.
        if(cameraController != null)
        {
            // We wait to apply this here in case the above code changed the current controller.
            if(_resetInterpolation && !interpolateCameraSwitching)
            {
                cameraController.ResetInterpolation( );
            }

            viewTarget.pointOfView.position = viewedActor.transform.position;
            viewTarget.pointOfView.rotation = viewedActor.transform.rotation;
            viewTarget.pointOfView.fieldOfView = 60.0f;

            cameraController.UpdateCamera( viewedActor, ref viewTarget, deltaTime );
        }

        _resetInterpolation = false;
    }

    /// <summary>
    /// Set a new view target with optional blending.
    /// </summary>
    /// <param name="newViewTarget"></param>
    /// <param name="transitionParams"></param>
    public void SetViewTarget( CameraActor newTarget, FBViewTargetTransitionParams transitionParams = null )
    {
        // If we are already transitioning to this target, don't interrupt.
        if(pendingViewTarget.actor != null && newTarget == pendingViewTarget.actor)
        {
            return;
        }

        // If our view target is different than the new one, then assign it.
        if(newTarget != currentViewTarget.actor)
        {
            // If a blend time is specified, then set the pending view target accordingly.
            if(transitionParams != null && transitionParams.totalBlendTime > 0.0f)
            {
                if(pendingViewTarget.actor == null)
                {
                    pendingViewTarget.actor = currentViewTarget.actor;
                }

                // Use the last frame's point of view.
                currentViewTarget.pointOfView = lastFrameCameraCache.pointOfView;
                viewTargetBlendParams = transitionParams;
                _blendTimeToGo = transitionParams.totalBlendTime;

                // Assign the pending view target.
                AssignViewTarget( newTarget, ref pendingViewTarget );
            }
            else
            {
                // Otherwise, assign the new view target instantly.
                AssignViewTarget( newTarget, ref currentViewTarget );

                // Remove the old pending view target, so we don't try to switch to it.
                pendingViewTarget.actor = null;
            }
        }
        else
        {
            // We're setting the new target to the view target we were transitioning away from, just abort the transition.
            pendingViewTarget.actor = null;
        }
    }

    /// <summary>
    /// Assigns the new view target.
    /// </summary>
    /// <param name="newTarget"></param>
    private void AssignViewTarget( CameraActor newTarget, ref ViewTarget viewTarget )
    {
        if(newTarget == null || (newTarget == viewTarget.actor))
        {
            return;
        }

        viewTarget.actor = newTarget;
        viewTarget.pointOfView.position = newTarget.transform.position;
        viewTarget.pointOfView.rotation = newTarget.transform.rotation;
        viewTarget.pointOfView.fieldOfView = 60.0f;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="viewedActor"></param>
    /// <returns></returns>
    private CameraController FindBestCameraController( CameraActor viewedActor )
    {
        return cameraController;
    }

    /// <summary>
    /// Blends between two view targets.
    /// </summary>
    /// <param name="viewTargetA">Source view target.</param>
    /// <param name="viewTargetB">Destination view target.</param>
    /// <param name="blendPct">Percentage of blend from A to B.</param>
    /// <returns></returns>
    private PointOfView BlendViewTargets( ViewTarget viewTargetA, ViewTarget viewTargetB, float blendPct )
    {
        PointOfView pointOfView = new PointOfView( );

        pointOfView.position = Vector3.Lerp( viewTargetA.pointOfView.position, viewTargetB.pointOfView.position, blendPct );
        pointOfView.rotation = Quaternion.Slerp( viewTargetA.pointOfView.rotation, viewTargetB.pointOfView.rotation, blendPct );
        pointOfView.fieldOfView = Mathf.Lerp( viewTargetA.pointOfView.fieldOfView, viewTargetB.pointOfView.fieldOfView, blendPct );

        return pointOfView;
    }

    /// <summary>
    /// Updates the camera's cache.
    /// </summary>
    /// <param name="NewPOV"></param>
    private void FillCameraCache( PointOfView newPointOfView )
    {
        float time = Time.time;

        if(cameraCache.timeStamp != time)
        {
            lastFrameCameraCache = cameraCache;
        }

        cameraCache.timeStamp = time;
        cameraCache.pointOfView = newPointOfView;
    }

    /// <summary>
    /// Master function to retrieve Camera's actual view point.
    /// </summary>
    /// <param name="outCameraPosition">Camera position.</param>
    /// <param name="outCameraRotation">Camera rotation.</param>
    public void GetCameraViewPoint( out Vector3 outCameraPosition, out Quaternion outCameraRotation )
    {
        outCameraPosition = cameraCache.pointOfView.position;
        outCameraRotation = cameraCache.pointOfView.rotation;
    }

    #region Shake

    //public Vector3 originalShakeLoc;
    //public Quaternion originalShakeRot;
    //public float shakeDecay;
    //public float shakeIntensity;

    //private void UpdateCameraShake( )
    //{
    //    if(shakeIntensity > 0.0f)
    //    {
    //        Camera.main.transform.position = Camera.main.transform.position + UnityEngine.Random.insideUnitSphere * shakeIntensity;
    //        Camera.main.transform.rotation = new Quaternion(
    //                        Camera.main.transform.rotation.x + UnityEngine.Random.Range( -shakeIntensity, shakeIntensity ) * 0.001f,
    //                        Camera.main.transform.rotation.y + UnityEngine.Random.Range( -shakeIntensity, shakeIntensity ) * 0.001f,
    //                        Camera.main.transform.rotation.z + UnityEngine.Random.Range( -shakeIntensity, shakeIntensity ) * 0.001f,
    //                        Camera.main.transform.rotation.w + UnityEngine.Random.Range( -shakeIntensity, shakeIntensity ) * 0.001f );
    //        shakeIntensity -= shakeDecay;
    //    }
    //}

    //public void Shake( float inShakeIntensity, float inShakeDecay )
    //{
    //    originalShakeLoc = Camera.main.transform.position;
    //    originalShakeRot = Camera.main.transform.rotation;

    //    shakeIntensity = inShakeIntensity;
    //    shakeDecay = inShakeDecay;
    //}

    //public void StopShake( )
    //{
    //    shakeIntensity = 0;
    //}

    #endregion
}

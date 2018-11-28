using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using Rewired.ComponentControls;

public class PlayerInput : MonoBehaviour
{
    public Transform cameraRotationTarget;

    public TouchJoystick joystick;
    public CameraController cameraController;
    private TouchInteractable.InteractionState _prevInteractionState;

    public void OnEnable( )
    {
        //if(!ReInput.isReady)
        //    return;

        //Player player = ReInput.players.GetPlayer( 0 );
        //if(player == null)
        //    return;

        //player.AddInputEventDelegate( OnHorizontalMovement, UpdateLoopType.Update, InputActionEventType.AxisActive, "Horizontal" );
        //player.AddInputEventDelegate( OnHorizontalMovement, UpdateLoopType.Update, InputActionEventType.AxisInactive, "Horizontal" );
        //player.AddInputEventDelegate( OnVerticalMovement, UpdateLoopType.Update, InputActionEventType.AxisActive, "Vertical" );
        //player.AddInputEventDelegate( OnVerticalMovement, UpdateLoopType.Update, InputActionEventType.AxisInactive, "Vertical" );

        
    }

    //private void OnHorizontalMovement( InputActionEventData data )
    //{
    //    _inputDirection.x = data.GetAxis( );

    //    //Debug.Log( _inputDirection );
    //}

    //private void OnVerticalMovement( InputActionEventData data )
    //{
    //    _inputDirection.z = data.GetAxis( );

    //    //Debug.Log( _inputDirection );
    //}

    private void Update( )
    {
        if(_prevInteractionState != joystick.interactionState)
        {
            if(joystick.interactionState == TouchInteractable.InteractionState.Pressed)
                Debug.Log( "Pressed" );
            else if(_prevInteractionState == TouchInteractable.InteractionState.Pressed)
                Debug.Log( "Released" );
        }

        _prevInteractionState = joystick.interactionState;
    }

    public void FixedUpdate( )
    {
        Quaternion deltaRotation = Quaternion.identity;
        Quaternion preDeltaRotation = Quaternion.identity;
        Quaternion viewRotation = cameraRotationTarget.rotation;

        // Calculate the input delta to be applied on the viewRotation.

        Vector2 inputVector = joystick.GetValue( );

        deltaRotation = Quaternion.AngleAxis( inputVector.x, Vector3.up ) * Quaternion.AngleAxis( inputVector.y, Vector3.right );

        // Cache the input delta before we process it.
        preDeltaRotation = deltaRotation;

        // Process View Rotation
        //cameraController.ProcessViewRotation( gameObject, ref viewRotation, ref deltaRotation, Time.fixedDeltaTime );

        // Weapons needs a chance to process the view rotation too!

        viewRotation *= deltaRotation;

        // Apply look rotation to the camera target.
        cameraRotationTarget.rotation = viewRotation;

        // Apply the rotation
        transform.Rotate( Vector3.up, preDeltaRotation.eulerAngles.y );
    }
}

/*
 *     public void ProcessMove( float deltaTime )
    {
        Quaternion deltaRotation = Quaternion.identity;
        Quaternion preDeltaRotation = Quaternion.identity;
        Quaternion viewRotation = cameraRotationTarget.rotation;

        // Calculate the input delta to be applied on the viewRotation.
        deltaRotation = Quaternion.AngleAxis( playerInput.turn * 1.25f, Vector3.up ) * Quaternion.AngleAxis( playerInput.look, Vector3.right );

        deltaRotation *= _cameraRotationTargetExtra;
        _cameraRotationTargetExtra = Quaternion.identity;

        ///////////////////////////////////////////////
        // Can Apply Aim-Assistance here with adhesion
        ///////////////////////////////////////////////

        // Cache the input delta before we process it.
        preDeltaRotation = deltaRotation;

        //
        ProcessViewRotation( ref viewRotation, ref deltaRotation );

        // Apply look rotation to the camera target.
        cameraRotationTarget.rotation = viewRotation;

        //int animatorState = animator.GetInteger( FBCommon.AnimatorParams.State );
        //if( animatorState != 2 && animatorState != 3 )
        //    cachedTransform.Rotate( Vector3.up, preDeltaRotation.eulerAngles.y );
        cachedTransform.Rotate(Vector3.up, preDeltaRotation.eulerAngles.y);

        //
        ProcessRootMotion( deltaTime );

        //
        animator.SetFloat( FBCommon.AnimatorParams.Strafe, playerInput.strafe );
        animator.SetFloat( FBCommon.AnimatorParams.Forward, playerInput.forward );
    }
 */

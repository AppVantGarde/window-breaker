using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Rewired.ComponentControls;

public class Player : CameraActor
{
    public SharedInt ammoCount;
    public AudioCue perfectSound;

    [Header("Input")]
    public TouchJoystick joystick;
    private TouchInteractable.InteractionState _prevInteractionState;

    [Space( 10 )]
    public Animator animator;

    [NonSerialized] public SpecialMoveType activeSpecialMove;
    [NonSerialized] public SpecialMoveType previousSpecialMove;

    public PendingSpecialMove pendingSpecialMove;

    [NonSerialized] public bool isEndingSpecialMove;
    [NonSerialized] public bool isPendingSpecialMove;
    [NonSerialized] public float previousSpecialMoveStartTime;

    public SpecialMove[ ] specialMoves;

    public Transform releasePoint;
    public Transform handSocket;
    public Transform hitLocation;
    private TrajectorySimulator trajectorySimulator;

    public GameObject crosshair;
    public Transform cameraFocusTarget;
    public LineRenderer trajectoryRenderer;

    public int _perfectStreak;
    private float _lastWindowSmashedTime;

    public override void Awake( )
    {
        base.Awake( );

        trajectorySimulator = new TrajectorySimulator( );

        specialMoves = new SpecialMove[(int)SpecialMoveType.Count];
    }

    public void Update( )
    {
        if(SceneManager.GetActiveScene( ).name != "main")
            return;

        if(ammoCount.value <= 0 && SceneManager.GetActiveScene( ).name == "main")
            return;

        if(GameStateManager.Instance != null)
        {
            if(GameStateManager.Instance.gameState != GameStateManager.GameState.Gameplay || !UIScreenManager.Instance.IsScreenActive( "gameplay" ))
                return;
        }


        //if(_prevInteractionState != joystick.interactionState)
        //{
        //    if(joystick.interactionState == TouchInteractable.InteractionState.Pressed)
        //    {
        //        animator.SetBool( "isAiming", true );
        //    }
        //    else if(_prevInteractionState == TouchInteractable.InteractionState.Pressed)
        //    {
        //        animator.SetBool( "isAiming", false );
        //    }
        //}

        //_prevInteractionState = joystick.interactionState;

        Vector3 touchLocation = Vector3.zero;

        if(Input.GetMouseButtonUp( 0 ))
            touchLocation = Input.mousePosition;

        if((Input.touchCount > 0) && (Input.GetTouch( 0 ).phase == TouchPhase.Began))
        {
            touchLocation = Input.GetTouch( 0 ).position;
        }

        if(touchLocation != Vector3.zero && canThrow)
        {
            //Ray screenRay = Camera.main.ScreenPointToRay( touchLocation );

            //RaycastHit hitInfo;
            //if(Physics.Raycast( screenRay.origin, screenRay.direction, out hitInfo ))
            //{
            //    animator.CrossFade( "aim", 0.05f );

            //    _ballHitLocation = FocusTarget.instance.subFocusPoint.position;//hitInfo.point;
            //    //_ballHitLocation.y += 0.1f;
            //    Debug.DrawLine( screenRay.origin, hitInfo.point, Color.blue, 1.0f );

            //    Vector3 ToHitLocation = (_ballHitLocation - screenRay.origin);//handSocket.transform.position);

            //    _projectileDirection = ToHitLocation;
            //    Vector3 lookDirection = _projectileDirection;
            //    lookDirection.x = 0;
            //    lookDirection.z = 0;

            //    transform.DORotate( lookDirection, 0.2f );
            //}

            //animator.ResetTrigger( "throw" );

            canThrow = false;
            animator.SetTrigger( "throw" );
            DOVirtual.DelayedCall( 0.15f, ( ) => { canThrow = true; } );

            //_ballHitLocation = FocusTarget.instance.subFocusPoint.position;//hitInfo.point;
                                                                           //_ballHitLocation.y += 0.1f;
            //Debug.DrawLine( screenRay.origin, hitInfo.point, Color.blue, 1.0f );

            //Vector3 ToHitLocation = (_ballHitLocation - handSocket.transform.position); //screenRay.origin);//

            //_projectileDirection = ToHitLocation;
            //Vector3 lookDirection = _projectileDirection;
            //lookDirection.x = 0;
            //lookDirection.z = 0;

            //transform.DORotate( lookDirection, 0.2f );
        }
    }

    private bool canThrow = true;

    private Touch _previousTouch;

    private Vector3 _ballHitLocation;
    private Vector3 _projectileDirection;

    public void FixedUpdate( )
    {
        if(SceneManager.GetActiveScene( ).name != "main")
            return;

        //Vector2 input = joystick.GetValue( );

        trajectoryRenderer.SetPosition( 0, handSocket.position );
        trajectoryRenderer.SetPosition( 1, FocusTarget.instance.mainFocusPoint.position );

        //input.y *= -1.0f;

        Quaternion deltaRotation = Quaternion.identity;
        Quaternion preDeltaRotation = Quaternion.identity;
        Quaternion viewRotation = cameraTarget.rotation;

        // Calculate the input delta to be applied on the viewRotation.
        //deltaRotation = Quaternion.AngleAxis( input.x, Vector3.up ) * Quaternion.AngleAxis( input.y, Vector3.right );
        deltaRotation = Quaternion.LookRotation( FocusTarget.instance.mainFocusPoint.position - Camera.main.transform.position );//Quaternion.RotateTowards( viewRotation, Quaternion.LookRotation( cameraFocusTarget.position - Camera.main.transform.position ), 10f * Time.fixedDeltaTime );
        //Debug.DrawLine( Camera.main.transform.position, Camera.main.transform.position + Camera.main.transform.forward * 50, Color.blue );

        // Cache the input delta before we process it.
        preDeltaRotation = deltaRotation;

        cameraOperator.cameraController.ProcessViewRotation( this, ref viewRotation, ref deltaRotation, Time.fixedDeltaTime );

        // Apply look rotation to the camera target.
        cameraTarget.rotation = viewRotation;

        //transform.Rotate( Vector3.up, preDeltaRotation.eulerAngles.y );

        // Apply turn rotation to the camera actor.
        transform.rotation = Quaternion.Euler( 0, cameraTarget.eulerAngles.y, 0 );

        UpdateCamera( Time.fixedDeltaTime );

        if(joystick.interactionState == TouchInteractable.InteractionState.Pressed)
        {
            trajectorySimulator.RunSimulation( releasePoint.position, cameraTarget.forward * throwForce, Physics.gravity * gravMultiplier );

            Vector3 hitPosition = trajectorySimulator.lastHit.point;
            hitPosition.z *= 0.99f;

            hitLocation.position = hitPosition;
        }

        if((Time.time - _feverLastIncrementTime) > 4f)
        {
            _feverDecayInterval += Time.fixedDeltaTime;
            if(_feverDecayInterval > 2f)
            {
                _feverDecayInterval = 0;
                _feverValue = Mathf.Max( _feverValue - _feverDecayValue, 0 );
            }
        }

        if(_feverActive && _feverValue <= 0.8f)
            _feverActive = false;
    }

    public float throwForce = 500;
    public float gravMultiplier = 1;

    public void AnimEvent_Release( )
    {
        if(!canThrow)
            return;

        AudioManager.Instance.Play( "cue_throw" );

        DOVirtual.DelayedCall( 1f, ( ) => { ammoCount.value -= 1; } );

        GameObject rockObject = Instantiate( Resources.Load<GameObject>( "Prefabs/Game/Rock" ) );
        rockObject.transform.position = handSocket.transform.position;
        //rockObject.transform.forward = cameraTarget.forward;

        RockProjectile projectile = rockObject.GetComponent<RockProjectile>( );



        _ballHitLocation = FocusTarget.instance.mainFocusPoint.position;
        projectile.Fire( (_ballHitLocation - handSocket.transform.position).normalized, HasFever() );

        if(HasFever( ))
            _feverValue = 0;
        //rigidBody.AddForce( (_ballHitLocation - handSocket.transform.position).normalized * throwForce, ForceMode.Force );
    }

    private bool _feverActive;
    public float _feverValue;
    private float _feverIncrementValue = 0.25f;
    private float _feverDecayValue = 0.125f;
    private float _feverDecayInterval = 0;
    private float _feverLastIncrementTime;

    public void PerfectThrow( bool perfect )
    {
        if(perfect)
        {
            _feverValue = Mathf.Min( _feverValue + _feverIncrementValue, 1.0f );
            _feverLastIncrementTime = Time.time;
            _feverDecayInterval = 0;

            if(_feverValue >= 1.0f)
                _feverActive = true;

            //_perfectStreak = Mathf.Min( _perfectStreak + 1, 4 );
            perfectSound.Play( 0.65f, Mathf.Lerp( 0.9f, 1.3f, (_perfectStreak / 5.0f) ) );
        }
        else
        {
            //_perfectStreak = Mathf.Max( _perfectStreak - 1, 0 );
        }
    }

    public bool HasFever( )
    {
        return _feverActive;
    }

    public void DoSpecialMove( SpecialMoveType newMove, int specialMoveFlags = -1, bool forced = false )
    {
        if(newMove != SpecialMoveType.None && specialMoves[(int)newMove] == null)
            return;

        PendingSpecialMove newPendingMove = new PendingSpecialMove( )
        {
            specialMove = newMove,
            specialMoveFlags = specialMoveFlags
        };

        if(isPendingSpecialMove)
        {
            pendingSpecialMove.specialMove = SpecialMoveType.None;
            pendingSpecialMove.specialMoveFlags = specialMoveFlags;
        }

        if(!forced && newMove == activeSpecialMove)
            return;

        if(isEndingSpecialMove)
        {
            pendingSpecialMove = newPendingMove;
            return;
        }

        if(isPendingSpecialMove && !forced && activeSpecialMove != SpecialMoveType.None && newMove != SpecialMoveType.None)
        {
            if(specialMoves[(int)activeSpecialMove].CanBeOverridenBy( newMove ) || specialMoves[(int)newMove].CanOverride( activeSpecialMove ))
            {
                forced = true;
            }
        }

        if(newMove != SpecialMoveType.None && !forced && !specialMoves[(int)newMove].CanDoSpecialMove( ))
        {
            return;
        }

        previousSpecialMove = activeSpecialMove;

        if(activeSpecialMove != SpecialMoveType.None)
        {
            if(!isPendingSpecialMove)
            {
                isEndingSpecialMove = true;
                activeSpecialMove = SpecialMoveType.None;
                SpecialMoveEnded( previousSpecialMove, (newMove == SpecialMoveType.None && pendingSpecialMove.specialMove != SpecialMoveType.None) ? pendingSpecialMove.specialMove : newMove );
                isEndingSpecialMove = false;
            }
        }

        if(newMove == SpecialMoveType.None && pendingSpecialMove.specialMove != SpecialMoveType.None)
        {
            activeSpecialMove = previousSpecialMove;
            SpecialMoveType nextMove = pendingSpecialMove.specialMove;

            isPendingSpecialMove = true;
            DoSpecialMove( pendingSpecialMove.specialMove, pendingSpecialMove.specialMoveFlags, false );
            isPendingSpecialMove = false;

            if(activeSpecialMove == nextMove)
                return;
        }

        activeSpecialMove = newPendingMove.specialMove;

        if(activeSpecialMove != SpecialMoveType.None)
        {
            SpecialMoveStarted( activeSpecialMove, previousSpecialMove, specialMoveFlags );

            if(forced)
            {
                pendingSpecialMove.specialMove = SpecialMoveType.None;
                pendingSpecialMove.specialMoveFlags = specialMoveFlags;
            }
        }
    }

    public void StopSpecialMove( SpecialMoveType specialMoveToStop )
    {
        if(activeSpecialMove == SpecialMoveType.None)
            return;

        if(specialMoveToStop != SpecialMoveType.None && pendingSpecialMove.specialMove == specialMoveToStop)
        {
            pendingSpecialMove.specialMove = SpecialMoveType.None;
        }

        if(specialMoveToStop == SpecialMoveType.None || activeSpecialMove == specialMoveToStop)
            DoSpecialMove( SpecialMoveType.None );
    }

    private void SpecialMoveStarted( SpecialMoveType newMove, SpecialMoveType previousMove, int specialMoveFlags )
    {
        if(newMove == SpecialMoveType.None || specialMoves[(int)newMove] == null )
            return;

        specialMoves[(int)newMove].SpecialMoveStarted( previousMove, specialMoveFlags );
    }

    private void SpecialMoveEnded( SpecialMoveType previousMove, SpecialMoveType nextMove )
    {
        if(previousMove == SpecialMoveType.None || specialMoves[(int)previousMove] == null)
            return;

        specialMoves[(int)previousMove].SpecialMoveEnded( previousMove, nextMove );
    }
}

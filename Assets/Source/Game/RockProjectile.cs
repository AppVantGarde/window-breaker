using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RockProjectile : MonoBehaviour
{
    public Rigidbody rigidBody;
    private bool _playedThud;

    private bool _inFlight;
    private float _movementSpeed = 45f;
    private Vector3 _fireDirection;

    public bool isFeverBall;
    public ParticleSystem fireParticles;

    public void OnCollisionEnter( Collision collision )
    {
        _inFlight = false;
        rigidBody.useGravity = true;

        //rigidBody.velocity = collision.contacts[0].normal * 5;

        fireParticles.Stop( );

        if(_playedThud)
        {
            return;
        }


        _playedThud = true;

        if(collision.collider.tag == "Shutter")
        {
            GameStateManager.Instance.TriggerGameOver( );
            AudioManager.Instance.Play( "cue_thud" );
            return;
        }

        if(collision.collider.tag == "Obstacle")
        {
            

            if(GameStateManager.Instance.player.HasFever( ))
            {
                GameStateManager.Instance.feverBallExplosion.gameObject.transform.position = transform.position;
                GameStateManager.Instance.feverBallExplosion.Play( );
                AudioManager.Instance.Play( 5 );

                Collider[ ] sphereCollisions = Physics.OverlapSphere( transform.position, 3 );
                for(int i = 0; i < sphereCollisions.Length; i++)
                {
                    if(sphereCollisions[i].tag == "Window")
                    {
                        BreakableObject window = sphereCollisions[i].GetComponent<BreakableObject>( );
                        window.triggerBreak( );
                    }
                }
            }
            else
            {
                AudioManager.Instance.Play( "cue_thud" );
            }
        }
    }

    public void LateUpdate( )
    {
        //if(_inFlight)
        //    rigidBody.velocity += _fireDirection * (_movementSpeed * Time.deltaTime);
    }

    public void Fire( Vector3 direction, bool fever )
    {
        isFeverBall = fever;
        if(isFeverBall)
            fireParticles.Play( );

        rigidBody.velocity = direction * _movementSpeed;
        _inFlight = true;
        rigidBody.useGravity = false;
        _fireDirection = direction;
    }
}

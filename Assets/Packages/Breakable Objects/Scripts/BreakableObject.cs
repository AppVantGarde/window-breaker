/* 	Breakable Object
	(C) Unluck Software
	http://www.chemicalbliss.com
*/
#pragma warning disable 0618

using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;


public class BreakableObject : MonoBehaviour
{
    public Window parentWindow;
    public GameObject windowMesh;
    public Collider windowCollider;
    
	public Transform fragments; 					//Place the fractured object
	public float waitForRemoveCollider = 1.0f; 		//Delay before removing collider (negative/zero = never)
	public float waitForRemoveRigid = 10.0f; 		//Delay before removing rigidbody (negative/zero = never)
	public float waitForDestroy = 2.0f; 			//Delay before removing objects (negative/zero = never)
	public float explosiveForce = 350.0f; 			//How much random force applied to each fragment
	public float durability = 5.0f; 				//How much physical force the object can handle before it breaks
	public ParticleSystem breakParticles;			//Assign particle system to apear when object breaks
	public bool mouseClickDestroy;					//Mouse Click breaks the object
	Transform fragmentd;							//Stores the fragmented object after break
	bool broken;                                    //Determines if the object has been broken or not 
	Transform frags;

    public GameObject perfectZone;
    public ParticleSystem sparkleParticleFX;

    public Vector3 fragmentSize;



    public void Start( )
    {
        if(sparkleParticleFX == null)
        {
            sparkleParticleFX = Instantiate( Resources.Load<GameObject>( "Prefabs/Game/window_sparkle" ), transform.position, transform.rotation ).GetComponent<ParticleSystem>( );
            sparkleParticleFX.transform.SetParent( transform, false );
            sparkleParticleFX.transform.localPosition = Vector3.zero;
        }

        if(perfectZone == null)
        {
            perfectZone = Instantiate( Resources.Load<GameObject>( "Prefabs/Game/perfect_zone" ) );
            perfectZone.transform.parent = transform;
            perfectZone.transform.localPosition = new Vector3( 0, 0, -0.1f );
            perfectZone.transform.localRotation = Quaternion.identity;
        }
    }

    public void OnDrawGizmos( )
    {
        ///UnityEditor.Handles.DrawWireDisc( transform.position, transform.forward, 0.3f * 0.3f );
    }

    public void OnCollisionEnter(Collision collision)
    {
        if(GameStateManager.Instance.gameState != GameStateManager.GameState.Gameplay)
            return;

        if(collision.collider.tag == "Rock")
        {

            bool wasPerefectHit = false;
            if((transform.position - collision.contacts[0].point).sqrMagnitude <= (0.45f * 0.45))
            {
                wasPerefectHit = true;

                iOSHapticFeedback.Instance.Trigger( iOSHapticFeedback.iOSFeedbackType.Success );

                GameStateManager.Instance.shotClock.value += 2f;

                GameStateManager.Instance.BonusTimeMessage( 2 );
                GameStateManager.Instance.ShowPerfectMessage( transform.position );
            }
            else
            {
                iOSHapticFeedback.Instance.Trigger( iOSHapticFeedback.iOSFeedbackType.ImpactHeavy );

                GameStateManager.Instance.ShowMessage( false );
            }

            RockProjectile rock = collision.collider.GetComponent<RockProjectile>( );
            if(rock != null && rock.isFeverBall)
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
                        if(window != this)
                        {
                            window.triggerBreak( );                
                        }
                    }
                }
            }

            GameStateManager.Instance.player.PerfectThrow( wasPerefectHit );
            triggerBreak( );

            collision.collider.tag = "RockUsed";      
        }
	}
	
	public void OnMouseDown()
    {
		if(mouseClickDestroy){
			triggerBreak();
		}
	}
	
	public void triggerBreak()
    {
        parentWindow.hasBeenBroken = true;
        AudioManager.Instance.Play( "cue_glass_break" );
        GameStateManager.Instance.PlayGlassBreakParticleFX( transform.position );
        GameStateManager.Instance.windowBrokenCount.value += 1;

        sparkleParticleFX.Stop( );
        windowMesh.SetActive( false );
        perfectZone.SetActive( false );
        windowCollider.enabled = false;

	    //Destroy(transform.FindChild("object").gameObject);
	    //Destroy(transform.GetComponent<Collider>());
	    //Destroy(transform.GetComponent<Rigidbody>());
	    StartCoroutine(breakObject());
	}

	// breaks object
	public IEnumerator breakObject()
    {
	    if (!broken) {
	    	if(this.GetComponent<AudioSource>() != null){
	    		GetComponent<AudioSource>().Play();
	    	}
	    	broken = true;
	    	if(breakParticles!=null){
				// adds particle system to stage
				ParticleSystem ps = (ParticleSystem)Instantiate(breakParticles,transform.position, transform.rotation);
				// destroys particle system after duration of particle system
				Destroy(ps.gameObject, ps.duration); 
	    	}
			// adds fragments to stage (!memo:consider adding as disabled on start for improved performance > mem)
			fragmentd = (Transform)Instantiate(fragments, transform.position, transform.rotation);
            // set size of fragments
            fragmentd.localScale = fragmentSize == Vector3.zero ? transform.localScale : fragmentSize;
			frags = fragmentd.FindChild("fragments");

            Vector3 explosionDirection = (Camera.main.transform.position - transform.position);
            Vector3 explosionForce = explosionDirection * Random.Range( 25, 50 );

			foreach (Transform child in frags) {
				Rigidbody cr = child.GetComponent<Rigidbody>();
                cr.AddForce( explosionForce );//Random.Range(-explosiveForce, explosiveForce), Random.Range(-explosiveForce, explosiveForce), Random.Range(-explosiveForce, explosiveForce));

                cr.AddTorque( explosionForce );//Random.Range(-explosiveForce, explosiveForce), Random.Range(-explosiveForce, explosiveForce), Random.Range(-explosiveForce, explosiveForce));

            }
	        StartCoroutine(removeColliders());
	        StartCoroutine(removeRigids());
			// destroys fragments after "waitForDestroy" delay
			if (waitForDestroy > 0) { 
	   //         foreach(Transform child in transform) {
	   //					child.gameObject.SetActive(false);
				//}				
	            yield return new WaitForSeconds(waitForDestroy);
	            GameObject.Destroy(fragmentd.gameObject); 
	            GameObject.Destroy(transform.gameObject);
				// destroys gameobject
			} else if (waitForDestroy <=0){
	        	foreach(Transform child in transform) {
	   					child.gameObject.SetActive(false);
				}
	        	yield return new WaitForSeconds(1.0f);
	            GameObject.Destroy(transform.gameObject);
	        }	
	    }
	}

	// removes rigidbodies from fragments after "waitForRemoveRigid" delay
	public IEnumerator removeRigids() {
	    if (waitForRemoveRigid > 0 && waitForRemoveRigid != waitForDestroy) {
	        yield return new WaitForSeconds(waitForRemoveRigid);
	        foreach(Transform child in frags) {
	            child.GetComponent<Rigidbody>().isKinematic = true;
	        }
	    }
	}

	// removes colliders from fragments "waitForRemoveCollider" delay
	public IEnumerator removeColliders() {
	    if (waitForRemoveCollider > 0){
	        yield return new WaitForSeconds(waitForRemoveCollider);
	        foreach(Transform child in frags) {
	            child.GetComponent<Collider>().enabled = false;
	        }
	    }
	}
}
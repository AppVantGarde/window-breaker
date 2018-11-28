using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Window : MonoBehaviour
{
    public MeshRenderer windowRenderer;

    public Vector3 windowForward;

    public Vector3 positionOffset;
    public Vector3 customRotation;
    public Vector3 customSize;

    public bool hasBeenBroken;

    public void Awake( )
    {
            windowRenderer = GetComponent<MeshRenderer>( );

        BreakableObject breackable = Instantiate( Resources.Load<GameObject>( "Prefabs/Game/window_breakable" ) ).GetComponent<BreakableObject>( );
        breackable.transform.position = windowRenderer.bounds.center + positionOffset;

        breackable.parentWindow = this;

        Vector3 customForward = transform.forward;
        if(windowForward.x != 0)
            customForward = windowForward.x * transform.right;
        else if(windowForward.z != 0)
            customForward = windowForward.z * transform.forward;
        else if(windowForward.y != 0)
            customForward = windowForward.y * transform.up;

        breackable.transform.rotation = Quaternion.LookRotation( customForward );//Quaternion.LookRotation( windowForward.x != 0 ? transform.right : transform.forward  );

        BoxCollider boxCollider = breackable.gameObject.AddComponent<BoxCollider>( );

        Vector3 triggerSize = new Vector3( customSize.x != 0 ? customSize.x : windowRenderer.bounds.size.x, customSize.y != 0 ? customSize.y : windowRenderer.bounds.size.y, 0.25f);

        //Vector3 rightOffset = breackable.transform.position; rightOffset.x += windowRenderer.bounds.extents.magnitude;
        //Vector3 leftOffset = breackable.transform.position; leftOffset.x -= windowRenderer.bounds.extents.magnitude;

        //triggerSize.x = (rightOffset - leftOffset).magnitude * 0.9f;

        //triggerSize.z = 0.25f;

        boxCollider.size = triggerSize;

        Vector3 triggerCenter = boxCollider.center;
        triggerCenter.z += triggerSize.z * 0.5f;

        boxCollider.center = triggerCenter;

        breackable.sparkleParticleFX = Instantiate( Resources.Load<GameObject>( "Prefabs/Game/window_sparkle" ) ).GetComponent<ParticleSystem>( );
        breackable.sparkleParticleFX.transform.position = breackable.transform.position;
        breackable.sparkleParticleFX.transform.SetParent( breackable.transform, false );
        breackable.sparkleParticleFX.transform.localPosition = Vector3.zero;
        breackable.sparkleParticleFX.transform.localScale = triggerSize;

        breackable.perfectZone = Instantiate( Resources.Load<GameObject>( "Prefabs/Game/perfect_zone" ) );
        breackable.perfectZone.transform.parent = breackable.transform;
        breackable.perfectZone.transform.localPosition = new Vector3( 0, 0, -0.1f );
        breackable.perfectZone.transform.localRotation = Quaternion.identity;

        breackable.windowMesh = windowRenderer.gameObject;
        breackable.windowCollider = boxCollider;
        breackable.fragmentSize = triggerSize;
        //breackable.transform.parent = transform;

        if(customRotation != Vector3.zero)
        {
            breackable.transform.localRotation = Quaternion.Euler( customRotation );
        }

        if(GameStateManager.Instance != null)
        {
            GameStateManager.Instance.unparentedWindows.Add( breackable );
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AutoDestroy : MonoBehaviour
{
    private float _lifeTime = 5;
    public void Start( )
    {
        //DOVirtual.DelayedCall( 5, ( ) =>
        //{
        //    Destroy( gameObject );
        //} );
    }

    public void Update( )
    {
        _lifeTime -= Time.deltaTime;
        if(_lifeTime <= 0)
        {
            Destroy( gameObject );
        }
    }
}

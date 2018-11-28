using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;

public class UIButton_Simple : UIButton
{
    public AudioCue buttonSound;

    

    private bool _hasButtonAudio = false;

    protected Vector3 _punchedScale;

    #region Unity Engine Callbacks

    public virtual void Awake( )
    {
        _hasButtonAudio = buttonSound != null;
        _punchedScale = new Vector3( -0.25f, -0.25f, -0.25f );
    }

    #endregion

    public override void Pressed( )
    {


        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale( _punchedScale, 0.25f ).OnComplete( ( ) => { transform.localScale = Vector3.one; } );

        if(_hasButtonAudio)
            buttonSound.Play( );
    }
}

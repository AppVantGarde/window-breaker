using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FocusTarget : MonoBehaviour
{
    public static FocusTarget instance;

    public Transform mainFocusPoint;
    public Transform subFocusPoint;

    public Transform oscillator;
    public Transform oscillationPointA;
    public Transform oscillationPointB;

    BreakableWindowSet _windowSet;
    private Vector3 _prevSubFocusPosition;
    private float _fixedTime;

    public bool idle = true;

    public void Awake( )
    {
        instance = this;
    }

    public void FixedUpdate( )
    {
        if(!idle)
        {
            BreakableWindowSet windowSet = GameStateManager.Instance.activeGame.GetActiveWindowSet( );
            if(windowSet != _windowSet)
            {
                _windowSet = windowSet;

                subFocusPoint.DOKill( );

                Vector3[ ] path = new Vector3[_windowSet.path.Count];
                for(int i = 0; i < _windowSet.path.Count; i++)
                    path[i] = _windowSet.path[i].position;

                subFocusPoint.position = path[0];
                subFocusPoint.DOPath( path, _windowSet.pathDuration ).SetEase( Ease.Linear ).SetLoops( -1, LoopType.Yoyo );
            }
        }
        //else
        //{
        //    if(_windowSet != null)
        //    {
        //        _windowSet = null;
        //        subFocusPoint.DOKill( );
        //    }

        //    subFocusPoint.position = Vector3.zero;
        //}

        mainFocusPoint.position = SmoothLerp.Vector3( mainFocusPoint.position, _prevSubFocusPosition, subFocusPoint.position, 5f, Time.fixedDeltaTime );
        _prevSubFocusPosition = subFocusPoint.position;

#if UNITY_EDITOR
        Debug.DrawLine( oscillationPointA.position, oscillationPointB.position, Color.black );
#endif
    }

#if UNITY_EDITOR
    public void OnDrawGizmos( )
    {
        UnityEditor.Handles.color = Color.black;
        UnityEditor.Handles.DrawWireDisc( oscillationPointA.position, Vector3.forward, 0.25f );
        UnityEditor.Handles.DrawWireDisc( oscillationPointB.position, Vector3.forward, 0.25f );
        UnityEditor.Handles.DrawWireDisc( subFocusPoint.position, Vector3.forward, 0.25f );
    }
#endif
}

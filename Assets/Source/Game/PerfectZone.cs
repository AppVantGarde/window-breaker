using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PerfectZone : MonoBehaviour
{
    public SpriteRenderer ringSpriteRenderer;
    public SpriteRenderer circleSpriteRenderer;

    public void Start( )
    {
        ringSpriteRenderer.transform.DOScale( 0.25f, 0.8f ).SetEase( Ease.OutCubic ).SetLoops( -1, LoopType.Yoyo );
    }
}

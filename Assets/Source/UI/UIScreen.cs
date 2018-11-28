using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIScreen : MonoBehaviour
{
    public string screenName;
    [NonSerialized] public UIScreenState screenState;

    public RectTransform screenRect;

    public CanvasGroup canvasGroup;

    public float transitionOnTime;
    public float transitionOffTime;
    public bool hideLesserScreens;
    public bool destroyOnDeactivate;
    [NonSerialized] public bool coveredByOtherScreen;
    [NonSerialized] public bool closing;

    #region Unity Engine Callbacks



    #endregion

    /// <summary>
    /// 
    /// </summary>
    public virtual void Open() { }

    /// <summary>
    /// 
    /// </summary>
    public virtual void Close() { }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using DG.Tweening;

public class UIButton : MonoBehaviour
{
    public EventTrigger buttonEvent;
    private EventTrigger.Entry _buttonEventEntry;

    public delegate void ButtonPress( );
    public ButtonPress OnButtonPressed;

    [NonSerialized] public bool disabled;

    #region Unity Engine Callbacks

    public void OnEnable()
    {
        _buttonEventEntry = new EventTrigger.Entry();
        _buttonEventEntry.eventID = EventTriggerType.PointerClick;
        _buttonEventEntry.callback.AddListener((eventData) => { OnPointerClick_Button((PointerEventData)eventData); });
        buttonEvent.triggers.Add(_buttonEventEntry);
    }

    public void OnDisable()
    {
        buttonEvent.triggers.Remove(_buttonEventEntry);
        _buttonEventEntry = null;
    }

    private void OnPointerClick_Button(PointerEventData eventData)
    {
        if(disabled)
            return;

        Pressed();

        if(OnButtonPressed != null)
            OnButtonPressed();
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    public virtual void Pressed( ) { }
}

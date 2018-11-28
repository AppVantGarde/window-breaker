using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton_ScrollViewChild : MonoBehaviour, IPointerClickHandler
{
    public delegate void ButtonPress( );
    public ButtonPress OnButtonPressed;

    public void OnPointerClick( PointerEventData eventData )
    {
        Pressed( );

        if(OnButtonPressed != null)
            OnButtonPressed( );
    }

    /// <summary>
    /// 
    /// </summary>
    public virtual void Pressed( ) { }
}

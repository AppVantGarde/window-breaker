using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;

public class FacebookManager : MonoBehaviour
{
    public void Awake( )
    {
        //
        FB.Init( this.OnInitComplete, this.OnHideUnity );

        DontDestroyOnLoad( gameObject );
    }

    private void OnInitComplete( )
    {
        string logMessage = string.Format( "OnInitCompleteCalled IsLoggedIn='{0}' IsInitialized='{1}'", FB.IsLoggedIn, FB.IsInitialized );
        Debug.Log( logMessage );
        if(AccessToken.CurrentAccessToken != null)
        {
            Debug.Log( AccessToken.CurrentAccessToken.ToString( ) );
        }
    }

    private void OnHideUnity( bool isGameShown )
    {
        Debug.Log( "[Facebook] Is game shown: " + isGameShown );
    }
}

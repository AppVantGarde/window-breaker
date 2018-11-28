using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using DG.Tweening;

public class UIScreenManager : MonoBehaviour
{
    #region Singleton
    private static UIScreenManager _instance;
    public static UIScreenManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = Instantiate(Resources.Load<GameObject>("Prefabs/UI/screen_manager")).GetComponent<UIScreenManager>();
                _instance.name = "screen_manager";

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }
    #endregion

    private List<UIScreen> _loadedScreens = new List<UIScreen>();

    private string[] _screenHistory = new string[5];

    #region Unity Engine Callbacks

    public void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            _instance.name = "screen_manager";
            DontDestroyOnLoad(_instance.gameObject);
        }
    }

    public void Update()
    {
        bool otherScreenHasFocus = false;

        for(int i = _loadedScreens.Count - 1; i >= 0; i--)
        {
            if(!_loadedScreens[i].gameObject.activeInHierarchy)
                continue;

            UIScreen screen = _loadedScreens[i];

            if(screen.screenState == UIScreenState.TransitionOn || screen.screenState == UIScreenState.Active)
            {
                if(!otherScreenHasFocus)
                    otherScreenHasFocus = true;

                screen.coveredByOtherScreen = screen.hideLesserScreens;
            }
        }
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="screenToOpen"></param>
    public void Open(string screenToOpen)
    {
        if(screenToOpen == "previous")
        {
            screenToOpen = _screenHistory[_screenHistory.Length - 2];
        }

        UIScreen nextScreen = null;
        if(!GetScreen(screenToOpen, out nextScreen))
        {
            if(!LoadScreen(screenToOpen))
                throw new Exception("Failed to load screen '" + screenToOpen + "'. Check file path.");

            //
            GetScreen(screenToOpen, out nextScreen);
        }

        UIScreen activeScreen = null;
        if(!GetScreen(_screenHistory[_screenHistory.Length - 1], out activeScreen))
        {
            // If there is no active screen then we might be opening our very first screen.
            ActivateScreen(nextScreen);
        }
        else
        {
            //
            activeScreen.Close();

            if(activeScreen.transitionOffTime <= 0)
            {
                ChangeScreens(activeScreen, nextScreen);
            }
            else
            {
                DOVirtual.DelayedCall(activeScreen.transitionOffTime + 0.1f, () => { ChangeScreens(activeScreen, nextScreen); });
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="screenToClose"></param>
    /// <param name="screenToOpen"></param>
    private void ChangeScreens(UIScreen screenToClose, UIScreen screenToOpen)
    {
        //
        DeactivateScreen(screenToClose);

        //
        ActivateScreen(screenToOpen);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="screenToActivate"></param>
    private void ActivateScreen(UIScreen screenToActivate)
    {
        screenToActivate.closing = false;

        screenToActivate.gameObject.SetActive(true);

        screenToActivate.transform.SetAsLastSibling();

        UpdateScreenHistory(screenToActivate.screenName);

        screenToActivate.Open();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="screenToDeactivate"></param>
    private void DeactivateScreen(UIScreen screenToDeactivate)
    {
        if(screenToDeactivate.destroyOnDeactivate)
        {
            _loadedScreens.Remove(screenToDeactivate);

            Destroy(screenToDeactivate.gameObject);

            screenToDeactivate = null;
        }
        else
        {
            screenToDeactivate.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="previousScreen"></param>
    private void UpdateScreenHistory(string screenToRecord)
    {
        for(int i = 0; i < _screenHistory.Length - 1; i++)
            _screenHistory[i] = _screenHistory[i + 1];

        _screenHistory[_screenHistory.Length - 1] = screenToRecord;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="screenType"></param>
    /// <param name="screen"></param>
    /// <returns></returns>
    public bool GetScreen(string screenType, out UIScreen screen)
    {
        for(int i = 0; i < _loadedScreens.Count; i++)
            if(_loadedScreens[i].screenName == screenType)
            {
                screen = _loadedScreens[i];
                return true;
            }

        screen = null;
        return false;
    }

    public bool IsScreenActive( string screenName )
    {
        UIScreen activeScreen = null;
        GetScreen( _screenHistory[_screenHistory.Length - 1], out activeScreen );

        if(activeScreen == null)
            return false;

        return activeScreen.screenName == screenName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="screenToLoad"></param>
    /// <returns>True if the screen is loaded, false otherwise.</returns>
    public bool LoadScreen(string screenToLoad)
    {
        UIScreen screen = null;
        if(GetScreen(screenToLoad, out screen))
            return true;

        screen = Instantiate( Resources.Load<GameObject>("Prefabs/UI/screen_" + screenToLoad)).GetComponent<UIScreen>( );
        if(screen == null)
            return false;

        screen.gameObject.SetActive( false );
        screen.transform.parent         = transform;
        screen.screenRect.sizeDelta     = Vector2.zero;
        screen.transform.localScale     = Vector3.one;
        screen.transform.localPosition  = Vector3.zero;
        
        _loadedScreens.Add(screen);
        return true;
    }
}

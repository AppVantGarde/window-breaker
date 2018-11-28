using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class UIScreen_GameOver : UIScreen
{
    public SharedInt windowCount;
    public SharedPersistentInt currentLevel;
    public SharedInt windowBrokenCount;

    public Image background;
    public TMP_Text currentLevelText;
    public TMP_Text windowsText;
    public UIButton retryButton;

    public void OnEnable( )
    {
        retryButton.OnButtonPressed += OnButtonPressed_Retry;
    }

    public void OnDisable( )
    {
        retryButton.OnButtonPressed -= OnButtonPressed_Retry;
    }

    public override void Open( )
    {
        background.color = new Color( 0, 0, 0, 0 );
        background.DOColor( new Color( 0, 0, 0, 0.5f ), 0.5f );

        currentLevelText.SetText( "LEVEL " + currentLevel.Value );
        windowsText.SetText( windowBrokenCount.value + " / " + windowCount.value );
    }

    private void OnButtonPressed_Retry( )
    {
        GameStateManager.Instance.ChangeGameState( GameStateManager.GameState.Home );
    }
}

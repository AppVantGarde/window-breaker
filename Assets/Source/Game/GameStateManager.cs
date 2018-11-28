using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using GameAnalyticsSDK;

public class GameStateManager : MonoBehaviour
{
    public int startingLevel = -1;

    public static GameStateManager Instance;

    public enum GameState { Home, StartGame, Gameplay, SwitchLevel, GameOver, Waiting }

    [NonSerialized] public GameState gameState;

    public SharedFloat shotClock;
    public SharedInt ammoCount;
    public SharedInt windowCount;
    public SharedPersistentInt currentLevel;
    public SharedInt windowBrokenCount;

    public GameInfo[] gameLevels;

    public GameInfo activeGame;
    private GameInfo _previousGame;

    private UIScreen_Gameplay _gameplayScreen;
    private string[ ] _messages;

    public Player player;
    public ParticleSystem feverBallExplosion;

    public List<BreakableObject> unparentedWindows = new List<BreakableObject>( );

    public ParticleSystem glassBreakParticleFX;

    

    #region Unity Engine Callbacks

    public void Awake( )
    {
        Instance = this;

        if(startingLevel > 50)
            startingLevel = 50;

        if(startingLevel == 0)
            startingLevel = 1;
    }

    public void Start( )
    {
        //
        SaveGame.Instance.Load( );

        //
        Instantiate( Resources.Load( "Prefabs/Audio/audio_manager" ) );

        //
        Instantiate( Resources.Load( "Prefabs/UI/screen_manager" ) );

        //
        ChangeGameState( GameState.Home );

        _messages = new string[ ]
        {
            "AWESOME",
            "AMAZING",
            "BRILLIANT",
            "FANTASTIC",
            "INCREDIBLE",
            "MARVELOUS",
            "STUNNING",
            "UNBELIEVABLE",
            "WONDERFUL",
            "EXCELLENT",
            "FABULOUS",
            "MAGNIFICENT",
            "OUTSTANDING",
            "PHENOMENAL",
            "REMARKABLE",
            "SENSATIONAL",
            "SUPERB",
            "TERRIFIC"
        };
    }

    public void OnApplicationQuit( )
    {
        //
        SaveGame.Instance.Save( );
    }

    public void Update( )
    {
        if(gameState == GameState.Gameplay)
        {  
            if(windowBrokenCount.value >= windowCount.value)
            {
                ChangeGameState( GameState.Waiting );

                DOVirtual.DelayedCall( 0.5f, ( ) => { ChangeGameState( GameState.SwitchLevel ); } );
            }
            else if(ammoCount.value <= 0 )//|| (shotClock.value < 0 && currentLevel.Value > 0))
            {
                iOSHapticFeedback.Instance.Trigger( iOSHapticFeedback.iOSFeedbackType.Failure );

                ChangeGameState( GameState.GameOver );
            }

           // shotClock.value -= Time.deltaTime;
        }
    }

    public void TriggerGameOver( )
    {
        iOSHapticFeedback.Instance.Trigger( iOSHapticFeedback.iOSFeedbackType.Failure );

        ChangeGameState( GameState.GameOver );
    }

    #endregion

    public void ChangeGameState( GameState newGameState )
    {
        // We might just be restarting a state, so don't end it of so.
        if(gameState != newGameState)
        {
            // End current state.
            switch(gameState)
            {
                case GameState.Home:
                    break;

                case GameState.Gameplay:
                    {
                        FocusTarget.instance.idle = true;
                    }
                    break;
            }
        }

        gameState = newGameState;

        // Start next state.
        switch(newGameState)
        {
            case GameState.Home:
                {
                    GameAnalytics.NewProgressionEvent( GAProgressionStatus.Start, "game" );

                    if(startingLevel > 0)
                        currentLevel.Value = startingLevel;

                    ChangeLevel( currentLevel.Value, true );
                }
                break;

            case GameState.Gameplay:
                {

                    FocusTarget.instance.idle = false;
                    UIScreenManager.Instance.Open( "gameplay" );
                }
                break;

            case GameState.SwitchLevel:
                {
                    //UIScreen gameplayScreen;
                    //UIScreenManager.Instance.GetScreen( "gameplay", out gameplayScreen );
                    //gameplayScreen.Close( );

                    int level = currentLevel.Value;

                    if(++level >= gameLevels.Length)
                    {
                        level = UnityEngine.Random.Range( 0, gameLevels.Length );
                    }

                    currentLevel.Value = level;

                    DOVirtual.DelayedCall( 0.5f, ( ) => { ChangeLevel( currentLevel.Value ); } );
                }
                break;

            case GameState.GameOver:
                {
                    player._feverValue = 0;
                    GameAnalytics.NewProgressionEvent( GAProgressionStatus.Complete, "game", windowBrokenCount.value );

                    DOVirtual.DelayedCall( 1f, ( ) => { UIScreenManager.Instance.Open( "gameOver" ); } );                    
                }break;
        } 
    }

    public void ChangeLevel(int level, bool instant = false )
    {
        AudioManager.Instance.Play( "cue_level_up" );

        _previousGame = activeGame;

        activeGame = Instantiate( gameLevels[level] );
        
        for(int i = 0; i < unparentedWindows.Count; i++)
        {
            unparentedWindows[i].gameObject.transform.parent = activeGame.transform;
        }
        unparentedWindows.Clear( );

        activeGame.transform.position = new Vector3( -30, 0, 0 );

        if(_previousGame != null)
        {
            _previousGame.transform.DOMove( new Vector3( 30, 0, 0 ), 0.5f ).OnComplete( ( ) =>
            {
                Destroy( _previousGame.gameObject );
            } );

            activeGame.transform.DOMove( Vector3.zero, 0.5f );
        }
        else
        {
            activeGame.transform.position = Vector3.zero;
        }

        windowCount.value = activeGame.TotalWindowCount( );
        windowBrokenCount.value = 0;

        Borodar.FarlandSkies.LowPoly.SkyboxController.Instance.TopColor = activeGame.topColor;
        Borodar.FarlandSkies.LowPoly.SkyboxController.Instance.MiddleColor = activeGame.middleColor;
        Borodar.FarlandSkies.LowPoly.SkyboxController.Instance.BottomColor = activeGame.bottomColor;

        float timer = activeGame.gameTime;
        if(shotClock.value >= 1.0f)
            timer += 1f;

        shotClock.value = timer;

        ammoCount.value = activeGame.ammoCount;

        ChangeGameState( GameState.Waiting );
        DOVirtual.DelayedCall( 0.5f, ( ) => { ChangeGameState( GameState.Gameplay ); } );
    }

    public void ShowMessage( bool perfect )
    {
        if(_gameplayScreen == null)
        {
            UIScreen screen;
            UIScreenManager.Instance.GetScreen( "gameplay", out screen );

            _gameplayScreen = (UIScreen_Gameplay)screen;
        }

        _gameplayScreen.ShowMessage( perfect ? "PERFECT!" : _messages[UnityEngine.Random.Range(0, _messages.Length)] );
    }

    public void BonusTimeMessage( int additionalTime )
    {
        if(_gameplayScreen == null)
        {
            UIScreen screen;
            UIScreenManager.Instance.GetScreen( "gameplay", out screen );

            _gameplayScreen = (UIScreen_Gameplay)screen;
        }

        _gameplayScreen.BonusTimeMessage( additionalTime );
    }

    public void PlayGlassBreakParticleFX( Vector3 position )
    {
        glassBreakParticleFX.gameObject.transform.position = position;
        glassBreakParticleFX.Play( );
    }
}

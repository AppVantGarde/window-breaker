using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class UIScreen_Gameplay : UIScreen
{
    public SharedFloat shotClock;
    public SharedInt ammoCount;
    public SharedInt windowCount;
    public SharedPersistentInt currentLevel;
    public SharedInt windowBrokenCount;

    public Slider progressSlider;
    public Slider feverSlider;
    public Image feverFillImage;
    public TMP_Text currentLevelText;
    public TMP_Text nextLevelText;
    public TMP_Text windowCountText;
    public TMP_Text ammoCountText;
    public TMP_Text shotClockText;

    public TMP_Text messageText;
    public TMP_Text tutorialText;
    public RectTransform meesageTextTransform;
    public TMP_Text bonusTimeText;
    public RectTransform bonusTextTransform;

    private Sequence feverAnim;
    private bool _feverActive;
    private float _playerFeverValue;

    private Color _defaultFeverSliderColor = new Color( 0, 0.8378737f, 1 );
    private Color _feverColor1 = new Color( 1, 0.8623215f, 0.504717f );
    private Color _feverColor2 = new Color( 1, 0.5222169f, 0.5058824f );

    public override void Open( )
    {
        _prevWinowBrokenCount = windowBrokenCount.value;

        progressSlider.DOValue( 0, 0.5f );

        currentLevelText.SetText( (currentLevel.Value + 1).ToString() );
        nextLevelText.SetText( (currentLevel.Value + 2).ToString( ) );

        windowCountText.SetText( (windowCount.value - windowBrokenCount.value).ToString( ) );

        meesageTextTransform = messageText.GetComponent<RectTransform>( );
        bonusTextTransform = bonusTimeText.GetComponent<RectTransform>( );

        if(currentLevel.Value == 0)
        {
            shotClockText.SetText( "" );
            tutorialText.gameObject.SetActive( true );
        }
    }

    private int _prevAmmoCount;
    private int _prevWinowBrokenCount;
    private float _prevShotClockTime;
    public void Update( )
    {
        if(currentLevel.Value > 0)
        {
            tutorialText.gameObject.SetActive( false );
        }

        if(_prevWinowBrokenCount != windowBrokenCount.value)
        {
            float progress = windowBrokenCount.value / (float)windowCount.value;

            progressSlider.DOValue( progress, 0.5f );

            windowCountText.SetText( (windowCount.value - windowBrokenCount.value).ToString( ) );

            _prevWinowBrokenCount = windowBrokenCount.value;
        }

        if(_prevAmmoCount != ammoCount.value)
        {
            ammoCountText.SetText( "x" + ammoCount.value );

            if( _prevAmmoCount > ammoCount.value )
                ammoCountText.transform.DOPunchScale( Vector3.one * -0.25f, 0.3f );
            else
                ammoCountText.transform.DOPunchScale( Vector3.one * 0.5f, 0.5f );

            _prevAmmoCount = ammoCount.value;
        }

        if(_prevShotClockTime != shotClock.value && currentLevel.Value > 0)
        {
            shotClockText.SetText( string.Format( "{0:F1}", shotClock.value ) );

            _prevShotClockTime = shotClock.value;
        }

        if(_playerFeverValue != GameStateManager.Instance.player._feverValue)
        {
            _playerFeverValue = GameStateManager.Instance.player._feverValue;

            feverSlider.DOValue( _playerFeverValue, 0.5f );
        }

        if(_feverActive != GameStateManager.Instance.player.HasFever( ))
        {
            if(GameStateManager.Instance.player.HasFever( ))
            {
                feverAnim = DOTween.Sequence( );
                feverAnim.Append( feverFillImage.DOColor( _feverColor1, 0.25f ) );
                feverAnim.Append( feverFillImage.DOColor( _feverColor2, 0.25f ) );
                feverAnim.SetLoops( -1, LoopType.Yoyo );
                feverAnim.Play( );
                
            }
            else
            {
                feverAnim.Pause( );
                DOTween.Kill( feverAnim.id );



                feverFillImage.DOColor( _defaultFeverSliderColor, 0.25f );
            }

            _feverActive = GameStateManager.Instance.player.HasFever( );
        }
    }

    public void ShowMessage( string message )
    {
        messageText.SetText( message );
        messageText.color = Color.white;

        meesageTextTransform.localScale = message == "PERFECT!" ? new Vector3( 2, 2, 2 ) : Vector3.one;

        meesageTextTransform.anchoredPosition = Vector3.zero;
        meesageTextTransform.DOAnchorPosY( 100, 0.5f ).SetEase( Ease.OutCubic );
        messageText.DOColor( new Color( 1, 1, 1, 0 ), 0.25f ).SetDelay( 0.6f );
    }

    public void BonusTimeMessage( int additionalTime )
    {
        bonusTimeText.SetText( "+" + additionalTime + " PERFECT SHOT" );
        bonusTimeText.color = Color.green;

        bonusTextTransform.anchoredPosition = Vector3.zero;
        bonusTextTransform.DOAnchorPosY( 30, 0.5f ).SetEase( Ease.OutCubic );
        bonusTimeText.DOColor( new Color( 1, 1, 1, 0 ), 0.25f ).SetDelay( 0.6f );
    }
}

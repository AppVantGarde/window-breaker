using System;

using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

[CreateAssetMenu] public class AudioCue : ScriptableObject
{
    [NonSerialized] private int _index; // The index where this AudioCue lives in the AudioEngine.
    [NonSerialized] private bool _hasBeenRegistered;
    [NonSerialized] private bool _hasCustomAudioGroup;
    [NonSerialized] private bool _hasCustomAudioSource;
    
    public AudioClip[] clips;
    public AudioMixerGroup group;
    public AudioSource source;
     
    [Space(10)]
    [MinMaxRange(-3, 3)] public RangedFloat pitch;
    [MinMaxRange(0, 1)] public RangedFloat volume;

    /// <summary>
    /// Plays the AudioCue on the specified AudioSource. Overrides custom AudioSource if one exists.
    /// </summary>
    /// <param name="audioSource">The AudioSource to play thie cue on.</param>
    public void Play(AudioSource audioSource)
    {
        RegisterWithAudioEngine();

        if(clips.Length == 0)
            return;

        audioSource.clip     = clips[Random.Range(0, clips.Length)];
        audioSource.volume   = Random.Range(volume.minimum, volume.maximum);
        audioSource.pitch    = Random.Range(pitch.minimum, pitch.maximum);
        audioSource.Play();
    }

    /// <summary>
    /// Plays the AudioCue on a 2D source from the AudioManager.
    /// </summary>
    /// <param name="ignorePause">Should this sound continue playing is the game gets paused.</param>
    public void Play(bool ignorePause = false)
    {
        RegisterWithAudioEngine( );

        AudioManager.Instance.Play( _index, ignorePause );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    /// <param name="ignorePause"></param>
    public void Play( float volume, float pitch, bool ignorePause = false )
    {
        RegisterWithAudioEngine( );

        AudioManager.Instance.Play( _index, volume, pitch, ignorePause );
    }

    /// <summary>
    /// Plays the AudioCue on a 3D source from the AudioManager.
    /// </summary>
    /// <param name="location">The location of this sound.</param>
    /// <param name="ignorePause">Should this sound continue playing is the game gets paused.</param>
    public void Play(Vector3 location, bool ignorePause = false)
    {
        RegisterWithAudioEngine();
    }

    /// <summary>
    /// 
    /// </summary>
    public void RegisterWithAudioEngine()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
            return;
#endif
        if(_hasBeenRegistered)
            return;

        _hasBeenRegistered      = true;
        _hasCustomAudioGroup    = group != null;
        _hasCustomAudioSource   = source != null;
        
        _index = AudioManager.Instance.RegisterAudioCue(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool UsesCustomAudioGroup() { return _hasCustomAudioGroup; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool UsesCustomAudioSource() { return _hasCustomAudioSource; } 
}

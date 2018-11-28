using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = Instantiate(Resources.Load<GameObject>("Prefabs/Audio/audio_manager")).GetComponent<AudioManager>();
                _instance.name = "audio_manager";

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }
    #endregion

    private class CachedAudioSource
    {
        public int customId;  
        public AudioSource audioSource;
    }

    public List<AudioCue> audioCues;

    private List<CachedAudioSource> _activeAudioSources;
    private List<CachedAudioSource> _inactiveAudioSources;

    #region Unity Engine Callbacks

    public void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            _instance.name = "audio_manager";
            DontDestroyOnLoad(_instance.gameObject);
        }

        _activeAudioSources     = new List<CachedAudioSource>();
        _inactiveAudioSources   = new List<CachedAudioSource>();

        for(int i = 0; i < audioCues.Count; i++)
            audioCues[i].RegisterWithAudioEngine();
    }

    public void Update()
    {
        for(int i = 0; i < _activeAudioSources.Count; i++ )
        {
            CachedAudioSource activeSource = _activeAudioSources[i];
            if(!activeSource.audioSource.isPlaying)
            {
                AudioSourceFinished(activeSource);

                _activeAudioSources.RemoveAt(i--);
            }
        }
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="audioCue"></param>
    /// <returns></returns>
    public int RegisterAudioCue(AudioCue audioCue)
    {
        for(int i = 0; i < audioCues.Count; i++)
        {
            if(string.Equals(audioCue.name, audioCues[i].name))
                return i;
        }

        audioCues.Add(audioCue);

        return audioCues.Count - 1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="audioToCache"></param>
    private void AudioSourceFinished( CachedAudioSource audioToCache )
    {
        audioToCache.audioSource.transform.parent = transform;
        audioToCache.audioSource.gameObject.SetActive(false);

        _inactiveAudioSources.Add(audioToCache);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private CachedAudioSource GetAudioSource(int index)
    {
        if(audioCues[index].UsesCustomAudioSource())
        {
            for(int i = 0; i < _inactiveAudioSources.Count; i++)
            {
                if(index == _inactiveAudioSources[i].customId)
                {
                    CachedAudioSource source = _inactiveAudioSources[i];

                    _inactiveAudioSources.RemoveAt(i);

                    return source;
                }
            }

            CachedAudioSource customSource = new CachedAudioSource();
            customSource.customId          = index;
            customSource.audioSource       = Instantiate(audioCues[index].source);
            customSource.audioSource.gameObject.name = "audio_source_" + (_activeAudioSources.Count + _inactiveAudioSources.Count + 1);

            return customSource;
        }

        if(_inactiveAudioSources.Count > 0)
        {
            CachedAudioSource source = _inactiveAudioSources[_inactiveAudioSources.Count - 1];

            _inactiveAudioSources.RemoveAt(_inactiveAudioSources.Count - 1);

            return source;
        }

        CachedAudioSource newSource = new CachedAudioSource();
        newSource.audioSource = new GameObject("audio_source_" + (_activeAudioSources.Count + _inactiveAudioSources.Count + 1)).AddComponent<AudioSource>();
        newSource.audioSource.playOnAwake = false;

        return newSource;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="ignorePause"></param>
    private void PlayAudioCue(int index, bool ignorePause )
    {
        //if(audioMuted.Value || audioCues[index].clips.Length == 0)
        //    return;

        CachedAudioSource source    = GetAudioSource(index);
        source.audioSource.clip     = audioCues[index].clips[Random.Range(0, audioCues[index].clips.Length)];
        source.audioSource.pitch    = Random.Range(audioCues[index].pitch.minimum, audioCues[index].pitch.maximum);
        source.audioSource.volume   = Random.Range(audioCues[index].volume.minimum, audioCues[index].volume.maximum);
        source.audioSource.ignoreListenerPause = ignorePause;

        // We only assign a group if a custom AudioSource is not present.
        if(!audioCues[index].UsesCustomAudioSource())
        {
            if(audioCues[index].UsesCustomAudioGroup())
                source.audioSource.outputAudioMixerGroup = audioCues[index].group;
        }

        source.audioSource.gameObject.SetActive(true);
        source.audioSource.transform.parent = transform;
        source.audioSource.Play();

        _activeAudioSources.Add(source);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    /// <param name="ignorePause"></param>
    private void PlayAudioCue( int index, float volume, float pitch, bool ignorePause )
    {
        CachedAudioSource source = GetAudioSource( index );
        source.audioSource.clip = audioCues[index].clips[Random.Range( 0, audioCues[index].clips.Length )];
        source.audioSource.pitch = pitch;
        source.audioSource.volume = volume;
        source.audioSource.ignoreListenerPause = ignorePause;

        // We only assign a group if a custom AudioSource is not present.
        if(!audioCues[index].UsesCustomAudioSource( ))
        {
            if(audioCues[index].UsesCustomAudioGroup( ))
                source.audioSource.outputAudioMixerGroup = audioCues[index].group;
        }

        source.audioSource.gameObject.SetActive( true );
        source.audioSource.transform.parent = transform;
        source.audioSource.Play( );

        _activeAudioSources.Add( source );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool IsValidAudioCue(int index)
    {
        if(index < 0 || index >= audioCues.Count)
            return false;

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private int GetAudioCueIndexByName(string name)
    {
        for(int i = 0; i < audioCues.Count; i++)
        {
            if(string.Equals(name, audioCues[i].name))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="ignorePause"></param>
    public void Play(int index, bool ignorePause = false)
    {
        if(!IsValidAudioCue(index))
            return;

        PlayAudioCue(index, ignorePause);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    /// <param name="ignorePause"></param>
    public void Play( int index, float volume, float pitch, bool ignorePause = false )
    {
        if(!IsValidAudioCue( index ))
            return;

        PlayAudioCue( index, volume, pitch, ignorePause );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignorePause"></param>
    public void Play(string name, bool ignorePause = false)
    {
        int index = GetAudioCueIndexByName(name);

        if(!IsValidAudioCue(index))
            return;

        PlayAudioCue(index, ignorePause);
    }

    public void Play(int index, Vector3 location, bool ignorePause = false) { }
    public void Play(string name, Vector3 location, bool ignorePause = false) { }
    public void Play(int index, GameObject attachee, bool ignorePause = false) { }
    public void Play(string name, GameObject attachee, bool ignorePause = false) { }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum SoundType
{
    SINGLE = 0,
    BGM,
    MIXER,
}

public class SoundManager : MonoSingleton<SoundManager>
{
    private sealed class SoundClip
    {
        public AudioClip audioClip { get; set; }
        public float clipVolume { get; set; }
        public Action onCompleteCallback { get; set; }

    }
    private Dictionary<SoundType, AudioSource> _audioSourceDict = new Dictionary<SoundType, AudioSource>();
    private Dictionary<SoundType, SoundClip> _soundClipDict = new Dictionary<SoundType, SoundClip>();
    private Dictionary<AudioSource, SoundClip> _audioSourceMixerDit = new Dictionary<AudioSource, SoundClip>();

    private List<AudioSource> mixerListKey = new List<AudioSource>();

    private GameObject _root;

    private void Awake()
    {
        for (SoundType type = SoundType.SINGLE; type <= SoundType.BGM; type++)
        {
            _audioSourceDict.Add(type, CreateAudioSource(type));
            SoundClip soundClip = new SoundClip();
            _soundClipDict.Add(type, soundClip);
        }

        if(!PlayerPrefs.HasKey("VOLUME"))
        {
            PlayerPrefs.SetFloat("VOLUME", 1f);
        }

    }

    AudioSource CreateAudioSource(SoundType soundType)
    {
        if (_root == null)
        {
            _root = new GameObject("AudioSources");
            DontDestroyOnLoad(_root);
        }
        AudioSource audioSource = _root.AddComponent<AudioSource>();
        audioSource.loop = soundType == SoundType.BGM ? true : false;
        audioSource.playOnAwake = false;
        audioSource.volume = PlayerPrefs.GetFloat("VOLUME");
        return audioSource;
    }

    AudioSource CreateMixerAudioSource()
    {
        AudioSource audioSource = _root.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = PlayerPrefs.GetFloat("VOLUME");
        return audioSource;
    }

    void Update()
    {
        for (SoundType type = SoundType.SINGLE; type <= SoundType.BGM; type++)
        {
            AudioSource audioSource = _audioSourceDict[type];

            if (audioSource.clip != null)
            {
                if (audioSource.volume != _soundClipDict[type].clipVolume)
                {
                    audioSource.volume = _soundClipDict[type].clipVolume;
                }

                if (!audioSource.loop && audioSource.time + 0.003f >= audioSource.clip.length)
                {
                    Stop(type);
                }
            }
        }

        RefreshMixerDic();
        var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
        while (mixerEnumerator.MoveNext())
        {
            var _cur = mixerEnumerator.Current;
            if (!_cur.Key.loop && _cur.Key.time + 0.003f >= _cur.Key.clip.length)
            {
                Stop(SoundType.MIXER);
            }
        }
    }

    void RefreshMixerDic()
    {
        if (mixerListKey.Count > 0)
        {
            for (int i = 0; i < mixerListKey.Count; i++)
            {
                _audioSourceMixerDit.Remove(mixerListKey[i]);
                UnityEngine.Object.Destroy(mixerListKey[i]);
            }
            mixerListKey.RemoveRange(0, mixerListKey.Count > 0 ? mixerListKey.Count - 1 : 0);
        }
    }

    public void Stop(SoundType soundType)
    {
        if (soundType != SoundType.MIXER)
        {
            if (_audioSourceDict.ContainsKey(soundType))
            {
                _audioSourceDict[soundType].Stop();
                _audioSourceDict[soundType].clip = null;
                _audioSourceDict[soundType].volume = PlayerPrefs.GetFloat("VOLUME");
                if (_soundClipDict[soundType].onCompleteCallback != null)
                {
                    _soundClipDict[soundType].onCompleteCallback();
                    _soundClipDict[soundType].onCompleteCallback = null;
                }
            }
        }
        else
        {
            var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
            while (mixerEnumerator.MoveNext())
            {
                var _cur = mixerEnumerator.Current;
                if (_cur.Value.onCompleteCallback != null)
                {
                    _cur.Value.onCompleteCallback();
                    _cur.Value.onCompleteCallback = null;
                }
                mixerListKey.Add(_cur.Key);
            }
        }
    }

    private void PlaySoundClip(SoundType soundType, SoundClip soundClip)
    {
        //非混合模式停止当前audioSource
        if (soundType != SoundType.MIXER)
        {
            Stop(soundType);
        }

        if (soundClip != null)
        {
            if (soundType != SoundType.MIXER)
            {

                _soundClipDict[soundType] = soundClip;
                _audioSourceDict[soundType].clip = soundClip.audioClip;
                _audioSourceDict[soundType].volume = soundClip.clipVolume;
                _audioSourceDict[soundType].Play();
            }
            else
            {
                AudioSource audiosource = CreateMixerAudioSource();
                audiosource.clip = soundClip.audioClip;
                audiosource.volume = soundClip.clipVolume;
                Debug.Log(soundClip.audioClip.name);
                audiosource.Play();
                Debug.Log(audiosource.isPlaying);
                _audioSourceMixerDit.Add(audiosource, soundClip);
            }

        }
    }

    public void Play(SoundType soundType, string soundName, float volume = 1f, Action onCompleteCallback = null)
    {
        ResourceManager.Instance.LoadFromAssetBundleAsync<AudioClip>("audio", soundName, (obj) =>
        {
            SoundClip soundClip = new SoundClip();
            AudioClip audioClip = obj;
            soundClip.audioClip = audioClip;
            PlayerPrefs.SetFloat("VOLUME", volume);
            soundClip.clipVolume = PlayerPrefs.GetFloat("VOLUME");
            soundClip.onCompleteCallback = onCompleteCallback;
            PlaySoundClip(soundType, soundClip);
        });
    }

    public void Pause(SoundType soundType)
    {
        if (soundType != SoundType.MIXER)
        {
            _audioSourceDict[soundType].Pause();
        }
        else
        {
            var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
            while (mixerEnumerator.MoveNext())
            {
                var _cur = mixerEnumerator.Current;
                _cur.Key.Pause();
            }
        }
    }

    public void Resume(SoundType soundType)
    {
        if (soundType != SoundType.MIXER)
        {
            _audioSourceDict[soundType].UnPause();
        }
        else
        {
            var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
            while (mixerEnumerator.MoveNext())
            {
                var _cur = mixerEnumerator.Current;
                _cur.Key.UnPause();
            }
        }
    }

    public void SetSoundVolumeByType(SoundType soundType, float volume)
    {
        PlayerPrefs.SetFloat("VOLUME", volume);
        if (soundType != SoundType.MIXER)
        {
            _soundClipDict[soundType].clipVolume = PlayerPrefs.GetFloat("VOLUME");
        }
        else
        {
            var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
            while (mixerEnumerator.MoveNext())
            {
                var _cur = mixerEnumerator.Current;
                _cur.Value.clipVolume = PlayerPrefs.GetFloat("VOLUME");
            }
        }
    }

    public void SetSoundVolume(float volume)
    {
        PlayerPrefs.SetFloat("VOLUME",volume);
        for (SoundType type = SoundType.SINGLE; type <= SoundType.BGM; type++)
        {
            _soundClipDict[type].clipVolume = PlayerPrefs.GetFloat("VOLUME");
        }
        var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
        while (mixerEnumerator.MoveNext())
        {
            var _cur = mixerEnumerator.Current;
            _cur.Value.clipVolume = PlayerPrefs.GetFloat("VOLUME");
        }

    }

    public bool IsPlaying(SoundType soundType)
    {
        if (soundType != SoundType.MIXER)
        {
            if (_audioSourceDict.ContainsKey(soundType))
            {
                return _audioSourceDict[soundType].isPlaying;
            }
        }
        else
        {
            var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
            while (mixerEnumerator.MoveNext())
            {
                var _cur = mixerEnumerator.Current;
                if (_cur.Key.isPlaying)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void MuteAll(bool mute)
    {
        var _enumerator = _audioSourceDict.GetEnumerator();
        while (_enumerator.MoveNext())
        {
            var _cur = _enumerator.Current;
            _cur.Value.mute = mute;
        }

        var mixerEnumerator = _audioSourceMixerDit.GetEnumerator();
        while (mixerEnumerator.MoveNext())
        {
            var _cur = mixerEnumerator.Current;
            _cur.Key.mute = mute;
        }
    }
}



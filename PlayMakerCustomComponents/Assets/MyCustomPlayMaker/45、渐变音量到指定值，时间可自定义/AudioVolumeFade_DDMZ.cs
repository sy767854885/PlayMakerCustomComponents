using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Audio")]
[HutongGames.PlayMaker.Tooltip("渐变音量到指定值，时间可自定义")]
public class AudioVolumeFade_DDMZ : FsmStateAction
{
    [RequiredField]
    [CheckForComponent(typeof(AudioSource))]
    [HutongGames.PlayMaker.Tooltip("要控制的音频源")]
    public FsmOwnerDefault audioSource;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("目标音量值")]
    [HasFloatSlider(0f, 1f)]
    public FsmFloat targetVolume;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("渐变时间（秒）")]
    public FsmFloat fadeTime;

    [HutongGames.PlayMaker.Tooltip("完成后是否发送事件")]
    public FsmEvent finishedEvent;

    private AudioSource _source;
    private float _startVolume;
    private float _timer;

    public override void Reset()
    {
        audioSource = null;
        targetVolume = 1f;
        fadeTime = 1f;
        finishedEvent = null;
    }

    public override void OnEnter()
    {
        _source = Fsm.GetOwnerDefaultTarget(audioSource)?.GetComponent<AudioSource>();

        if (_source == null)
        {
            Finish();
            return;
        }

        _startVolume = _source.volume;
        _timer = 0f;
    }

    public override void OnUpdate()
    {
        if (_source == null) return;

        _timer += Time.deltaTime;
        float t = fadeTime.Value > 0 ? _timer / fadeTime.Value : 1f;
        _source.volume = Mathf.Lerp(_startVolume, targetVolume.Value, t);

        if (t >= 1f)
        {
            _source.volume = targetVolume.Value;
            Fsm.Event(finishedEvent);
            Finish();
        }
    }
}

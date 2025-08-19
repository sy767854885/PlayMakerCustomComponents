using HutongGames.PlayMaker;
using UnityEngine;

[ActionCategory(ActionCategory.Audio)]  // 在 PlayMaker 的 Action 面板中归类到 "Audio"
[HutongGames.PlayMaker.Tooltip("检查 AudioSource 是否正在播放。")]
public class GetAudioIsPlaying_DDMZ : FsmStateAction
{
    [RequiredField]
    [CheckForComponent(typeof(AudioSource))]
    public FsmOwnerDefault gameObject;  // 目标物体（必须包含 AudioSource 组件）

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("如果 AudioSource 正在播放则为 True")]
    public FsmBool isPlaying;           // 输出布尔值，表示是否正在播放

    [HutongGames.PlayMaker.Tooltip("当 AudioSource 正在播放时要发送的事件")]
    public FsmEvent isPlayingEvent;     // 如果正在播放，触发的事件

    [HutongGames.PlayMaker.Tooltip("当 AudioSource 没有播放时要发送的事件")]
    public FsmEvent isNotPlayingEvent;  // 如果没有播放，触发的事件

    private AudioSource _comp;          // 缓存 AudioSource 组件


    /// <summary>
    /// 初始化参数
    /// </summary>
    public override void Reset()
    {
        gameObject = null;
        isPlaying = null;
        isPlayingEvent = null;
        isNotPlayingEvent = null;
    }

    /// <summary>
    /// 进入状态时执行一次
    /// </summary>
    public override void OnEnter()
    {
        // 获取目标物体
        GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null) return;

        // 尝试获取 AudioSource 组件
        _comp = go.GetComponent<AudioSource>();
        if (_comp == null)
        {
            // 如果缺少 AudioSource，报错
            LogError("GetAudioIsPlaying: 目标缺少 AudioSource 组件！");
            return;
        }

        // 检查是否正在播放
        bool _isPlaying = _comp.isPlaying;

        // 把结果存入变量
        isPlaying.Value = _isPlaying;

        // 根据状态发送不同事件
        Fsm.Event(_isPlaying ? isPlayingEvent : isNotPlayingEvent);

        // 执行完毕，结束 Action
        Finish();
    }
}

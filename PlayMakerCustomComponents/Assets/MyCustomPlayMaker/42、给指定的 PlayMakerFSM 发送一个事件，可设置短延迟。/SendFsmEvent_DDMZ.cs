using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections;

[ActionCategory("Custom/FSM")]
[HutongGames.PlayMaker.Tooltip("给指定的 PlayMakerFSM 发送一个事件，可设置短延迟。")]
public class SendFsmEvent_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要操作的 PlayMakerFSM 组件（直接拖入组件）。")]
    [ObjectType(typeof(PlayMakerFSM))]
    public FsmObject targetFsm;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("发送给目标 FSM 的事件名称。")]
    public FsmString eventName;

    [HutongGames.PlayMaker.Tooltip("发送事件前的短延迟时间（秒）。")]
    public FsmFloat delayTime;

    public override void Reset()
    {
        targetFsm = null;
        eventName = new FsmString { UseVariable = false, Value = "" };
        delayTime = 0.01f; // 默认短延迟
    }

    public override void OnEnter()
    {
        Fsm.Owner.StartCoroutine(SendEventCoroutine());
    }

    private IEnumerator SendEventCoroutine()
    {
        if (targetFsm == null || targetFsm.Value == null || string.IsNullOrEmpty(eventName.Value))
        {
            Finish();
            yield break;
        }

        var fsmComponent = targetFsm.Value as PlayMakerFSM;
        if (fsmComponent == null)
        {
            Finish();
            yield break;
        }

        if (delayTime.Value > 0f)
            yield return new WaitForSeconds(delayTime.Value);

        SendEventToTargetFsm(fsmComponent, eventName.Value);

        Finish();
    }

    private void SendEventToTargetFsm(PlayMakerFSM fsmComponent, string eventName)
    {
        if (fsmComponent == null || string.IsNullOrEmpty(eventName)) return;

        try
        {
            // 优先使用 PlayMakerFSM.SendEvent(string)
            fsmComponent.SendEvent(eventName);
        }
        catch
        {
            try
            {
                // 备用：通过内部 Fsm.Event(FsmEvent)
                if (fsmComponent.Fsm != null)
                {
                    var tempEvent = FsmEvent.GetFsmEvent(eventName);
                    if (tempEvent != null)
                        fsmComponent.Fsm.Event(tempEvent);
                }
            }
            catch
            {
                try { Fsm.Event(eventName); } catch { }
            }
        }
    }
}

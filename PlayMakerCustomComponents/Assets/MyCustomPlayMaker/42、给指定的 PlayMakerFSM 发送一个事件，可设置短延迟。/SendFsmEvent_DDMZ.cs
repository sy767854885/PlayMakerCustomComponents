using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections;

[ActionCategory("Custom/FSM")]
[HutongGames.PlayMaker.Tooltip("给指定的 PlayMakerFSM 发送一个事件，可选择是否延迟发送。")]
public class SendFsmEvent_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要操作的 PlayMakerFSM 组件（直接拖入组件）。")]
    [ObjectType(typeof(PlayMakerFSM))]
    public FsmObject targetFsm;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("发送给目标 FSM 的事件名称。")]
    public FsmString eventName;

    [HutongGames.PlayMaker.Tooltip("是否启用延迟发送事件。")]
    public FsmBool useDelay;

    [HutongGames.PlayMaker.Tooltip("延迟时间（秒），只有启用延迟时生效。")]
    public FsmFloat delayTime;

    public override void Reset()
    {
        targetFsm = null;
        eventName = new FsmString { UseVariable = false, Value = "" };
        useDelay = false;
        delayTime = 0.01f;
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

        // 如果启用延迟且 delayTime > 0，则等待
        if (useDelay.Value && delayTime.Value > 0f)
            yield return new WaitForSeconds(delayTime.Value);

        SendEventToTargetFsm(fsmComponent, eventName.Value);

        Finish();
    }

    private void SendEventToTargetFsm(PlayMakerFSM fsmComponent, string eventName)
    {
        if (fsmComponent == null || string.IsNullOrEmpty(eventName)) return;

        try
        {
            fsmComponent.SendEvent(eventName);
        }
        catch
        {
            try
            {
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

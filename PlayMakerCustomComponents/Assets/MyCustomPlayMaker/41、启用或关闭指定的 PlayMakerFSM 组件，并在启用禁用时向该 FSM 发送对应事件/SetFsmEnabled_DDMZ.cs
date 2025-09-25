using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections;

[ActionCategory("Custom/FSM")]
[HutongGames.PlayMaker.Tooltip("启用或关闭指定的 PlayMakerFSM 组件，并在启用/禁用时向该 FSM 发送对应事件，带短延迟以确保事件接收。")]
public class SetFsmEnabled_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要操作的 PlayMakerFSM 组件（直接拖入组件）。")]
    [ObjectType(typeof(PlayMakerFSM))]
    public FsmObject targetFsm;

    [HutongGames.PlayMaker.Tooltip("是否启用 FSM（勾选=启用，取消勾选=禁用）。")]
    public FsmBool enable;

    [HutongGames.PlayMaker.Tooltip("当启用目标 FSM 时发送到目标 FSM 的事件名称（可选）。")]
    public FsmString onEnableEventName;

    [HutongGames.PlayMaker.Tooltip("当禁用目标 FSM 时发送到目标 FSM 的事件名称（可选）。")]
    public FsmString onDisableEventName;

    [HutongGames.PlayMaker.Tooltip("事件发送前的短延迟时间（秒）。")]
    public FsmFloat delayTime;

    public override void Reset()
    {
        targetFsm = null;
        enable = true;
        onEnableEventName = new FsmString { UseVariable = false, Value = "" };
        onDisableEventName = new FsmString { UseVariable = false, Value = "" };
        delayTime = 0.01f; // 默认短延迟
    }

    public override void OnEnter()
    {
        // 开启协程执行，支持短延迟
        Fsm.Owner.StartCoroutine(DoSetFsmEnabledCoroutine());
    }

    private IEnumerator DoSetFsmEnabledCoroutine()
    {
        if (targetFsm == null || targetFsm.Value == null)
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

        float waitTime = delayTime.Value;

        if (!enable.Value)
        {
            // 发送禁用事件
            if (!string.IsNullOrEmpty(onDisableEventName.Value))
            {
                SendEventToTargetFsm(fsmComponent, onDisableEventName.Value);
            }

            // 等待短延迟
            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);

            // 禁用 FSM
            fsmComponent.enabled = false;
        }
        else
        {
            // 启用 FSM
            fsmComponent.enabled = true;

            // 等待短延迟
            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);

            // 发送启用事件
            if (!string.IsNullOrEmpty(onEnableEventName.Value))
            {
                SendEventToTargetFsm(fsmComponent, onEnableEventName.Value);
            }
        }

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

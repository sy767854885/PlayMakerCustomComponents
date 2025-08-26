using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 将物体从当前位置加速移动到目标物体的位置；可选择启动后立即Finish以继续后续逻辑，且可选择离开状态时是否终止Tween。")]
public class DoMoveToGameObjectEaseIn_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("目标物体（会移动到它的位置）")]
    public FsmGameObject targetGameObject;

    [HutongGames.PlayMaker.Tooltip("移动持续时间（秒）")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("缓动类型（Ease）")]
    public Ease easeType = Ease.InQuad;

    [HutongGames.PlayMaker.Tooltip("是否忽略Time.timeScale（使用不受时间缩放影响的更新）")]
    public FsmBool ignoreTimeScale;

    [HutongGames.PlayMaker.Tooltip("是否在开始移动后立即Finish该Action，以便继续执行同一状态后续Action/或尽快允许状态切换")]
    public FsmBool finishImmediately;

    [HutongGames.PlayMaker.Tooltip("离开状态时是否终止Tween（一般为了让移动在状态切换后继续，建议关闭）")]
    public FsmBool killOnExit;

    [HutongGames.PlayMaker.Tooltip("移动完成后触发的事件（可选）")]
    public FsmEvent onCompleteEvent;

    private Tween tween;

    public override void Reset()
    {
        gameObject = null;
        targetGameObject = null;
        duration = 1f;
        easeType = Ease.InQuad;
        ignoreTimeScale = false;
        finishImmediately = true;  // 默认：启动后立即Finish，满足“移动过程中执行后续逻辑”
        killOnExit = false;        // 默认：不杀，保证切走状态也继续移动
        onCompleteEvent = null;
        tween = null;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        var target = targetGameObject.Value;

        if (go == null || target == null)
        {
            Finish();
            return;
        }

        var targetPosition = target.transform.position;

        tween = go.transform.DOMove(targetPosition, duration.Value)
            .SetEase(easeType)
            .SetUpdate(ignoreTimeScale.Value) // true=独立于timeScale
            .OnComplete(() =>
            {
                if (onCompleteEvent != null)
                {
                    // 注意：这里发事件不会“唤醒”已Finish的本Action——它只是给FSM发事件用于状态切换
                    Fsm.Event(onCompleteEvent);
                }
            });

        // 关键：立刻Finish，允许状态继续执行后续Action或尽快响应切换
        if (finishImmediately.Value)
        {
            Finish();
        }
    }

    public override void OnExit()
    {
        // 仅当明确要求时才终止Tween
        if (killOnExit.Value && tween != null && tween.IsActive())
        {
            tween.Kill();
        }
    }
}

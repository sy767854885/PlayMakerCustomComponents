using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 将物体从当前位置移动到指定的目标位置")]
public class DoMoveToPosition_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("移动的目标位置（世界坐标）")]
    public FsmVector3 targetPosition;

    [HutongGames.PlayMaker.Tooltip("移动持续时间（秒）")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("缓动类型（Ease）")]
    public Ease easeType = Ease.Linear;

    [HutongGames.PlayMaker.Tooltip("移动完成后触发的事件（可选）")]
    public FsmEvent onCompleteEvent;

    private Tween tween;

    public override void Reset()
    {
        gameObject = null;
        targetPosition = null;
        duration = 1f;
        easeType = Ease.Linear;
        onCompleteEvent = null;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            Finish();
            return;
        }

        tween = go.transform.DOMove(targetPosition.Value, duration.Value)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
                Finish();
            });
    }

    public override void OnExit()
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill();
        }
    }
}

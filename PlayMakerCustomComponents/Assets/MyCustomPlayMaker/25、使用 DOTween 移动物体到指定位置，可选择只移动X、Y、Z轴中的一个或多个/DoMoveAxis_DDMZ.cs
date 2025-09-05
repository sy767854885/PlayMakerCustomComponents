using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 移动物体到指定位置，可选择只移动X、Y、Z轴中的一个或多个")]
public class DoMoveAxis_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("目标位置（仅勾选的轴会生效）")]
    public FsmVector3 targetPosition;

    [HutongGames.PlayMaker.Tooltip("是否移动X轴")]
    public FsmBool moveX;

    [HutongGames.PlayMaker.Tooltip("是否移动Y轴")]
    public FsmBool moveY;

    [HutongGames.PlayMaker.Tooltip("是否移动Z轴")]
    public FsmBool moveZ;

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
        moveX = false;
        moveY = false;
        moveZ = false;
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

        Vector3 startPos = go.transform.position;
        Vector3 endPos = startPos;

        if (moveX.Value) endPos.x = targetPosition.Value.x;
        if (moveY.Value) endPos.y = targetPosition.Value.y;
        if (moveZ.Value) endPos.z = targetPosition.Value.z;

        tween = go.transform.DOMove(endPos, duration.Value)
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

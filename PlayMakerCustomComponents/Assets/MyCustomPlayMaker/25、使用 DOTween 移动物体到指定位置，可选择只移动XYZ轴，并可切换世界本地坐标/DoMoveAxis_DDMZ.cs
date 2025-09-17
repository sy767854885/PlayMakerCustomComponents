using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 移动物体到指定位置，可选择只移动X/Y/Z轴，并可切换世界/本地坐标")]
public class DoMoveAxis_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("目标位置（仅勾选的轴会生效；若“使用本地坐标”为TRUE，则此为本地坐标）")]
    public FsmVector3 targetPosition;

    [HutongGames.PlayMaker.Tooltip("是否移动X轴")]
    public FsmBool moveX;

    [HutongGames.PlayMaker.Tooltip("是否移动Y轴")]
    public FsmBool moveY;

    [HutongGames.PlayMaker.Tooltip("是否移动Z轴")]
    public FsmBool moveZ;

    [HutongGames.PlayMaker.Tooltip("是否使用本地坐标（TRUE=localPosition/DOLocalMove；FALSE=position/DOMove）")]
    public FsmBool useLocalSpace;

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
        useLocalSpace = false; // 默认世界坐标
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

        // 取起始与目标
        Vector3 startPos = useLocalSpace.Value ? go.transform.localPosition : go.transform.position;
        Vector3 endPos = startPos;

        if (targetPosition != null)
        {
            Vector3 target = targetPosition.Value;
            if (moveX.Value) endPos.x = target.x;
            if (moveY.Value) endPos.y = target.y;
            if (moveZ.Value) endPos.z = target.z;
        }

        // 持续时间<=0时，直接瞬移并触发事件
        if (duration.Value <= 0f)
        {
            if (useLocalSpace.Value) go.transform.localPosition = endPos;
            else go.transform.position = endPos;

            if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
            Finish();
            return;
        }

        // 根据空间模式选择 DOMove / DOLocalMove
        tween = useLocalSpace.Value
            ? go.transform.DOLocalMove(endPos, duration.Value)
            : go.transform.DOMove(endPos, duration.Value);

        tween.SetEase(easeType)
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

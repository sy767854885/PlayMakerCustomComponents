using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 移动物体到指定位置，可选择只移动X/Y/Z轴，并可切换世界/本地坐标。支持按时长或按速度移动。")]
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

    [HutongGames.PlayMaker.Tooltip("按时间移动时使用的持续时间（秒）。当 “Use Speed” 为 FALSE 时生效")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("按速度移动时使用的速度（单位/秒）。当 “Use Speed” 为 TRUE 时生效")]
    public FsmFloat speed;

    [HutongGames.PlayMaker.Tooltip("是否按速度移动（TRUE=按 speed 移动；FALSE=按 duration 移动）")]
    public FsmBool useSpeed;

    [HutongGames.PlayMaker.Tooltip("缓动类型（Ease）")]
    public Ease easeType = Ease.Linear;

    [HutongGames.PlayMaker.Tooltip("移动完成后触发的事件（可选）")]
    public FsmEvent onCompleteEvent;

    private Tween tween;

    public override void Reset()
    {
        gameObject = null;
        targetPosition = null;
        moveX = true;
        moveY = true;
        moveZ = true;
        useLocalSpace = false;
        duration = 1f;
        speed = 1f;
        useSpeed = false;
        easeType = Ease.Linear;
        onCompleteEvent = null;
        tween = null;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            Finish();
            return;
        }

        // 如果没有选中任何轴，则认为没有移动
        if ((moveX == null || !moveX.Value) &&
            (moveY == null || !moveY.Value) &&
            (moveZ == null || !moveZ.Value))
        {
            // nothing to move
            if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
            Finish();
            return;
        }

        Vector3 startPos = useLocalSpace != null && useLocalSpace.Value ? go.transform.localPosition : go.transform.position;
        Vector3 endPos = startPos;

        if (targetPosition != null)
        {
            Vector3 target = targetPosition.Value;
            if (moveX != null && moveX.Value) endPos.x = target.x;
            if (moveY != null && moveY.Value) endPos.y = target.y;
            if (moveZ != null && moveZ.Value) endPos.z = target.z;
        }

        // 如果目标位置与起始位置相同（在选中的轴上），直接完成
        Vector3 diff = endPos - startPos;
        float movedDistance = 0f;
        if (moveX != null && moveX.Value) movedDistance += Mathf.Abs(diff.x);
        if (moveY != null && moveY.Value) movedDistance += Mathf.Abs(diff.y);
        if (moveZ != null && moveZ.Value) movedDistance += Mathf.Abs(diff.z);

        if (Mathf.Approximately(movedDistance, 0f))
        {
            if (useLocalSpace != null && useLocalSpace.Value) go.transform.localPosition = endPos;
            else go.transform.position = endPos;

            if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
            Finish();
            return;
        }

        bool speedBased = useSpeed != null && useSpeed.Value;

        // 处理瞬移或无效参数
        if (!speedBased)
        {
            if (duration == null || duration.Value <= 0f)
            {
                // 瞬移
                if (useLocalSpace != null && useLocalSpace.Value) go.transform.localPosition = endPos;
                else go.transform.position = endPos;

                if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
                Finish();
                return;
            }
        }
        else
        {
            if (speed == null || speed.Value <= 0f)
            {
                // 无效速度，直接结束（不做移动）
                Finish();
                return;
            }
        }

        float tweenParam = speedBased ? speed.Value : duration.Value;

        // 创建 tween：DOMove / DOLocalMove 接受的第二个参数会被 SetSpeedBased(true) 时解释为 speed（units/sec）
        tween = (useLocalSpace != null && useLocalSpace.Value)
            ? go.transform.DOLocalMove(endPos, tweenParam)
            : go.transform.DOMove(endPos, tweenParam);

        if (tween != null)
        {
            if (speedBased) tween.SetSpeedBased(true);
            tween.SetEase(easeType)
                 .OnComplete(() =>
                 {
                     if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
                     Finish();
                 });
        }
        else
        {
            // 创建失败则直接结束
            if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
            Finish();
        }
    }

    public override void OnExit()
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill();
            tween = null;
        }
    }
}

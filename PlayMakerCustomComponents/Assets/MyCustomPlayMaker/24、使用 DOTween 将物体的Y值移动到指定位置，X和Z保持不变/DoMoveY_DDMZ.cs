using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 将物体的Y值移动到指定位置，X和Z保持不变")]
public class DoMoveY_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("目标Y坐标值")]
    public FsmFloat targetY;

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
        targetY = 0f;
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
        Vector3 endPos = new Vector3(startPos.x, targetY.Value, startPos.z);

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

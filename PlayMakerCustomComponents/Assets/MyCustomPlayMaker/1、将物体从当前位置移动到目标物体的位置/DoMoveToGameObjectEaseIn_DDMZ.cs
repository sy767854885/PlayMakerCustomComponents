using UnityEngine;
using HutongGames.PlayMaker;
using DG.Tweening;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("使用 DOTween 将物体从当前位置加速移动到目标物体的位置")]
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

    [HutongGames.PlayMaker.Tooltip("移动完成后触发的事件")]
    public FsmEvent onCompleteEvent;

    [HutongGames.PlayMaker.Tooltip("缓动类型（Ease）")]
    public Ease easeType = Ease.InQuad;

    private Tween tween;

    public override void Reset()
    {
        gameObject = null;
        targetGameObject = null;
        duration = 1f;
        onCompleteEvent = null;
        easeType = Ease.InQuad;
    }

    public override void OnEnter()
    {
        GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);
        GameObject target = targetGameObject.Value;

        if (go == null || target == null)
        {
            Finish();
            return;
        }

        Vector3 targetPosition = target.transform.position;

        tween = go.transform.DOMove(targetPosition, duration.Value)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                if (onCompleteEvent != null)
                {
                    Fsm.Event(onCompleteEvent);
                }
                Finish();
            });
    }

    public override void OnExit()
    {
        // 可选：状态退出时终止 tween
        if (tween != null && tween.IsActive())
        {
            tween.Kill();
        }
    }
}

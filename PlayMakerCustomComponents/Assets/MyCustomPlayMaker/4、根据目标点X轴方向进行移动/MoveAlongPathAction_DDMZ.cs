using System.Collections.Generic;
using DG.Tweening;
using HutongGames.PlayMaker;
using UnityEngine;

[ActionCategory("Custom/Enemy")]
[HutongGames.PlayMaker.Tooltip("沿给定路径（FsmArray<GameObject>）移动。可按时间（duration）或按速度（units/sec）移动。目标被禁用时自动停止，空路径安全退出。")]
public class MoveAlongPathAction_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [UIHint(UIHint.Variable)]
    [ArrayEditor(VariableType.GameObject)]
    [HutongGames.PlayMaker.Tooltip("路径点（FsmArray，元素类型为 GameObject；将取每个元素的 Transform.position）")]
    public FsmArray pathPoints;

    [HutongGames.PlayMaker.Tooltip("移动时长（秒）。当 “Use Speed” 未勾选时生效")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("按速度移动时使用（单位：单位/秒）。当 “Use Speed” 勾选时生效")]
    public FsmFloat speed;

    [HutongGames.PlayMaker.Tooltip("是否按速度（units/sec）移动；否则按 duration（秒）移动")]
    public FsmBool useSpeed;

    [HutongGames.PlayMaker.Tooltip("移动完成后触发的事件（可选）")]
    public FsmEvent onCompleteEvent;

    [HutongGames.PlayMaker.Tooltip("当目标物体被 SetActive(false) 时，是否停止 Tween")]
    public FsmBool stopWhenOwnerDisabled;

    private GameObject ownerGo;
    private Transform body;
    private Tween tween;

    public override void Reset()
    {
        gameObject = null;
        pathPoints = null;      // 在 FSM 里新建一个 FsmArray（元素类型设为 GameObject）
        duration = 3f;
        speed = 1f;
        useSpeed = false;
        onCompleteEvent = null;
        stopWhenOwnerDisabled = true;
        tween = null;
        body = null;
        ownerGo = null;
    }

    public override void OnEnter()
    {
        ownerGo = Fsm.GetOwnerDefaultTarget(gameObject);
        if (ownerGo == null) { Finish(); return; }
        body = ownerGo.transform;

        // —— 安全收集有效路径点 —— //
        var list = new List<Vector3>();

        if (pathPoints != null && pathPoints.Length > 0)
        {
            for (int i = 0; i < pathPoints.Length; i++)
            {
                var obj = pathPoints.Get(i) as GameObject; // FsmArray 元素以 object 存取
                if (obj != null)
                {
                    list.Add(obj.transform.position);
                }
            }
        }

        // 路径点不足（0 或 1 个）时，不执行 Tween，直接结束
        if (list.Count < 2) { Finish(); return; }

        // 选择使用速度还是时间
        float tweenParam;
        bool speedBased = useSpeed != null && useSpeed.Value;
        if (speedBased)
        {
            if (speed == null || speed.Value <= 0f)
            {
                // 无效速度，直接退出
                Finish();
                return;
            }
            tweenParam = speed.Value;
        }
        else
        {
            if (duration == null || duration.Value <= 0f)
            {
                // 无效时长，直接退出
                Finish();
                return;
            }
            tweenParam = duration.Value;
        }

        // 创建 DOPath：当 speedBased 为 true 时，传入的 tweenParam 会被 DOTween 解释为“速度单位/秒”
        tween = body.DOPath(
                    list.ToArray(),
                    tweenParam,
                    PathType.CatmullRom,
                    PathMode.TopDown2D)
                .SetEase(Ease.Linear)
                .SetLookAt(0.01f)
                .OnComplete(() =>
                {
                    if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
                    Finish();
                });

        if (speedBased)
        {
            tween.SetSpeedBased(true); // 把传入的“时间参数”解释为速度（units/sec）
        }
    }

    public override void OnUpdate()
    {
        if (stopWhenOwnerDisabled.Value && (ownerGo == null || !ownerGo.activeInHierarchy))
        {
            KillTween();
            Finish();
        }
    }

    public override void OnExit() => KillTween();

    private void KillTween()
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill();
            tween = null;
        }
    }
}

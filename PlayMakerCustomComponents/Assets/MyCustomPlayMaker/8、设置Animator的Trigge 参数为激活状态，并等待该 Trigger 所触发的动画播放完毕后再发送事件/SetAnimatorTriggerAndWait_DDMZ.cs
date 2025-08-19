// (c) Copyright HutongGames, LLC 2010-2016. All rights reserved.

using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

[ActionCategory(ActionCategory.Animator)]
[HutongGames.PlayMaker.Tooltip("设置 Animator 的 Trigger 参数为激活状态，并等待该 Trigger 所触发的动画播放完毕后再发送事件。")]
public class SetAnimatorTriggerAndWait_DDMZ : ComponentAction<Animator>
{
    [RequiredField]
    [CheckForComponent(typeof(Animator))]
    [HutongGames.PlayMaker.Tooltip("包含 Animator 组件的 GameObject")]
    public FsmOwnerDefault gameObject;   // 要操作的目标对象（必须有 Animator 组件）

    [RequiredField]
    [UIHint(UIHint.AnimatorTrigger)]
    [HutongGames.PlayMaker.Tooltip("Animator 中的 Trigger 参数名")]
    public FsmString trigger;            // 触发的 Trigger 参数名

    [HutongGames.PlayMaker.Tooltip("可选：Animator 的层索引（默认0层）")]
    public FsmInt layer;                 // 动画层（默认为0）

    [HutongGames.PlayMaker.Tooltip("Trigger 会进入的目标动画状态名（必须完全匹配）。")]
    public FsmString targetStateName;    // 目标动画状态名（要与 Animator 状态机里的名字一致）

    [HutongGames.PlayMaker.Tooltip("当动画播放结束时要触发的事件。")]
    public FsmEvent onAnimationFinished; // 动画播放完毕时触发的事件

    private Animator animator;           // 缓存 Animator 组件


    /// <summary>
    /// 初始化参数
    /// </summary>
    public override void Reset()
    {
        gameObject = null;
        trigger = null;
        layer = 0;
        targetStateName = "";
        onAnimationFinished = null;
    }

    /// <summary>
    /// 进入状态时调用一次
    /// </summary>
    public override void OnEnter()
    {
        // 获取目标对象
        GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);

        // 如果目标无效，直接结束
        if (!UpdateCache(go))
        {
            Finish();
            return;
        }

        // 缓存 Animator 组件
        animator = cachedComponent;

        // 设置 Trigger，触发动画
        animator.SetTrigger(trigger.Value);
    }

    /// <summary>
    /// 每帧更新时调用
    /// </summary>
    public override void OnUpdate()
    {
        if (animator == null) return;

        // 获取当前层的动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer.Value);

        // 检查条件：
        // 1. 当前状态名与目标状态名相同
        // 2. normalizedTime >= 1.0f 表示动画播放到末尾
        // 3. 当前不处于过渡状态
        if (stateInfo.IsName(targetStateName.Value) &&
            stateInfo.normalizedTime >= 1.0f &&
            !animator.IsInTransition(layer.Value))
        {
            // 触发完成事件
            Fsm.Event(onAnimationFinished);

            // 结束 Action
            Finish();
        }
    }
}

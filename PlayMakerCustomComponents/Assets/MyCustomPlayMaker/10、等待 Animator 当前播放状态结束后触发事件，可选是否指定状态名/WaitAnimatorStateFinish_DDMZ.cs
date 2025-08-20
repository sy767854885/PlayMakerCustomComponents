using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Animator")]
[HutongGames.PlayMaker.Tooltip("等待 Animator 当前播放状态结束后触发事件，可选是否指定状态名")]
public class WaitAnimatorStateFinish_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("包含 Animator 的 GameObject")]
    public FsmOwnerDefault gameObject; // 目标对象（必须带有 Animator 组件）

    [HutongGames.PlayMaker.Tooltip("Animator 层索引，默认 0")]
    public FsmInt layer; // 动画层索引，默认使用第0层

    [HutongGames.PlayMaker.Tooltip("是否指定状态名进行检测")]
    public FsmBool matchStateName; // 是否限定检查某个指定的动画状态名

    [HutongGames.PlayMaker.Tooltip("状态名（例如 'Attack'，仅在 matchStateName 为 true 时使用）")]
    public FsmString stateName; // 需要检测的状态名（只有 matchStateName 为 true 时才会生效）

    [HutongGames.PlayMaker.Tooltip("动画播放完后触发的事件")]
    public FsmEvent onAnimationFinished; // 动画播放结束后要触发的事件

    private Animator animator; // 缓存目标对象上的 Animator 组件

    // 初始化变量（当动作被重置时调用）
    public override void Reset()
    {
        gameObject = null;        // 目标对象为空
        layer = 0;                // 默认检查第0层动画
        matchStateName = false;   // 默认不检查特定状态名
        stateName = "";           // 状态名默认为空
        onAnimationFinished = null; // 默认没有触发事件
    }

    // 当进入该动作时调用
    public override void OnEnter()
    {
        // 获取绑定的目标对象
        GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            // 如果为空，则直接结束该 Action
            Finish();
            return;
        }

        // 尝试获取 Animator 组件
        animator = go.GetComponent<Animator>();
        if (animator == null)
        {
            // 如果没找到 Animator，输出警告，并结束该 Action
            Debug.LogWarning("缺少 Animator 组件，目标物体: " + go.name);
            Finish();
            return;
        }
    }

    // 每一帧都会执行（检测动画状态是否结束）
    public override void OnUpdate()
    {
        // 获取指定层的动画状态信息
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer.Value);

        // 默认认为当前是目标状态
        bool isTarget = true;

        // 如果要求匹配特定状态名，则进行检测
        if (matchStateName.Value)
        {
            // 检查当前状态是否和指定的状态名一致
            isTarget = stateInfo.IsName(stateName.Value);
        }

        // 条件判断：
        // 1. 当前动画是目标状态（匹配状态名或无条件匹配）
        // 2. 动画 normalizedTime >= 1.0 表示动画播放完一遍（normalizedTime 0-1 代表一次播放的进度）
        // 3. 不处于状态切换中（防止过渡动画干扰判断）
        if (isTarget && stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(layer.Value))
        {
            // 触发 PlayMaker 中设置的事件
            Fsm.Event(onAnimationFinished);

            // 本 Action 执行完成
            Finish();
        }
    }
}

using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Combat")]
[HutongGames.PlayMaker.Tooltip("处理一次伤害：若当前生命值未初始化则等于最大生命值，然后扣除伤害；当HP<=阈值时触发事件。")]
public class HpDamageAndThreshold_DDMZ : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("当前生命值（FSM变量，会被修改）。不要直接填数字，请绑定到Variables里的Float变量。")]
    public FsmFloat currentHP;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("最大生命值。")]
    public FsmFloat maxHP;

    [HutongGames.PlayMaker.Tooltip("本次要扣除的伤害值。")]
    public FsmFloat damage;

    [HutongGames.PlayMaker.Tooltip("执行事件的阈值：当当前生命值<=该值时触发事件。")]
    public FsmFloat threshold;

    [HutongGames.PlayMaker.Tooltip("当生命值低于或等于阈值时触发的事件。")]
    public FsmEvent onThresholdReached;

    [HutongGames.PlayMaker.Tooltip("是否把HP限制在[0, MaxHP]范围内。")]
    public FsmBool clampToRange;

    [HutongGames.PlayMaker.Tooltip("如果当前生命值为0（如未初始化），是否在本次执行前用最大生命值初始化。")]
    public FsmBool initWithMaxIfZero;

    public override void Reset()
    {
        currentHP = null;          // 必须绑定变量
        maxHP = 100f;
        damage = 0f;
        threshold = 0f;
        onThresholdReached = null;
        clampToRange = true;
        initWithMaxIfZero = true;
    }

    public override void OnEnter()
    {
        // 初始化（仅在变量当前值为0且勾选时）
        if (initWithMaxIfZero.Value && Mathf.Approximately(currentHP.Value, 0f))
        {
            currentHP.Value = maxHP.Value;
        }

        // 扣血
        float hp = currentHP.Value - Mathf.Max(0f, damage.Value);

        // 夹紧
        if (clampToRange.Value)
        {
            hp = Mathf.Clamp(hp, 0f, maxHP.Value);
        }

        // 写回FSM变量（到Variables面板能看到变化）
        currentHP.Value = hp;

        // 阈值判断
        if (hp <= threshold.Value && onThresholdReached != null)
        {
            Fsm.Event(onThresholdReached);
        }

        Finish();
    }
}
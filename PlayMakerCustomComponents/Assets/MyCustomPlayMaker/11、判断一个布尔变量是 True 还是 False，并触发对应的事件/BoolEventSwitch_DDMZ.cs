using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Logic")]
[HutongGames.PlayMaker.Tooltip("判断一个布尔变量是 True 还是 False，并触发对应的事件")]
public class BoolEventSwitch_DDMZ : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("要检测的布尔变量")]
    public FsmBool boolVariable;  // 要检测的布尔变量

    [HutongGames.PlayMaker.Tooltip("当布尔变量为 True 时触发的事件")]
    public FsmEvent trueEvent;    // 变量为 true 时执行的事件

    [HutongGames.PlayMaker.Tooltip("当布尔变量为 False 时触发的事件")]
    public FsmEvent falseEvent;   // 变量为 false 时执行的事件

    [HutongGames.PlayMaker.Tooltip("是否每一帧都检测，勾选后会实时检测")]
    public bool everyFrame;       // 是否每帧检测

    // 重置默认值
    public override void Reset()
    {
        boolVariable = null;
        trueEvent = null;
        falseEvent = null;
        everyFrame = false;
    }

    // 进入该 Action 时调用
    public override void OnEnter()
    {
        DoCheck();

        // 如果不是每帧检测，则执行一次后直接结束
        if (!everyFrame)
        {
            Finish();
        }
    }

    // 每帧检测（如果勾选了 everyFrame）
    public override void OnUpdate()
    {
        DoCheck();
    }

    // 判断逻辑
    private void DoCheck()
    {
        if (boolVariable.Value)
        {
            // 如果布尔变量为 true，触发 trueEvent
            Fsm.Event(trueEvent);
        }
        else
        {
            // 如果布尔变量为 false，触发 falseEvent
            Fsm.Event(falseEvent);
        }
    }
}

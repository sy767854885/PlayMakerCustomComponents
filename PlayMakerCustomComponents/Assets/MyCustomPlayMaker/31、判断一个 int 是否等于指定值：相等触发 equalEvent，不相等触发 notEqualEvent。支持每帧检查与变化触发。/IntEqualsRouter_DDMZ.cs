using HutongGames.PlayMaker;
using UnityEngine;

[ActionCategory("Custom/Logic")]
[HutongGames.PlayMaker.Tooltip("判断一个 int 是否等于指定值：相等触发 equalEvent，不相等触发 notEqualEvent。支持每帧检查与变化触发。")]
public class IntEqualsRouter_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要判断的 int 变量")]
    public FsmInt value;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("用来比较的目标值")]
    public FsmInt target;

    [HutongGames.PlayMaker.Tooltip("相等时要触发的事件（可选）")]
    public FsmEvent equalEvent;

    [HutongGames.PlayMaker.Tooltip("不相等时要触发的事件（可选）")]
    public FsmEvent notEqualEvent;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("把比较结果（是否相等）存到一个 Bool 变量里（可选）")]
    public FsmBool storeIsEqual;

    [HutongGames.PlayMaker.Tooltip("是否每帧检查")]
    public FsmBool everyFrame;

    [HutongGames.PlayMaker.Tooltip("仅在结果发生变化时才发送事件，避免每帧重复触发")]
    public FsmBool onlyOnChange;

    private bool _hasLast;
    private bool _lastResult;

    public override void Reset()
    {
        value = 0;
        target = 0;
        equalEvent = null;
        notEqualEvent = null;
        storeIsEqual = new FsmBool { UseVariable = true };
        everyFrame = false;
        onlyOnChange = true;   // 默认：结果变化才触发
        _hasLast = false;
        _lastResult = false;
    }

    public override void OnEnter()
    {
        DoCheck();
        if (!everyFrame.Value) Finish();
    }

    public override void OnUpdate()
    {
        DoCheck();
    }

    private void DoCheck()
    {
        bool isEqual = value.Value == target.Value;

        if (!storeIsEqual.IsNone)
            storeIsEqual.Value = isEqual;

        // 仅在变化时触发
        if (onlyOnChange.Value)
        {
            if (!_hasLast || isEqual != _lastResult)
            {
                Fire(isEqual);
                _lastResult = isEqual;
                _hasLast = true;
            }
        }
        else
        {
            // 每次检查都触发
            Fire(isEqual);
            _lastResult = isEqual;
            _hasLast = true;
        }
    }

    private void Fire(bool isEqual)
    {
        if (isEqual)
        {
            if (equalEvent != null) Fsm.Event(equalEvent);
        }
        else
        {
            if (notEqualEvent != null) Fsm.Event(notEqualEvent);
        }
    }
}

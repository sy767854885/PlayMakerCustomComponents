using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Math")]
[HutongGames.PlayMaker.Tooltip("判断一个 Vector2 的 X/Y 是否在指定范围内，触发对应事件")]
public class Vector2CheckRange_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要检查的 Vector2")]
    public FsmVector2 vector;

    [HutongGames.PlayMaker.Tooltip("X 轴最小值")]
    public FsmFloat minX;

    [HutongGames.PlayMaker.Tooltip("X 轴最大值")]
    public FsmFloat maxX;

    [HutongGames.PlayMaker.Tooltip("Y 轴最小值")]
    public FsmFloat minY;

    [HutongGames.PlayMaker.Tooltip("Y 轴最大值")]
    public FsmFloat maxY;

    [HutongGames.PlayMaker.Tooltip("如果在范围内，触发的事件")]
    public FsmEvent inRangeEvent;

    [HutongGames.PlayMaker.Tooltip("如果不在范围内，触发的事件")]
    public FsmEvent outOfRangeEvent;

    [HutongGames.PlayMaker.Tooltip("是否每帧执行检查")]
    public bool everyFrame;

    public override void Reset()
    {
        vector = null;
        minX = -1f;
        maxX = 1f;
        minY = -1f;
        maxY = 1f;
        inRangeEvent = null;
        outOfRangeEvent = null;
        everyFrame = false;
    }

    public override void OnEnter()
    {
        DoCheck();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoCheck();
    }

    private void DoCheck()
    {
        if (vector == null) return;

        var v = vector.Value;

        bool inRange =
            v.x >= minX.Value && v.x <= maxX.Value &&
            v.y >= minY.Value && v.y <= maxY.Value;

        if (inRange && inRangeEvent != null)
        {
            Fsm.Event(inRangeEvent);
        }
        else if (!inRange && outOfRangeEvent != null)
        {
            Fsm.Event(outOfRangeEvent);
        }
    }
}

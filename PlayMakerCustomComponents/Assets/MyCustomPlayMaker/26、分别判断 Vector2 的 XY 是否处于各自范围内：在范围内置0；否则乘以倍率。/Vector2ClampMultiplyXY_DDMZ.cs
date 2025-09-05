using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Math")]
[HutongGames.PlayMaker.Tooltip("分别判断 Vector2 的 X/Y 是否处于各自范围内：在范围内置0；否则乘以倍率。")]
public class Vector2ClampMultiplyXY_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("输入的 Vector2 变量")]
    public FsmVector2 vector2Variable;

    // —— X 轴配置 —— //
    [HutongGames.PlayMaker.Tooltip("是否处理 X 轴")]
    public FsmBool affectX;

    [HutongGames.PlayMaker.Tooltip("X 轴最小范围")]
    public FsmFloat minRangeX;

    [HutongGames.PlayMaker.Tooltip("X 轴最大范围")]
    public FsmFloat maxRangeX;

    // —— Y 轴配置 —— //
    [HutongGames.PlayMaker.Tooltip("是否处理 Y 轴")]
    public FsmBool affectY;

    [HutongGames.PlayMaker.Tooltip("Y 轴最小范围")]
    public FsmFloat minRangeY;

    [HutongGames.PlayMaker.Tooltip("Y 轴最大范围")]
    public FsmFloat maxRangeY;

    // 倍率（共用一个，如需分别设置可再扩展 multiplyX / multiplyY）
    [HutongGames.PlayMaker.Tooltip("超出各自范围时所乘的倍率")]
    public FsmFloat multiplyBy;

    [HutongGames.PlayMaker.Tooltip("输出结果 Vector2")]
    [UIHint(UIHint.Variable)]
    public FsmVector2 storeResult;

    [HutongGames.PlayMaker.Tooltip("是否每帧执行")]
    public bool everyFrame;

    public override void Reset()
    {
        vector2Variable = null;

        affectX = true;
        minRangeX = -1f;
        maxRangeX = 1f;

        affectY = false;
        minRangeY = -1f;
        maxRangeY = 1f;

        multiplyBy = 1f;
        storeResult = null;
        everyFrame = false;
    }

    public override void OnEnter()
    {
        Process();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        Process();
    }

    private void Process()
    {
        if (vector2Variable == null) return;

        var v = vector2Variable.Value;

        if (affectX.Value)
        {
            if (v.x >= minRangeX.Value && v.x <= maxRangeX.Value)
                v.x = 0f;
            else
                v.x *= multiplyBy.Value;
        }

        if (affectY.Value)
        {
            if (v.y >= minRangeY.Value && v.y <= maxRangeY.Value)
                v.y = 0f;
            else
                v.y *= multiplyBy.Value;
        }

        storeResult.Value = v;
    }
}

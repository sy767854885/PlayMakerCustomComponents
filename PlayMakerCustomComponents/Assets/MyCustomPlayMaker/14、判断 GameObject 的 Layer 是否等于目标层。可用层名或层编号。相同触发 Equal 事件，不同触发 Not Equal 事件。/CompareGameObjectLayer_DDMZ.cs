using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Layer")]
[HutongGames.PlayMaker.Tooltip("判断 GameObject 的 Layer 是否等于目标层。可用层名或层编号。相同触发 Equal 事件，不同触发 Not Equal 事件。")]
public class CompareGameObjectLayer_DDMZ : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("要判断的对象。留空则使用 Owner。")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("目标层编号（0-31）。如果同时填写了 Layer Name，则以 Layer Name 为准。")]
    public FsmInt targetLayer;

    [HutongGames.PlayMaker.Tooltip("目标层名称（如 'Default'、'UI'）。优先级高于编号。")]
    public FsmString targetLayerName;

    [HutongGames.PlayMaker.Tooltip("是否每帧检测。")]
    public bool everyFrame;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("结果布尔（可选）。true 表示与目标层相同。")]
    public FsmBool storeResult;

    [HutongGames.PlayMaker.Tooltip("当层相同触发的事件。")]
    public FsmEvent equalEvent;

    [HutongGames.PlayMaker.Tooltip("当层不同触发的事件。")]
    public FsmEvent notEqualEvent;

    public override void Reset()
    {
        gameObject = null;
        targetLayer = 0;
        targetLayerName = null;
        everyFrame = false;
        storeResult = null;
        equalEvent = null;
        notEqualEvent = null;
    }

    public override void OnEnter()
    {
        DoCompare();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoCompare();
    }

    private void DoCompare()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            SetResult(false);
            return;
        }

        int target = ResolveTargetLayer();
        bool isEqual = go.layer == target;
        SetResult(isEqual);
    }

    private int ResolveTargetLayer()
    {
        if (!string.IsNullOrEmpty(targetLayerName.Value))
        {
            int byName = LayerMask.NameToLayer(targetLayerName.Value);
            if (byName >= 0) return byName;

            LogWarning($"Layer Name '{targetLayerName.Value}' 解析失败，已回退到编号 {targetLayer.Value}");
        }
        return Mathf.Clamp(targetLayer.Value, 0, 31);
    }

    private void SetResult(bool equal)
    {
        if (!storeResult.IsNone) storeResult.Value = equal;

        if (equal)
        {
            if (equalEvent != null) Fsm.Event(equalEvent);
        }
        else
        {
            if (notEqualEvent != null) Fsm.Event(notEqualEvent);
        }
    }
}

using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Component")]
[HutongGames.PlayMaker.Tooltip("批量启用或禁用 FsmArray(GameObject) 中的 PolygonCollider2D 组件。")]
public class SetPolygonCollider2DEnabledArray_DDMZ : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("FSM 中的 FsmArray（元素类型需为 GameObject）")]
    [ArrayEditor(VariableType.GameObject)]
    public FsmArray targetObjects;

    [HutongGames.PlayMaker.Tooltip("是否处理子物体（包含未激活对象）")]
    public FsmBool includeChildren;

    [HutongGames.PlayMaker.Tooltip("将 PolygonCollider2D 置为启用(true)或禁用(false)")]
    public FsmBool enable;

    [HutongGames.PlayMaker.Tooltip("是否每帧执行")]
    public FsmBool everyFrame;

    [UIHint(UIHint.Variable)] public FsmInt processedCount;
    [UIHint(UIHint.Variable)] public FsmInt notFoundCount;

    public override void Reset()
    {
        targetObjects = null;
        includeChildren = false;
        enable = true;
        everyFrame = false;
        processedCount = null;
        notFoundCount = null;
    }

    public override void OnEnter()
    {
        DoWork();
        if (!everyFrame.Value) Finish();
    }

    public override void OnUpdate()
    {
        if (everyFrame.Value) DoWork();
    }

    private void DoWork()
    {
        int processed = 0;
        int notFound = 0;

        if (targetObjects == null || targetObjects.Length == 0) return;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            var go = targetObjects.Get(i) as GameObject;
            if (go == null) continue;

            if (includeChildren.Value)
            {
                var comps = go.GetComponentsInChildren<PolygonCollider2D>(true);
                if (comps != null && comps.Length > 0)
                {
                    foreach (var c in comps) { c.enabled = enable.Value; processed++; }
                }
                else notFound++;
            }
            else
            {
                var comp = go.GetComponent<PolygonCollider2D>();
                if (comp != null) { comp.enabled = enable.Value; processed++; }
                else notFound++;
            }
        }

        if (processedCount != null) processedCount.Value = processed;
        if (notFoundCount != null) notFoundCount.Value = notFound;
    }
}

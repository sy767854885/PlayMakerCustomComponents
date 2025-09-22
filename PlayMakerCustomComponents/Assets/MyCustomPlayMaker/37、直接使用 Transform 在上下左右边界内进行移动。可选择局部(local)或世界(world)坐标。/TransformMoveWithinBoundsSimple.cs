using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("直接使用 Transform 在上下左右边界内进行移动。可选择局部(local)或世界(world)坐标。")]
public class TransformMoveWithinBoundsSimple : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的对象")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("移动方向（归一化向量更佳）。当 UseLocalSpace 为 true 时，此向量被视为本地方向。")]
    public FsmVector2 direction;

    [HutongGames.PlayMaker.Tooltip("移动速度（单位/秒）")]
    public FsmFloat speed;

    [HutongGames.PlayMaker.Tooltip("左边界 X（如果 UseLocalSpace 为 true，则为本地 X，否则为世界 X）")]
    public FsmFloat left;

    [HutongGames.PlayMaker.Tooltip("右边界 X（如果 UseLocalSpace 为 true，则为本地 X，否则为世界 X）")]
    public FsmFloat right;

    [HutongGames.PlayMaker.Tooltip("下边界 Y（如果 UseLocalSpace 为 true，则为本地 Y，否则为世界 Y）")]
    public FsmFloat bottom;

    [HutongGames.PlayMaker.Tooltip("上边界 Y（如果 UseLocalSpace 为 true，则为本地 Y，否则为世界 Y）")]
    public FsmFloat top;

    [HutongGames.PlayMaker.Tooltip("勾选后以本地坐标移动/约束（适用于父物体会移动的情况）；不勾则以世界坐标移动/约束")]
    public FsmBool useLocalSpace;

    private Transform trans;

    public override void Reset()
    {
        gameObject = null;
        direction = new FsmVector2 { UseVariable = false, Value = Vector2.zero };
        speed = 5f;
        left = new FsmFloat { UseVariable = false, Value = -5f };
        right = new FsmFloat { UseVariable = false, Value = 5f };
        bottom = new FsmFloat { UseVariable = false, Value = -3f };
        top = new FsmFloat { UseVariable = false, Value = 3f };
        useLocalSpace = new FsmBool { UseVariable = false, Value = false };
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go != null)
        {
            trans = go.transform;
        }
    }

    public override void OnUpdate()
    {
        if (trans == null) return;

        // 方向（保留 0 向量的行为）
        Vector2 dir2 = direction.IsNone ? Vector2.zero : direction.Value;
        Vector2 dirNormalized = dir2.sqrMagnitude > 1e-6f ? dir2.normalized : Vector2.zero;

        float dt = Time.deltaTime;
        Vector3 move = new Vector3(dirNormalized.x, dirNormalized.y, 0f) * (speed.IsNone ? 0f : speed.Value) * dt;

        if (useLocalSpace != null && useLocalSpace.Value)
        {
            // 本地坐标移动：把 move 当作本地增量直接加到 localPosition
            Vector3 newLocalPos = trans.localPosition + move;

            // 在本地坐标系中做边界限制（left/right/top/bottom 应以父物体的坐标系为基准输入）
            newLocalPos.x = Mathf.Clamp(newLocalPos.x, left.IsNone ? newLocalPos.x : left.Value, right.IsNone ? newLocalPos.x : right.Value);
            newLocalPos.y = Mathf.Clamp(newLocalPos.y, bottom.IsNone ? newLocalPos.y : bottom.Value, top.IsNone ? newLocalPos.y : top.Value);

            trans.localPosition = newLocalPos;
        }
        else
        {
            // 世界坐标移动：把 move 当作世界增量
            Vector3 newWorldPos = trans.position + move;

            // 在世界坐标系中做边界限制
            newWorldPos.x = Mathf.Clamp(newWorldPos.x, left.IsNone ? newWorldPos.x : left.Value, right.IsNone ? newWorldPos.x : right.Value);
            newWorldPos.y = Mathf.Clamp(newWorldPos.y, bottom.IsNone ? newWorldPos.y : bottom.Value, top.IsNone ? newWorldPos.y : top.Value);

            trans.position = newWorldPos;
        }
    }
}

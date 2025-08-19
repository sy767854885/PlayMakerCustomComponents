using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;


[ActionCategory(ActionCategory.Physics2D)]
[HutongGames.PlayMaker.Tooltip("给2D刚体施加相对力（相对自身或指定物体的方向）。可以使用Vector2或Vector3参数来指定力。")]
public class AddRelativeForce2d_DDMZ : ComponentAction<Rigidbody2D>
{
    [HutongGames.PlayMaker.Tooltip("使用另一个GameObject的方向代替自身的局部方向（通常取该物体的Transform.up方向）。")]
    public FsmGameObject directionSource;

    [RequiredField]
    [CheckForComponent(typeof(Rigidbody2D))]
    [HutongGames.PlayMaker.Tooltip("要施加力的目标GameObject（必须有Rigidbody2D组件）。")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("施力方式（Force = 连续作用的力，Impulse = 瞬时冲量）。")]
    public ForceMode2D forceMode;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("施加的二维力。可以直接设置Vector2值，也可以使用下面的X、Y覆盖。")]
    public FsmVector2 vector;

    [HutongGames.PlayMaker.Tooltip("沿X轴的力。如果设置为None，则保持不变。")]
    public FsmFloat x;

    [HutongGames.PlayMaker.Tooltip("沿Y轴的力。如果设置为None，则保持不变。")]
    public FsmFloat y;

    [HutongGames.PlayMaker.Tooltip("一个三维向量形式的力，z值会被忽略，仅使用x、y。")]
    public FsmVector3 vector3;

    [HutongGames.PlayMaker.Tooltip("是否在每一帧都重复施加该力。")]
    public bool everyFrame;


    public override void Reset()
    {
        gameObject = null;
        forceMode = ForceMode2D.Force; // 默认使用持续力
        vector = null;
        vector3 = new FsmVector3 { UseVariable = true };

        // 默认设置为可选变量（不强制赋值）
        x = new FsmFloat { UseVariable = true };
        y = new FsmFloat { UseVariable = true };

        everyFrame = false;
    }

    public override void OnPreprocess()
    {
        // 物理计算在FixedUpdate中执行
        Fsm.HandleFixedUpdate = true;
    }

    public override void OnEnter()
    {
        // 进入状态时执行一次
        DoAddRelativeForce();

        // 如果不是每帧执行，则完成动作
        if (!everyFrame)
        {
            Finish();
        }
    }

    public override void OnFixedUpdate()
    {
        // 每次物理更新时执行
        DoAddRelativeForce();
    }

    /// <summary>
    /// 实际执行施力的方法
    /// </summary>
    void DoAddRelativeForce()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (!UpdateCache(go))
        {
            return;
        }

        // 计算最终的力
        var force = vector.IsNone ? new Vector2(x.Value, y.Value) : vector.Value;

        // 如果设置了vector3，则用其x、y覆盖
        if (!vector3.IsNone)
        {
            force.x = vector3.Value.x;
            force.y = vector3.Value.y;
        }

        // 如果单独设置了x或y，则继续覆盖
        if (!x.IsNone) force.x = x.Value;
        if (!y.IsNone) force.y = y.Value;

        // ========== 施力逻辑 ==========
        // 如果设置了directionSource，则用它的方向
        if (directionSource != null && directionSource.Value != null)
        {
            // 使用directionSource的朝上方向（通常是Transform.up）
            Vector2 direction = directionSource.Value.transform.up.normalized;

            // 按照force的模长施加力，方向取自direction
            rigidbody2d.AddForce(direction * force.magnitude, forceMode);
        }
        else
        {
            // 默认情况下，使用自身的局部方向
            rigidbody2d.AddRelativeForce(force, forceMode);
        }
    }
}
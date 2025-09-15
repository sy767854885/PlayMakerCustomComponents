using System;
using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("在2D空间内进行无限弹射移动：碰到边界（Layer筛选）后，按反射向量并加入随机角度抖动继续移动，带防贴边与角落卡死处理。")]
public class BounceWithinBounds2D_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体（若不指定则为FSM宿主）。需要 Rigidbody2D（Dynamic），Gravity Scale=0。")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("用于识别“边界”的 Layer。只在与这些层发生碰撞/触发时才执行反弹。")]
    public FsmInt layerMask;

    [HutongGames.PlayMaker.Tooltip("是否响应触发器（Collider2D.isTrigger）。")]
    public FsmBool includeTriggers;

    [HutongGames.PlayMaker.Tooltip("进入状态时是否随机一个初始方向。")]
    public FsmBool randomizeInitialDirection;

    [HutongGames.PlayMaker.Tooltip("进入状态时的初速度（若为0则使用保持速度Speed）。")]
    public FsmFloat initialSpeed;

    [HutongGames.PlayMaker.Tooltip("持续保持的目标速度（每帧会把速度拉向这个值）。")]
    public FsmFloat speed;

    [HutongGames.PlayMaker.Tooltip("允许的最小速度（低于此值会被抬到该值）。")]
    public FsmFloat minSpeed;

    [HutongGames.PlayMaker.Tooltip("允许的最大速度（高于此值会被压到该值；<=0表示不限制）。")]
    public FsmFloat maxSpeed;

    [HutongGames.PlayMaker.Tooltip("每次反弹时在反射向量两侧增加的随机角度范围（度）。0~30 常用。")]
    public FsmFloat jitterAngleDeg;

    [HutongGames.PlayMaker.Tooltip("与碰撞法线的最小夹角（度），避免与边界几乎平行导致贴边滑行。建议 5~15 度。")]
    public FsmFloat minAngleFromNormalDeg;

    [HutongGames.PlayMaker.Tooltip("碰撞瞬间沿新方向推进的距离（米），帮助物体脱离边界。建议 0.02~0.2。")]
    public FsmFloat nudgeDistance;

    [HutongGames.PlayMaker.Tooltip("碰撞后的防抖时间（秒），在该时间内忽略连续碰撞以避免角落卡死。建议 0.02~0.08。")]
    public FsmFloat debounceTime;

    [HutongGames.PlayMaker.Tooltip("是否让物体朝向速度方向（顶视角可选）。")]
    public FsmBool alignRotationToVelocity;

    [HutongGames.PlayMaker.Tooltip("朝向平滑时间（秒）。")]
    public FsmFloat rotateSmoothing;

    [HutongGames.PlayMaker.Tooltip("调试：在 Scene 里画出速度与反射方向。")]
    public FsmBool debugDraw;

    private Rigidbody2D rb;
    private Transform tr;
    private float lastBounceTime;

    // 缓存
    private int lmMask;

    public override void Reset()
    {
        gameObject = null;
        layerMask = new FsmInt(-1);        // -1 表示所有层（与Unity LayerMask一致的语义很方便）
        includeTriggers = false;

        randomizeInitialDirection = true;
        initialSpeed = 5f;
        speed = 5f;
        minSpeed = 3f;
        maxSpeed = 0f;                     // 0或以下：不限制

        jitterAngleDeg = 12f;
        minAngleFromNormalDeg = 8f;
        nudgeDistance = 0.05f;
        debounceTime = 0.04f;

        alignRotationToVelocity = false;
        rotateSmoothing = 0.08f;

        debugDraw = false;

        lastBounceTime = -999f;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go == null)
        {
            Finish();
            return;
        }

        tr = go.transform;
        rb = go.GetComponent<Rigidbody2D>();
        if (!rb)
        {
            rb = go.AddComponent<Rigidbody2D>();
        }

        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 计算 LayerMask（FsmInt 直接当位掩码使用）
        lmMask = layerMask.Value;

        // 随机初始方向
        if (randomizeInitialDirection.Value)
        {
            var dir = UnityEngine.Random.insideUnitCircle.normalized;
            var v0 = dir * (initialSpeed.Value > 0 ? initialSpeed.Value : Mathf.Max(speed.Value, minSpeed.Value));
            rb.velocity = v0;
        }
        else
        {
            // 若未设置速度，给一个默认
            if (rb.velocity.sqrMagnitude < 0.0001f)
            {
                var dir = (Vector2)tr.right;
                var v0 = dir.normalized * Mathf.Max(speed.Value, minSpeed.Value);
                rb.velocity = v0;
            }
        }
    }

    public override void OnUpdate()
    {
        if (rb == null) return;

        // 维持目标速度并做限速
        var v = rb.velocity;
        float curSpeed = v.magnitude;
        float target = speed.Value > 0 ? speed.Value : curSpeed;

        if (curSpeed < Mathf.Max(minSpeed.Value, 0.01f))
        {
            // 速度太低，沿当前朝向或任意方向抬起来
            var dir = curSpeed < 0.001f ? UnityEngine.Random.insideUnitCircle.normalized : v.normalized;
            v = dir * Mathf.Max(minSpeed.Value, target);
        }
        else
        {
            // 拉向目标速度
            v = v.normalized * Mathf.Lerp(curSpeed, target, 0.12f);
        }

        // 上限
        if (maxSpeed.Value > 0 && v.magnitude > maxSpeed.Value)
            v = v.normalized * maxSpeed.Value;

        rb.velocity = v;

        // 可选：朝向速度方向（顶视角更自然）
        if (alignRotationToVelocity.Value && v.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            float newZ = Mathf.LerpAngle(tr.eulerAngles.z, targetAngle, Time.deltaTime / Mathf.Max(rotateSmoothing.Value, 0.0001f));
            tr.rotation = Quaternion.Euler(0, 0, newZ);
        }
    }

    // 只处理“边界层”的碰撞/触发
    public override void DoCollisionEnter2D(Collision2D collision)
    {
        if (!IsBoundary(collision.collider)) return;
        TryBounce(collision.GetContact(0).normal);
    }

    public override void DoTriggerEnter2D(Collider2D other)
    {
        if (!includeTriggers.Value) return;
        if (!IsBoundary(other)) return;

        // 触发器没有Contact normal，只能用从“中心指向碰撞点”的近似法线
        var normal = ((Vector2)tr.position - (Vector2)other.bounds.ClosestPoint(tr.position)).normalized;
        if (normal.sqrMagnitude < 1e-6f) normal = -rb.velocity.normalized; // 兜底
        TryBounce(normal);
    }

    private bool IsBoundary(Collider2D col)
    {
        // Layer 匹配（lmMask < 0 表示所有层）
        if (lmMask < 0) return true;
        int layerBit = 1 << col.gameObject.layer;
        return (lmMask & layerBit) != 0;
    }

    private void TryBounce(Vector2 hitNormal)
    {
        float t = Time.time;
        if (t - lastBounceTime < Mathf.Max(0f, debounceTime.Value)) return;
        lastBounceTime = t;

        var v = rb.velocity;
        if (v.sqrMagnitude < 1e-6f)
        {
            v = UnityEngine.Random.insideUnitCircle.normalized * Mathf.Max(minSpeed.Value, speed.Value);
        }

        // 1) 基础反射
        Vector2 reflect = Vector2.Reflect(v, hitNormal).normalized;

        // 2) 与法线保持最小夹角（避免几乎平行贴边）
        float minAngle = Mathf.Clamp(minAngleFromNormalDeg.Value, 0f, 89f);
        EnforceMinAngleFromNormal(ref reflect, hitNormal, minAngle);

        // 3) 抖动
        float jitter = jitterAngleDeg.Value;
        if (jitter > 0.01f)
        {
            float delta = UnityEngine.Random.Range(-jitter, jitter);
            reflect = Rotate(reflect, delta).normalized;
        }

        // 4) 维持速度并限速
        float target = Mathf.Max(speed.Value, minSpeed.Value);
        float maxV = maxSpeed.Value > 0 ? maxSpeed.Value : Mathf.Infinity;
        float newSpeed = Mathf.Clamp(v.magnitude, target, maxV);
        var newVel = reflect * newSpeed;

        // 5) 轻微推进，脱离边界
        float nudge = Mathf.Max(0f, nudgeDistance.Value);
        if (nudge > 1e-5f)
        {
            tr.position += (Vector3)(reflect * nudge);
        }

        rb.velocity = newVel;

        // Debug 绘制
        if (debugDraw.Value)
        {
            Debug.DrawRay(tr.position, v.normalized * 0.8f, Color.yellow, 0.3f);
            Debug.DrawRay(tr.position, reflect * 0.8f, Color.cyan, 0.3f);
            Debug.DrawRay(tr.position, hitNormal * 0.6f, Color.magenta, 0.3f);
        }
    }

    private static void EnforceMinAngleFromNormal(ref Vector2 dir, Vector2 normal, float minAngleDeg)
    {
        // 确保反射方向与法线至少有 minAngleDeg 夹角
        float angle = Vector2.Angle(dir, normal);
        if (angle < minAngleDeg)
        {
            // 围绕法线所在平面进行旋转（在2D里等价于朝远离法线的方向偏一点）
            float delta = (minAngleDeg - angle) + 0.001f;
            // 判断当前在法线的哪一侧，选择顺/逆时针
            float sign = Mathf.Sign(Vector3.Cross(normal, dir).z);
            dir = Rotate(dir, sign * delta).normalized;
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }
}

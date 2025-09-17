using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("纯Transform推进：在给定的矩形区域内进行无限弹射。支持父物体坐标系（局部空间）运算，避免父物体移动/旋转导致的顿挫或偏移。")]
public class BounceWithinRect_Translate2D_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体（默认FSM宿主）。不依赖刚体。")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("在谁的坐标系里计算（一般选父物体）。为空=世界坐标系。")]
    public FsmGameObject spaceRoot;

    [HutongGames.PlayMaker.Tooltip("矩形中心（在spaceRoot坐标系）。")]
    public FsmVector2 rectCenter;

    [HutongGames.PlayMaker.Tooltip("矩形半尺寸（在spaceRoot坐标系）。例如(5,3)表示宽10、高6。")]
    public FsmVector2 rectHalfSize;

    [HutongGames.PlayMaker.Tooltip("小球半径（会在边界内侧预留半径+皮肤）。")]
    public FsmFloat radius;

    [HutongGames.PlayMaker.Tooltip("与边界保持的皮肤厚度（防止数值抖）。")]
    public FsmFloat skin;

    [HutongGames.PlayMaker.Tooltip("移动速度（单位：每秒）。")]
    public FsmFloat speed;

    [HutongGames.PlayMaker.Tooltip("进入状态时随机一个初始方向。")]
    public FsmBool randomizeInitialDirection;

    [HutongGames.PlayMaker.Tooltip("如果不随机，则使用这个角度（度）。0度指向+X。")]
    public FsmFloat initialDirectionDeg;

    [HutongGames.PlayMaker.Tooltip("反弹时添加的随机抖动角（度）。")]
    public FsmFloat jitterAngleDeg;

    [HutongGames.PlayMaker.Tooltip("与法线的最小夹角（度），避免几乎平行导致贴边。")]
    public FsmFloat minAngleFromNormalDeg;

    [HutongGames.PlayMaker.Tooltip("每帧最多处理的弹射次数（防止高速越界时死循环），建议2~4。")]
    public FsmInt maxBouncesPerFrame;

    [HutongGames.PlayMaker.Tooltip("是否让物体朝向运动方向。")]
    public FsmBool alignRotationToDirection;

    [HutongGames.PlayMaker.Tooltip("朝向平滑时间（秒）。")]
    public FsmFloat rotateSmoothing;

    [HutongGames.PlayMaker.Tooltip("若物体上存在Rigidbody2D，是否在本Action期间临时禁用其模拟（推荐true）。")]
    public FsmBool temporarilyDisableRigidbody2D;

    [HutongGames.PlayMaker.Tooltip("调试：绘制边界与一步内的运动、法线。")]
    public FsmBool debugDraw;

    // —— 运行时缓存 —— //
    private Transform tr;
    private Transform space;
    private Vector2 dirLocal; // 在 space 坐标系里的单位方向
    private Rigidbody2D rb;
    private bool hadRb;
    private bool prevSimulated;

    public override void Reset()
    {
        gameObject = null;
        spaceRoot = null;
        rectCenter = Vector2.zero;
        rectHalfSize = new Vector2(5f, 3f);
        radius = 0.0f;
        skin = 0.01f;

        speed = 5f;
        randomizeInitialDirection = true;
        initialDirectionDeg = 0f;

        jitterAngleDeg = 10f;
        minAngleFromNormalDeg = 6f;

        maxBouncesPerFrame = 3;

        alignRotationToDirection = false;
        rotateSmoothing = 0.08f;

        temporarilyDisableRigidbody2D = true;
        debugDraw = false;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (!go) { Finish(); return; }

        tr = go.transform;
        space = spaceRoot.Value ? spaceRoot.Value.transform : null;

        // 方向初始化（在space坐标系）
        if (randomizeInitialDirection.Value)
        {
            dirLocal = Random.insideUnitCircle.normalized;
            if (dirLocal.sqrMagnitude < 1e-6f) dirLocal = Vector2.right;
        }
        else
        {
            float rad = initialDirectionDeg.Value * Mathf.Deg2Rad;
            dirLocal = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }

        // 如存在刚体，按需禁用其模拟（避免物理与Transform打架）
        rb = tr.GetComponent<Rigidbody2D>();
        hadRb = rb != null;
        if (hadRb && temporarilyDisableRigidbody2D.Value)
        {
            prevSimulated = rb.simulated;
            rb.simulated = false;
        }
    }

    public override void OnExit()
    {
        if (hadRb && temporarilyDisableRigidbody2D.Value && rb)
            rb.simulated = prevSimulated;
    }

    public override void OnUpdate()
    {
        if (tr == null) return;

        float dt = Time.deltaTime;
        float moveDist = Mathf.Max(0f, speed.Value) * dt;
        if (moveDist <= 1e-8f) return;

        // 取当前位置（space坐标系）
        Vector2 p = WorldToSpacePoint(tr.position);
        Vector2 center = rectCenter.Value;
        Vector2 half = Vector2.Max(rectHalfSize.Value, new Vector2(0.001f, 0.001f));
        float margin = Mathf.Max(0f, radius.Value + skin.Value);

        // 计算可用边界（内缩 margin）
        float minX = center.x - half.x + margin;
        float maxX = center.x + half.x - margin;
        float minY = center.y - half.y + margin;
        float maxY = center.y + half.y - margin;

        // 若一开始就不在边界内，直接钳制回去
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);

        int loops = Mathf.Max(1, maxBouncesPerFrame.Value);
        float remaining = moveDist;

        for (int i = 0; i < loops && remaining > 1e-6f; i++)
        {
            Vector2 m = dirLocal.normalized * remaining;

            // 计算与四边的命中时间（t in (0,1]）
            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;
            int hitAxis = 0; // 1=X墙(左右)，2=Y墙(上下)

            if (Mathf.Abs(m.x) > 1e-8f)
            {
                if (m.x > 0) tx = (maxX - p.x) / m.x; // 右墙
                else tx = (minX - p.x) / m.x; // 左墙
                if (tx > 1f || tx <= 0f) tx = float.PositiveInfinity;
            }
            if (Mathf.Abs(m.y) > 1e-8f)
            {
                if (m.y > 0) ty = (maxY - p.y) / m.y; // 上墙
                else ty = (minY - p.y) / m.y; // 下墙
                if (ty > 1f || ty <= 0f) ty = float.PositiveInfinity;
            }

            float tHit = Mathf.Min(tx, ty);

            if (float.IsInfinity(tHit))
            {
                // 本步不会撞墙，直接整段位移
                p += m;
                remaining = 0f;
                break;
            }

            // 先走到命中点（留一点epsilon）
            float eps = 1e-4f;
            float step = Mathf.Max(0f, (tHit * remaining) - eps);
            p += dirLocal.normalized * step;

            // 确认撞的是哪面墙，并做镜面反射
            if (tx < ty) { hitAxis = 1; } else { hitAxis = 2; }

            if (hitAxis == 1)
            {
                // 垂直墙（左右），法线在 ±X 方向：反转X分量
                dirLocal = new Vector2(-dirLocal.x, dirLocal.y).normalized;

                // 最小夹角与抖动（在space坐标系下）
                dirLocal = ApplyMinAngleAndJitter(dirLocal, Vector2.right * Mathf.Sign(m.x), minAngleFromNormalDeg.Value, jitterAngleDeg.Value);
                // Nudge到墙内一点（防数值卡牵）
                p.x = Mathf.Clamp(p.x, minX, maxX);
            }
            else
            {
                // 水平墙（上下），法线在 ±Y 方向：反转Y分量
                dirLocal = new Vector2(dirLocal.x, -dirLocal.y).normalized;

                dirLocal = ApplyMinAngleAndJitter(dirLocal, Vector2.up * Mathf.Sign(m.y), minAngleFromNormalDeg.Value, jitterAngleDeg.Value);
                p.y = Mathf.Clamp(p.y, minY, maxY);
            }

            // 剩余路程扣掉已走的
            remaining -= step;
        }

        // 写回世界坐标（只改XY，保留Z）
        Vector3 w = SpaceToWorldPoint(p);
        w.z = tr.position.z;
        tr.position = w;

        // 朝向
        if (alignRotationToDirection.Value && dirLocal.sqrMagnitude > 1e-8f)
        {
            Vector2 dirWorld = SpaceToWorldDir(dirLocal);
            float targetZ = Mathf.Atan2(dirWorld.y, dirWorld.x) * Mathf.Rad2Deg;
            float newZ = Mathf.LerpAngle(tr.eulerAngles.z, targetZ, Time.deltaTime / Mathf.Max(rotateSmoothing.Value, 1e-4f));
            tr.rotation = Quaternion.Euler(0, 0, newZ);
        }

        // 调试绘制
        if (debugDraw.Value)
        {
            // 边界
            Vector3 a = SpaceToWorldPoint(new Vector2(minX, minY));
            Vector3 b = SpaceToWorldPoint(new Vector2(maxX, minY));
            Vector3 c = SpaceToWorldPoint(new Vector2(maxX, maxY));
            Vector3 d = SpaceToWorldPoint(new Vector2(minX, maxY));
            Debug.DrawLine(a, b, Color.green, 0f);
            Debug.DrawLine(b, c, Color.green, 0f);
            Debug.DrawLine(c, d, Color.green, 0f);
            Debug.DrawLine(d, a, Color.green, 0f);

            // 方向
            Vector3 tip = w + (Vector3)(SpaceToWorldDir(dirLocal) * 0.6f);
            Debug.DrawLine(w, tip, Color.cyan, 0f);
        }
    }

    // —— 工具：坐标系转换 —— //
    private Vector2 WorldToSpacePoint(Vector3 world)
    {
        if (!space) return new Vector2(world.x, world.y);
        Vector3 l = space.InverseTransformPoint(world);
        return new Vector2(l.x, l.y);
    }
    private Vector3 SpaceToWorldPoint(Vector2 local)
    {
        if (!space) return new Vector3(local.x, local.y, tr.position.z);
        return space.TransformPoint(new Vector3(local.x, local.y, tr.position.z));
    }
    private Vector2 SpaceToWorldDir(Vector2 dir)
    {
        if (!space) return dir.normalized;
        Vector3 w = space.TransformDirection(new Vector3(dir.x, dir.y, 0f));
        return new Vector2(w.x, w.y).normalized;
    }

    // —— 工具：最小夹角 + 抖动 —— //
    private static Vector2 ApplyMinAngleAndJitter(Vector2 dir, Vector2 wallNormal, float minAngleDeg, float jitterDeg)
    {
        dir = dir.normalized;
        wallNormal = wallNormal.normalized;

        float angle = Vector2.Angle(dir, wallNormal);
        float minA = Mathf.Clamp(minAngleDeg, 0f, 89f);
        if (angle < minA)
        {
            float delta = (minA - angle) + 0.001f;
            float sign = Mathf.Sign(Vector3.Cross(wallNormal, dir).z); // 选择旋转方向
            dir = Rotate(dir, sign * delta).normalized;
        }

        if (jitterDeg > 0.001f)
        {
            float j = Random.Range(-jitterDeg, jitterDeg);
            dir = Rotate(dir, j).normalized;
        }
        return dir;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float s = Mathf.Sin(r);
        float c = Mathf.Cos(r);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }
}

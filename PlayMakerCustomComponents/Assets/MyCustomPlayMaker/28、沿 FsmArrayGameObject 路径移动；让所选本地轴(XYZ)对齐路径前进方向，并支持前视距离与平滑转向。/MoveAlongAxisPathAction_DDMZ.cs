using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using HutongGames.PlayMaker;
using UnityEngine;

[ActionCategory("Custom/Enemy")]
[HutongGames.PlayMaker.Tooltip("沿 FsmArray<GameObject> 路径移动；让所选本地轴(X/Y/Z)对齐路径前进方向，并支持前视距离与平滑转向。")]
public class MoveAlongAxisPathAction_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的物体")]
    public FsmOwnerDefault gameObject;

    [UIHint(UIHint.Variable)]
    [ArrayEditor(VariableType.GameObject)]
    [HutongGames.PlayMaker.Tooltip("路径点（FsmArray，元素为 GameObject）")]
    public FsmArray pathPoints;

    [HutongGames.PlayMaker.Tooltip("移动时长（秒）")]
    public FsmFloat duration;

    [HutongGames.PlayMaker.Tooltip("移动完成后触发的事件（可选）")]
    public FsmEvent onCompleteEvent;

    [HutongGames.PlayMaker.Tooltip("当目标 SetActive(false) 时是否停止")]
    public FsmBool stopWhenOwnerDisabled;

    // —— 朝向控制 —— //
    [HutongGames.PlayMaker.Tooltip("用物体本地 X 轴对齐路径方向")]
    public FsmBool aimWithLocalX;
    [HutongGames.PlayMaker.Tooltip("用物体本地 Y 轴对齐路径方向")]
    public FsmBool aimWithLocalY;
    [HutongGames.PlayMaker.Tooltip("用物体本地 Z 轴对齐路径方向")]
    public FsmBool aimWithLocalZ;
    [HutongGames.PlayMaker.Tooltip("反向（把所选轴的反方向对齐路径方向）")]
    public FsmBool invertAxis;

    [HutongGames.PlayMaker.Tooltip("2D 顶视（将方向约束到 XY 平面，路径点 Z 会被固定为物体当前 Z）")]
    public FsmBool topDown2D;

    // —— 平滑参数 —— //
    [HutongGames.PlayMaker.Tooltip("前视距离（米）：沿路径向前看的采样距离，值越大越平滑")]
    public FsmFloat lookAheadDistance;

    [HutongGames.PlayMaker.Tooltip("切换下一个路径点的阈值（米），用于推进 look 索引")]
    public FsmFloat waypointSwitchThreshold;

    [HutongGames.PlayMaker.Tooltip("启用指数平滑（Slerp）。关闭则使用最大角速度限幅")]
    public FsmBool useExponentialSmoothing;

    [HutongGames.PlayMaker.Tooltip("指数平滑强度（建议 5~12）。越大转向越快")]
    public FsmFloat turnSharpness;

    [HutongGames.PlayMaker.Tooltip("最大角速度（度/秒），仅在未启用指数平滑时生效")]
    public FsmFloat maxTurnSpeedDegPerSec;

    // —— 其它 —— //
    [HutongGames.PlayMaker.Tooltip("路径模式：2D 顶视建议 TopDown2D，3D 用 Full3D")]
    public PathMode pathMode = PathMode.TopDown2D;

    // —— 缓存 —— //
    private GameObject ownerGo;
    private Transform body;
    private TweenerCore<Vector3, Path, PathOptions> tween;
    private Vector3[] pts;      // 路径点
    private int lookIdx;        // 用于“瞄准”的参考索引
    private const float EPS = 1e-6f;

    public override void Reset()
    {
        gameObject = null;
        pathPoints = null;
        duration = 3f;
        onCompleteEvent = null;
        stopWhenOwnerDisabled = true;

        aimWithLocalX = false;
        aimWithLocalY = false;
        aimWithLocalZ = true;   // 默认用本地 Z
        invertAxis = false;

        topDown2D = false;
        lookAheadDistance = 0.6f;
        waypointSwitchThreshold = 0.2f;

        useExponentialSmoothing = true;
        turnSharpness = 8f;
        maxTurnSpeedDegPerSec = 360f;

        pathMode = PathMode.TopDown2D;

        tween = null;
        body = null;
        ownerGo = null;
        pts = null;
        lookIdx = 1;
    }

    public override void OnEnter()
    {
        ownerGo = Fsm.GetOwnerDefaultTarget(gameObject);
        if (ownerGo == null) { Finish(); return; }
        body = ownerGo.transform;

        if (!aimWithLocalX.Value && !aimWithLocalY.Value && !aimWithLocalZ.Value)
            aimWithLocalZ.Value = true;

        pts = CollectPoints();
        if (pts == null || pts.Length < 2) { Finish(); return; }

        lookIdx = 1;
        var mode = topDown2D.Value ? PathMode.TopDown2D : PathMode.Full3D;

        tween = body.DOPath(pts, duration.Value, PathType.CatmullRom, mode)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (onCompleteEvent != null) Fsm.Event(onCompleteEvent);
                        Finish();
                    });
    }

    public override void OnUpdate()
    {
        if (stopWhenOwnerDisabled.Value && (ownerGo == null || !ownerGo.activeInHierarchy))
        {
            KillTween();
            Finish();
            return;
        }
        if (tween == null || !tween.IsActive()) return;

        // 推进 lookIdx（靠近当前目标点则前进，减少抖动）
        if (lookIdx < pts.Length - 1)
        {
            float th2 = waypointSwitchThreshold.Value * waypointSwitchThreshold.Value;
            if ((pts[lookIdx] - body.position).sqrMagnitude <= th2) lookIdx++;
        }

        // —— 前视采样：从当前位置沿路径向前 lookAheadDistance 米取目标点 —— //
        Vector3 target = SampleLookahead(body.position, lookIdx, Mathf.Max(lookAheadDistance.Value, 0.001f));

        // 顶视：锁定 Z 平面
        Vector3 dir = target - body.position;
        if (topDown2D.Value) dir.z = 0f;
        if (invertAxis.Value) dir = -dir;
        if (dir.sqrMagnitude < EPS) return;
        dir.Normalize();

        // 计算“期望旋转”（让所选本地轴对齐到 dir）
        Vector3 chosenLocalAxis = GetChosenLocalAxis();
        Vector3 currentAxisWorld = body.TransformDirection(chosenLocalAxis);
        if (currentAxisWorld.sqrMagnitude < EPS) return;
        Quaternion desired = Quaternion.FromToRotation(currentAxisWorld, dir) * body.rotation;

        // —— 平滑转向 —— //
        if (useExponentialSmoothing.Value)
        {
            // 指数平滑（平滑且与帧率无关）
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, turnSharpness.Value) * Time.deltaTime);
            body.rotation = Quaternion.Slerp(body.rotation, desired, t);
        }
        else
        {
            // 最大角速度限幅（机械感更强）
            float maxDeg = Mathf.Max(1f, maxTurnSpeedDegPerSec.Value) * Time.deltaTime;
            body.rotation = Quaternion.RotateTowards(body.rotation, desired, maxDeg);
        }
    }

    public override void OnExit() => KillTween();

    // —— 工具方法 —— //

    private Vector3[] CollectPoints()
    {
        if (pathPoints == null || pathPoints.Length == 0) return null;
        var list = new List<Vector3>(pathPoints.Length);
        for (int i = 0; i < pathPoints.Length; i++)
        {
            var obj = pathPoints.Get(i) as GameObject;
            if (obj == null) continue;
            Vector3 p = obj.transform.position;
            if (topDown2D.Value) p.z = body.position.z;
            list.Add(p);
        }
        return list.ToArray();
    }

    // 从当前位置开始，沿着路径段（lookIdx 起），向前累计距离，得到 look-ahead 目标点
    private Vector3 SampleLookahead(Vector3 currentPos, int startIdx, float aheadDist)
    {
        // 先处理当前位置 -> 当前 lookIdx 点这一小段
        Vector3 segStart = currentPos;
        int idx = Mathf.Clamp(startIdx, 0, pts.Length - 1);

        for (int i = idx; i < pts.Length; i++)
        {
            Vector3 segEnd = pts[i];
            Vector3 seg = segEnd - segStart;
            float segLen = seg.magnitude;

            if (segLen > EPS)
            {
                if (aheadDist <= segLen)
                {
                    return segStart + seg.normalized * aheadDist;
                }
                aheadDist -= segLen;
            }

            // 继续下一段
            if (i < pts.Length - 1)
            {
                segStart = segEnd;
            }
            else
            {
                // 超出路径总长：就返回最后一个点
                return segEnd;
            }
        }

        return pts[pts.Length - 1];
    }

    private Vector3 GetChosenLocalAxis()
    {
        if (aimWithLocalZ.Value) return Vector3.forward;
        if (aimWithLocalY.Value) return Vector3.up;
        return Vector3.right; // 默认 X
    }

    private void KillTween()
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill();
            tween = null;
        }
    }
}

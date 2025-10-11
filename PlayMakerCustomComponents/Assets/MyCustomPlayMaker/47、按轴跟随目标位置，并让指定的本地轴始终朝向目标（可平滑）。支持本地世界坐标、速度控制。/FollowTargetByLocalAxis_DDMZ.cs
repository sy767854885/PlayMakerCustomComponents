using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Movement")]
[HutongGames.PlayMaker.Tooltip("直接跟随目标，按轴限制位置，并让指定的本地轴始终朝向目标。支持本地/世界坐标和速度控制。")]
public class FollowTargetByLocalAxis_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要移动的对象（Owner 或 指定 GameObject）")]
    public FsmOwnerDefault gameObject;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要跟随的目标 GameObject")]
    public FsmGameObject target;

    [HutongGames.PlayMaker.Tooltip("跟随移动速度（单位：单位/秒）。如果为0则瞬移。")]
    public FsmFloat moveSpeed;

    [HutongGames.PlayMaker.Tooltip("是否使用本地坐标（勾选为本地）。否则使用世界坐标。")]
    public FsmBool useLocalPosition;

    [ActionSection("Position Axes")]
    [HutongGames.PlayMaker.Tooltip("是否在 X 轴跟随目标位置")]
    public FsmBool followX;

    [HutongGames.PlayMaker.Tooltip("是否在 Y 轴跟随目标位置")]
    public FsmBool followY;

    [HutongGames.PlayMaker.Tooltip("是否在 Z 轴跟随目标位置")]
    public FsmBool followZ;

    [ActionSection("LookAt Axis (Local)")]
    [HutongGames.PlayMaker.Tooltip("启用让某个本地轴朝向目标")]
    public FsmBool enableLookAtLocalAxis;

    public enum LocalAxisOption
    {
        LocalForward_Z,
        LocalBack_negZ,
        LocalRight_X,
        LocalLeft_negX,
        LocalUp_Y,
        LocalDown_negY
    }

    [HutongGames.PlayMaker.Tooltip("选择哪个本地轴朝向目标（如 LocalForward_Z 代表本地 Z+ 指向目标）。")]
    public LocalAxisOption lookAtLocalAxis = LocalAxisOption.LocalForward_Z;

    [HutongGames.PlayMaker.Tooltip("旋转平滑速度（插值因子，单位：1/秒）。如果为0则直接对齐。")]
    public FsmFloat rotateSpeed;

    [HutongGames.PlayMaker.Tooltip("是否每帧执行（通常勾选）。")]
    public bool everyFrame = true;

    private Transform goTransform;
    private Transform targetTransform;

    public override void Reset()
    {
        gameObject = null;
        target = null;
        moveSpeed = 5f;
        useLocalPosition = false;
        followX = true;
        followY = true;
        followZ = true;
        enableLookAtLocalAxis = true;
        lookAtLocalAxis = LocalAxisOption.LocalForward_Z;
        rotateSpeed = 10f;
        everyFrame = true;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go != null) goTransform = go.transform;
        targetTransform = target.Value != null ? target.Value.transform : null;

        if (goTransform == null || targetTransform == null)
        {
            Finish();
            return;
        }

        DoFollow();

        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoFollow();
    }

    private void DoFollow()
    {
        if (goTransform == null || targetTransform == null) return;

        // 1. 计算目标差向量
        Vector3 currentPos = useLocalPosition.Value ? goTransform.localPosition : goTransform.position;
        Vector3 targetPos = useLocalPosition.Value ? targetTransform.localPosition : targetTransform.position;
        Vector3 delta = targetPos - currentPos;

        // 2. 按轴屏蔽不跟随的分量
        if (followX != null && !followX.Value) delta.x = 0f;
        if (followY != null && !followY.Value) delta.y = 0f;
        if (followZ != null && !followZ.Value) delta.z = 0f;

        // 3. 按速度限制移动
        float maxDelta = (moveSpeed != null ? moveSpeed.Value : 0f) * Time.deltaTime;
        if (delta.magnitude > maxDelta)
            delta = delta.normalized * maxDelta;

        // 4. 应用位置
        Vector3 newPos = currentPos + delta;
        if (useLocalPosition.Value)
            goTransform.localPosition = newPos;
        else
            goTransform.position = newPos;

        // 5. 本地轴朝向目标
        if (enableLookAtLocalAxis != null && enableLookAtLocalAxis.Value)
        {
            Vector3 dir = targetTransform.position - goTransform.position;
            if (dir.sqrMagnitude <= 0.0001f) return;
            Vector3 dirNorm = dir.normalized;

            Vector3 localAxis = Vector3.forward;
            switch (lookAtLocalAxis)
            {
                case LocalAxisOption.LocalForward_Z: localAxis = Vector3.forward; break;
                case LocalAxisOption.LocalBack_negZ: localAxis = Vector3.back; break;
                case LocalAxisOption.LocalRight_X: localAxis = Vector3.right; break;
                case LocalAxisOption.LocalLeft_negX: localAxis = Vector3.left; break;
                case LocalAxisOption.LocalUp_Y: localAxis = Vector3.up; break;
                case LocalAxisOption.LocalDown_negY: localAxis = Vector3.down; break;
            }

            Vector3 currentLocalAxisWorld = goTransform.TransformDirection(localAxis).normalized;
            Quaternion needed = Quaternion.FromToRotation(currentLocalAxisWorld, dirNorm);
            Quaternion targetRot = needed * goTransform.rotation;

            if (rotateSpeed != null && rotateSpeed.Value > 0f)
            {
                goTransform.rotation = Quaternion.Slerp(goTransform.rotation, targetRot, Mathf.Clamp01(rotateSpeed.Value * Time.deltaTime));
            }
            else
            {
                goTransform.rotation = targetRot;
            }
        }
    }
}

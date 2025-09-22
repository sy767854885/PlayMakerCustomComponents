using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Detection")]
[HutongGames.PlayMaker.Tooltip("在圆形范围内判断目标相对于中心的方向（9 状态：Center, Up, Down, Left, Right, UpRight, UpLeft, DownRight, DownLeft），每个方位的角度范围可自定义。当目标在半径外会触发 outsideEvent（可选）。支持 XY (2D/UI) 或 XZ (3D 地面) 平面判断。")]
public class CheckDirectionInCircle_CustomSectors_DDMZ : FsmStateAction
{
    [RequiredField] public FsmOwnerDefault target;

    [HutongGames.PlayMaker.Tooltip("圆心物体（为空则使用 Owner）。")]
    public FsmGameObject centerObject;

    [HutongGames.PlayMaker.Tooltip("手动指定圆心（仅在 centerObject 为空时生效）。")]
    public FsmVector3 centerPosition;

    [RequiredField] public FsmFloat radius;
    [HutongGames.PlayMaker.Tooltip("小于等于该距离视为 Center 状态（中间/基本状态）。")]
    public FsmFloat centerDeadZone;

    [HutongGames.PlayMaker.Tooltip("检测使用 XZ 平面（用于 3D 顶视/地面判断）。否则使用 XY（适用于2D或UI）。")]
    public bool useXZPlane = false;

    [HutongGames.PlayMaker.Tooltip("是否每帧检测。")]
    public bool everyFrame = true;

    [HutongGames.PlayMaker.Tooltip("仅在状态变化时发送事件。")]
    public bool onlySendOnChange = true;

    // Events
    public FsmEvent centerEvent;
    public FsmEvent upEvent;
    public FsmEvent downEvent;
    public FsmEvent leftEvent;
    public FsmEvent rightEvent;
    public FsmEvent upRightEvent;
    public FsmEvent upLeftEvent;
    public FsmEvent downRightEvent;
    public FsmEvent downLeftEvent;

    [HutongGames.PlayMaker.Tooltip("触发当目标在 radius 之外（可选）。")]
    public FsmEvent outsideEvent;

    [HutongGames.PlayMaker.Tooltip("保存当前状态为 int：0=center, 1=up, 2=down, 3=left, 4=right, 5=upRight, 6=upLeft, 7=downRight, 8=downLeft, -1=outside")]
    public FsmInt storeState;

    // Custom angle ranges for each sector (degrees). Use 0..360. Wrap-around supported (e.g., 350 -> 10).
    [HutongGames.PlayMaker.Tooltip("Up sector: min angle (deg). 0° is +X (right), 90° is +Y (up) in XY mode. In XZ mode 90° is +Z.")]
    public FsmFloat upMin = 67.5f;
    public FsmFloat upMax = 112.5f;

    [HutongGames.PlayMaker.Tooltip("Down sector")]
    public FsmFloat downMin = 247.5f;
    public FsmFloat downMax = 292.5f;

    [HutongGames.PlayMaker.Tooltip("Left sector")]
    public FsmFloat leftMin = 157.5f;
    public FsmFloat leftMax = 202.5f;

    [HutongGames.PlayMaker.Tooltip("Right sector")]
    public FsmFloat rightMin = 337.5f;
    public FsmFloat rightMax = 22.5f;

    [HutongGames.PlayMaker.Tooltip("UpRight sector")]
    public FsmFloat upRightMin = 22.5f;
    public FsmFloat upRightMax = 67.5f;

    [HutongGames.PlayMaker.Tooltip("UpLeft sector")]
    public FsmFloat upLeftMin = 112.5f;
    public FsmFloat upLeftMax = 157.5f;

    [HutongGames.PlayMaker.Tooltip("DownRight sector")]
    public FsmFloat downRightMin = 292.5f;
    public FsmFloat downRightMax = 337.5f;

    [HutongGames.PlayMaker.Tooltip("DownLeft sector")]
    public FsmFloat downLeftMin = 202.5f;
    public FsmFloat downLeftMax = 247.5f;

    private int lastState = int.MinValue;

    public override void Reset()
    {
        target = null;
        centerObject = null;
        centerPosition = new FsmVector3 { UseVariable = true };
        radius = 5f;
        centerDeadZone = 0.5f;
        useXZPlane = false;
        everyFrame = true;
        onlySendOnChange = true;

        centerEvent = null;
        upEvent = null;
        downEvent = null;
        leftEvent = null;
        rightEvent = null;
        upRightEvent = null;
        upLeftEvent = null;
        downRightEvent = null;
        downLeftEvent = null;
        outsideEvent = null;
        storeState = null;

        // defaults for 8-direction equal sectors (centered on cardinal/diagonals)
        upMin = 67.5f; upMax = 112.5f;
        downMin = 247.5f; downMax = 292.5f;
        leftMin = 157.5f; leftMax = 202.5f;
        rightMin = 337.5f; rightMax = 22.5f;
        upRightMin = 22.5f; upRightMax = 67.5f;
        upLeftMin = 112.5f; upLeftMax = 157.5f;
        downRightMin = 292.5f; downRightMax = 337.5f;
        downLeftMin = 202.5f; downLeftMax = 247.5f;

        lastState = int.MinValue;
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
        GameObject trg = Fsm.GetOwnerDefaultTarget(target);
        if (trg == null)
        {
            LogWarning("CheckDirectionInCircle_CustomSectors_DDMZ: target is null.");
            return;
        }

        Vector3 centerPos;
        if (centerObject != null && centerObject.Value != null)
            centerPos = centerObject.Value.transform.position;
        else if (centerPosition != null && !centerPosition.IsNone)
            centerPos = centerPosition.Value;
        else
            centerPos = Owner != null ? Owner.transform.position : Vector3.zero;

        Vector3 dir = trg.transform.position - centerPos;
        float dist = dir.magnitude;

        int state = -1; // -1 = outside, 0=center, 1..8 as defined

        // center
        if (!centerDeadZone.IsNone && dist <= centerDeadZone.Value)
        {
            state = 0;
        }
        else if (!radius.IsNone && dist <= radius.Value)
        {
            // compute angle
            float angle;
            if (useXZPlane)
            {
                // XZ: Atan2(z, x) -> 0° = +X, 90° = +Z
                angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            }
            else
            {
                // XY: Atan2(y, x) -> 0° = +X, 90° = +Y
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }

            // Normalize to 0..360
            angle = NormalizeAngle360(angle);

            // Check each sector — order doesn't really matter; first matched wins.
            if (IsAngleInRange(angle, upMin.Value, upMax.Value)) state = 1;          // up
            else if (IsAngleInRange(angle, downMin.Value, downMax.Value)) state = 2; // down
            else if (IsAngleInRange(angle, leftMin.Value, leftMax.Value)) state = 3; // left
            else if (IsAngleInRange(angle, rightMin.Value, rightMax.Value)) state = 4;// right
            else if (IsAngleInRange(angle, upRightMin.Value, upRightMax.Value)) state = 5; // upRight
            else if (IsAngleInRange(angle, upLeftMin.Value, upLeftMax.Value)) state = 6;   // upLeft
            else if (IsAngleInRange(angle, downRightMin.Value, downRightMax.Value)) state = 7; // downRight
            else if (IsAngleInRange(angle, downLeftMin.Value, downLeftMax.Value)) state = 8;   // downLeft
            else
            {
                // 如果没有任一范围匹配，认为是 outside（或你可以自定义为默认方向）
                state = -1;
            }
        }
        else
        {
            // outside radius
            state = -1;
        }

        // store
        if (storeState != null) storeState.Value = state;

        // only send on change
        if (onlySendOnChange && state == lastState)
        {
            return;
        }
        lastState = state;

        // dispatch events
        switch (state)
        {
            case 0:
                if (centerEvent != null) Fsm.Event(centerEvent);
                break;
            case 1:
                if (upEvent != null) Fsm.Event(upEvent);
                break;
            case 2:
                if (downEvent != null) Fsm.Event(downEvent);
                break;
            case 3:
                if (leftEvent != null) Fsm.Event(leftEvent);
                break;
            case 4:
                if (rightEvent != null) Fsm.Event(rightEvent);
                break;
            case 5:
                if (upRightEvent != null) Fsm.Event(upRightEvent);
                break;
            case 6:
                if (upLeftEvent != null) Fsm.Event(upLeftEvent);
                break;
            case 7:
                if (downRightEvent != null) Fsm.Event(downRightEvent);
                break;
            case 8:
                if (downLeftEvent != null) Fsm.Event(downLeftEvent);
                break;
            case -1:
            default:
                if (outsideEvent != null) Fsm.Event(outsideEvent);
                break;
        }
    }

    // Normalize angle to [0,360)
    private float NormalizeAngle360(float a)
    {
        a = a % 360f;
        if (a < 0) a += 360f;
        return a;
    }

    // Check whether angle (0..360) is inside [min..max] with wrap-around support
    private bool IsAngleInRange(float angle360, float minDeg, float maxDeg)
    {
        // Normalize min/max to 0..360
        minDeg = NormalizeAngle360(minDeg);
        maxDeg = NormalizeAngle360(maxDeg);

        if (Mathf.Approximately(minDeg, maxDeg))
        {
            // treated as full circle if equal — but通常这并不是期望，按你需要调整
            return true;
        }

        if (minDeg < maxDeg)
        {
            return angle360 >= minDeg && angle360 <= maxDeg;
        }
        else // wrap-around: e.g., min=350 max=10
        {
            return angle360 >= minDeg || angle360 <= maxDeg;
        }
    }
}

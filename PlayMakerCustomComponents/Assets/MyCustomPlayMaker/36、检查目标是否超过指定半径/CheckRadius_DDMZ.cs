using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Detection")]
[HutongGames.PlayMaker.Tooltip("检查目标是否超过指定半径。支持 World3D、World2D_XY、World2D_XZ、UI_Screen（像素）。触发 insideEvent (<= radius) 或 outsideEvent (> radius)。")]
public class CheckRadius_DDMZ : FsmStateAction
{
    public enum CheckMode { World3D, World2D_XY, World2D_XZ, UI_Screen }

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要检测的目标（要判断位置的物体）")]
    public FsmOwnerDefault target;

    [HutongGames.PlayMaker.Tooltip("圆心物体（为空则使用 Owner）。")]
    public FsmGameObject centerObject;

    [HutongGames.PlayMaker.Tooltip("手动指定圆心位置（仅在 centerObject 为空时生效）。")]
    public FsmVector3 centerPosition;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("半径。World 模式为世界单位；UI_Screen 模式为像素。")]
    public FsmFloat radius;

    [HutongGames.PlayMaker.Tooltip("检测模式：World3D、World2D_XY、World2D_XZ 或 UI_Screen（屏幕像素）。")]
    public CheckMode checkMode = CheckMode.World3D;

    [HutongGames.PlayMaker.Tooltip("当使用 UI_Screen 模式时，如果 Canvas 的 Render Mode 不是 Screen Space Overlay，指定用于转换的相机（通常是 Canvas 的 Render Camera）。可以为空（Overlay）。")]
    public Camera uiCamera;

    [HutongGames.PlayMaker.Tooltip("是否每帧检测（勾 = 每帧）。")]
    public bool everyFrame = true;

    [HutongGames.PlayMaker.Tooltip("仅当 inside/outside 状态发生变化时才发送事件。")]
    public bool onlySendOnChange = true;

    [HutongGames.PlayMaker.Tooltip("在半径之内（<= radius）触发该事件。")]
    public FsmEvent insideEvent;

    [HutongGames.PlayMaker.Tooltip("超过半径（> radius）触发该事件。")]
    public FsmEvent outsideEvent;

    [HutongGames.PlayMaker.Tooltip("把是否在半径内存为布尔（true 表示在半径内，<= radius）。")]
    public FsmBool storeInside;

    [HutongGames.PlayMaker.Tooltip("把实际距离（World 单位或像素）存储下来（可选）。")]
    public FsmFloat storeDistance;

    // internal
    private bool? lastInside = null;

    public override void Reset()
    {
        target = null;
        centerObject = null;
        centerPosition = new FsmVector3 { UseVariable = true };
        radius = 1f;
        checkMode = CheckMode.World3D;
        uiCamera = null;
        everyFrame = true;
        onlySendOnChange = true;
        insideEvent = null;
        outsideEvent = null;
        storeInside = null;
        storeDistance = null;
        lastInside = null;
    }

    public override void OnEnter()
    {
        DoCheck();

        if (!everyFrame)
            Finish();
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
            LogWarning("CheckRadius_DDMZ: target is null.");
            return;
        }

        Vector3 centerPos;
        if (centerObject != null && centerObject.Value != null)
            centerPos = centerObject.Value.transform.position;
        else if (centerPosition != null && !centerPosition.IsNone)
            centerPos = centerPosition.Value;
        else
            centerPos = Owner != null ? Owner.transform.position : Vector3.zero;

        float dist = 0f;
        bool isInside = false; // <= radius => inside

        switch (checkMode)
        {
            case CheckMode.World3D:
                dist = Vector3.Distance(trg.transform.position, centerPos);
                isInside = dist <= radius.Value;
                break;

            case CheckMode.World2D_XY:
                {
                    Vector2 a = new Vector2(trg.transform.position.x, trg.transform.position.y);
                    Vector2 b = new Vector2(centerPos.x, centerPos.y);
                    dist = Vector2.Distance(a, b);
                    isInside = dist <= radius.Value;
                }
                break;

            case CheckMode.World2D_XZ:
                {
                    Vector2 a = new Vector2(trg.transform.position.x, trg.transform.position.z);
                    Vector2 b = new Vector2(centerPos.x, centerPos.z);
                    dist = Vector2.Distance(a, b);
                    isInside = dist <= radius.Value;
                }
                break;

            case CheckMode.UI_Screen:
                {
                    // Convert world positions to screen points (pixels).
                    // If uiCamera is null and Canvas is Screen Space - Overlay, WorldToScreenPoint still works (camera null -> uses main camera, but for Overlay camera isn't needed).
                    // Unity's RectTransformUtility.WorldToScreenPoint accepts camera param (can be null).
                    Vector3 screenTarget = RectTransformUtility.WorldToScreenPoint(uiCamera, trg.transform.position);
                    Vector3 screenCenter = RectTransformUtility.WorldToScreenPoint(uiCamera, centerPos);
                    Vector2 a = new Vector2(screenTarget.x, screenTarget.y);
                    Vector2 b = new Vector2(screenCenter.x, screenCenter.y);
                    dist = Vector2.Distance(a, b);
                    isInside = dist <= radius.Value; // here radius is in pixels
                }
                break;
        }

        // store variables
        if (storeInside != null) storeInside.Value = isInside;
        if (storeDistance != null) storeDistance.Value = dist;

        // only send on change?
        if (onlySendOnChange)
        {
            if (lastInside.HasValue && lastInside.Value == isInside)
            {
                return;
            }
            lastInside = isInside;
        }

        // dispatch event
        if (isInside)
        {
            if (insideEvent != null) Fsm.Event(insideEvent);
        }
        else
        {
            if (outsideEvent != null) Fsm.Event(outsideEvent);
        }
    }
}

using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Transform")]
[HutongGames.PlayMaker.Tooltip("将一个物体的位置设置为另一个物体的位置。可选世界/本地坐标、偏移、按轴同步、是否每帧同步。")]
public class SetPositionToTarget_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要被设置位置的物体（Owner 或指定对象）")]
    public FsmOwnerDefault gameObject;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("作为参照的目标物体（位置从它来）")]
    public FsmGameObject targetGameObject;

    [HutongGames.PlayMaker.Tooltip("使用本地坐标（true）或世界坐标（false）进行对齐")]
    public FsmBool useLocalSpace;

    [HutongGames.PlayMaker.Tooltip("可选偏移（与上面的空间一致：本地/世界）。不需要可勾选 Use Variable")]
    public FsmVector3 offset;

    [HutongGames.PlayMaker.Tooltip("同步 X 轴")]
    public FsmBool syncX;

    [HutongGames.PlayMaker.Tooltip("同步 Y 轴")]
    public FsmBool syncY;

    [HutongGames.PlayMaker.Tooltip("同步 Z 轴")]
    public FsmBool syncZ;

    [HutongGames.PlayMaker.Tooltip("是否每帧同步（勾选后会在 Update/LateUpdate 中持续跟随）")]
    public FsmBool everyFrame;

    [HutongGames.PlayMaker.Tooltip("是否在 LateUpdate 中同步（避免跟随抖动，常用于跟随摄像机或骨骼）")]
    public FsmBool lateUpdate;

    private Transform selfT;
    private Transform targetT;

    public override void Reset()
    {
        gameObject = null;
        targetGameObject = null;
        useLocalSpace = false;
        offset = new FsmVector3 { UseVariable = true }; // 默认不使用偏移
        syncX = true;
        syncY = true;
        syncZ = true;
        everyFrame = false;
        lateUpdate = false;
        selfT = null;
        targetT = null;
    }

    public override void OnEnter()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        var targetGo = targetGameObject.Value;

        if (go == null || targetGo == null)
        {
            Finish();
            return;
        }

        selfT = go.transform;
        targetT = targetGo.transform;

        Apply();

        if (!everyFrame.Value) Finish();
    }

    public override void OnUpdate()
    {
        if (!everyFrame.Value || lateUpdate.Value) return;
        Apply();
    }

    public override void OnLateUpdate()
    {
        if (!everyFrame.Value || !lateUpdate.Value) return;
        Apply();
    }

    private void Apply()
    {
        if (selfT == null || targetT == null) return;

        // 读目标位置（本地/世界）
        Vector3 src = useLocalSpace.Value ? targetT.localPosition : targetT.position;

        // 读当前自身位置（用于只改选中的轴）
        Vector3 dst = useLocalSpace.Value ? selfT.localPosition : selfT.position;

        // 应用偏移（若未勾 Use Variable 则生效）
        if (!offset.IsNone)
        {
            var add = offset.Value;
            src += add;
        }

        Vector3 result = dst;
        if (syncX.Value) result.x = src.x;
        if (syncY.Value) result.y = src.y;
        if (syncZ.Value) result.z = src.z;

        if (useLocalSpace.Value) selfT.localPosition = result;
        else selfT.position = result;
    }
}

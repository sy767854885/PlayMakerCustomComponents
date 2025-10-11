using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Transform")]
[HutongGames.PlayMaker.Tooltip("让物体以指定速度绕选中的轴自旋转。可选择局部(local)或世界(world)坐标。")]
public class TransformSelfRotateByAxis_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要旋转的对象")]
    public FsmOwnerDefault gameObject;

    [HutongGames.PlayMaker.Tooltip("是否绕 X 轴旋转")]
    public FsmBool rotateX;

    [HutongGames.PlayMaker.Tooltip("是否绕 Y 轴旋转")]
    public FsmBool rotateY;

    [HutongGames.PlayMaker.Tooltip("是否绕 Z 轴旋转")]
    public FsmBool rotateZ;

    [HutongGames.PlayMaker.Tooltip("旋转速度 (度/秒)")]
    public FsmFloat speed;

    [HutongGames.PlayMaker.Tooltip("是否使用局部坐标系旋转")]
    public FsmBool useLocal;

    [HutongGames.PlayMaker.Tooltip("每帧更新")]
    public bool everyFrame = true;

    private Transform _transform;

    public override void Reset()
    {
        gameObject = null;
        rotateX = false;
        rotateY = true;
        rotateZ = false;
        speed = 90f;
        useLocal = true;
        everyFrame = true;
    }

    public override void OnEnter()
    {
        GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (go != null)
        {
            _transform = go.transform;
        }

        DoRotate();

        if (!everyFrame)
            Finish();
    }

    public override void OnUpdate()
    {
        DoRotate();
    }

    private void DoRotate()
    {
        if (_transform == null) return;

        Vector3 axis = new Vector3(
            rotateX.Value ? 1f : 0f,
            rotateY.Value ? 1f : 0f,
            rotateZ.Value ? 1f : 0f
        ).normalized;

        if (axis == Vector3.zero) return;

        float angle = speed.Value * Time.deltaTime;

        if (useLocal.Value)
            _transform.Rotate(axis, angle, Space.Self);
        else
            _transform.Rotate(axis, angle, Space.World);
    }
}

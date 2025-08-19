using DG.Tweening;               // 引入 DOTween 动画库
using HutongGames.PlayMaker;     // 引入 PlayMaker
using UnityEngine;

[ActionCategory("Custom/Enemy")]  // 在 PlayMaker Action 面板中归类到 "Custom/Enemy"
public class MoveAlongPathAction_DDMZ : FsmStateAction
{
    [RequiredField]
    public FsmOwnerDefault gameObject;  // 要移动的目标物体（FSM拥有者或指定对象）

    [Title("路径点 Transform[]")]
    public FsmGameObject[] pathPoints;  // 路径点数组（每个点是一个Transform所在的GameObject）

    public FsmFloat duration;           // 移动整个路径所需的时间（秒）

    public FsmEvent onCompleteEvent;    // 移动完成后要触发的事件（可选）

    private Transform body;             // 缓存目标物体的Transform


    /// <summary>
    /// 重置参数（当Action被添加或重置时调用）
    /// </summary>
    public override void Reset()
    {
        gameObject = null;
        pathPoints = null;
        duration = 3f;         // 默认移动时长为3秒
        onCompleteEvent = null;
    }

    /// <summary>
    /// 进入状态时执行一次
    /// </summary>
    public override void OnEnter()
    {
        // 获取目标物体
        GameObject go = Fsm.GetOwnerDefaultTarget(gameObject);

        // 如果目标物体不存在 或 没有路径点，则直接结束状态
        if (go == null || pathPoints == null || pathPoints.Length == 0)
        {
            Finish();
            return;
        }

        // 缓存Transform，便于后续操作
        body = go.transform;

        // 构建路径点的世界坐标数组
        Vector3[] path = new Vector3[pathPoints.Length];
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] != null)
                path[i] = pathPoints[i].Value.transform.position;
        }

        // 使用 DOTween 执行路径移动
        body.DOPath(
                path,                // 路径点数组
                duration.Value,      // 移动时长
                PathType.CatmullRom, // 路径类型：CatmullRom 插值，更平滑
                PathMode.TopDown2D   // 路径模式：适合2D俯视角（忽略Z旋转）
            )
            .SetLookAt(0.01f)        // 移动过程中，物体朝向路径前方（0.01f表示微小偏移）
            .SetEase(Ease.Linear)    // 匀速运动
            .OnComplete(() =>        // 移动完成时回调
            {
                if (onCompleteEvent != null)
                    Fsm.Event(onCompleteEvent); // 触发FSM事件
                Finish();                        // 结束Action
            });
    }
}

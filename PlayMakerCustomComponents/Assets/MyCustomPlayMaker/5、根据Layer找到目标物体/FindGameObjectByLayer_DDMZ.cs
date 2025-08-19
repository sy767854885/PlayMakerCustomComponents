using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom")]  // 在 PlayMaker 中分类到 "Custom"
[HutongGames.PlayMaker.Tooltip("查找第一个处于指定 Layer 的 GameObject。")]
public class FindGameObjectByLayer_DDMZ : FsmStateAction
{
    [HutongGames.PlayMaker.Tooltip("要查找的 Layer（层级编号）")]
    public FsmInt layer;  // 输入的目标 Layer 值（整数）

    [HutongGames.PlayMaker.Tooltip("存储找到的 GameObject")]
    [UIHint(UIHint.Variable)]
    public FsmGameObject storeResult;  // 输出变量，用来存储找到的物体

    /// <summary>
    /// 重置参数（当 Action 初始化时调用）
    /// </summary>
    public override void Reset()
    {
        layer = 0;
        storeResult = null;
    }

    /// <summary>
    /// 进入状态时执行一次
    /// </summary>
    public override void OnEnter()
    {
        // 获取场景中所有的 GameObject
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        // 遍历所有对象
        foreach (GameObject go in allObjects)
        {
            // 判断该物体是否处于指定 Layer
            if (go.layer == layer.Value)
            {
                // 存储找到的物体并退出循环（只取第一个）
                storeResult.Value = go;
                break;
            }
        }

        // 结束 Action
        Finish();
    }
}

using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Events")]
[HutongGames.PlayMaker.Tooltip("在 Inspector 中直接拖入一组 GameObject（或使用 FSM 中的 FsmArray），向其上的 PlayMakerFSM 发送事件。可按 FSM 名称筛选，留空则发送给该物体上的所有 FSM。")]
public class SendEventToGameObjects_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要发送事件的 GameObject 数组（类型必须为 GameObject）")]
    [UIHint(UIHint.Variable)]
    [ArrayEditor(VariableType.GameObject)]
    public FsmArray targetObjects; // 目标对象数组，可以在 Inspector 拖入或在 FSM 变量里设置

    [HutongGames.PlayMaker.Tooltip("只向名称匹配的 FSM 发送。留空表示该物体上所有 FSM 都会接收。")]
    public FsmString fsmNameFilter; // FSM 名称筛选条件

    [HutongGames.PlayMaker.Tooltip("是否在子物体上继续查找 FSM")]
    public FsmBool includeChildren; // 是否包含子物体上的 FSM

    [HutongGames.PlayMaker.Tooltip("是否在父物体上继续查找 FSM（一般不需要）")]
    public FsmBool includeParents;  // 是否包含父物体上的 FSM

    [UIHint(UIHint.FsmEvent)]
    [HutongGames.PlayMaker.Tooltip("发送的事件")]
    public FsmEvent sendEvent; // 要发送的事件

    [HutongGames.PlayMaker.Tooltip("进入该 State 时立即发送")]
    public FsmBool sendOnEnter; // 进入状态时立即执行发送

    [HutongGames.PlayMaker.Tooltip("是否每帧都发送（谨慎使用）")]
    public FsmBool everyFrame; // 是否每帧循环发送

    [HutongGames.PlayMaker.Tooltip("跳过为 Null 或未激活的对象/FSM")]
    public FsmBool skipNullOrDisabled; // 是否跳过无效对象或禁用的 FSM

    [HutongGames.PlayMaker.Tooltip("在 Console 输出调试信息")]
    public FsmBool debugLog; // 是否输出调试日志


    /// <summary>
    /// 初始化默认值
    /// </summary>
    public override void Reset()
    {
        targetObjects = null;
        fsmNameFilter = new FsmString { UseVariable = true, Value = "" };
        includeChildren = false;
        includeParents = false;

        sendEvent = null;

        sendOnEnter = true;
        everyFrame = false;
        skipNullOrDisabled = true;
        debugLog = false;
    }

    /// <summary>
    /// 进入状态时调用
    /// </summary>
    public override void OnEnter()
    {
        if (sendOnEnter.Value)
            Dispatch(); // 如果启用 sendOnEnter，立即发送事件

        if (!everyFrame.Value)
            Finish(); // 如果不需要每帧发送，直接结束 Action
    }

    /// <summary>
    /// 每帧调用
    /// </summary>
    public override void OnUpdate()
    {
        if (everyFrame.Value)
            Dispatch(); // 每帧持续发送事件
    }

    /// <summary>
    /// 核心逻辑：向目标 FSM 发送事件
    /// </summary>
    private void Dispatch()
    {
        // 如果没有设置对象数组
        if (targetObjects == null || targetObjects.Length == 0 || targetObjects.IsNone)
        {
            if (debugLog.Value) Debug.LogWarning("[SendEventToGameObjects_Sy] targetObjects 未设置或长度为 0。", Fsm?.Owner);
            return;
        }

        // 如果事件未设置
        if (sendEvent == null || string.IsNullOrEmpty(sendEvent.Name))
        {
            if (debugLog.Value) Debug.LogWarning("[SendEventToGameObjects_Sy] 事件未设置。", Fsm?.Owner);
            return;
        }

        string evt = sendEvent.Name;   // 事件名
        string filter = fsmNameFilter.Value; // FSM 筛选名
        int totalSent = 0;             // 计数器：本次共发送多少次事件

        // 遍历所有目标对象
        for (int i = 0; i < targetObjects.Length; i++)
        {
            GameObject go = targetObjects.Get(i) as GameObject;

            if (go == null)
            {
                if (!skipNullOrDisabled.Value && debugLog.Value)
                    Debug.LogWarning($"[SendEventToGameObjects_Sy] 第 {i} 个目标为 null 或非 GameObject。", Fsm?.Owner);
                continue;
            }

            // 如果物体未激活且选择跳过，则直接跳过
            if (!go.activeInHierarchy && skipNullOrDisabled.Value)
                continue;

            // 收集目标物体（包括子物体/父物体）的 FSM
            var fsms = CollectFsms(go, filter, includeChildren.Value, includeParents.Value);

            foreach (var fsm in fsms)
            {
                if (fsm == null) continue;

                // 如果 FSM 被禁用或者其所在物体未激活，并且选择跳过
                if (skipNullOrDisabled.Value && (!fsm.enabled || !fsm.gameObject.activeInHierarchy))
                    continue;

                // 发送事件
                fsm.SendEvent(evt);
                totalSent++;

                // 打印调试信息
                if (debugLog.Value)
                    Debug.Log($"[SendEventToGameObjects_Sy] 已向 {fsm.gameObject.name}/{fsm.FsmName} 发送事件：{evt}", fsm.gameObject);
            }
        }

        // 总结日志
        if (debugLog.Value)
            Debug.Log($"[SendEventToGameObjects_Sy] 本次共发送 {totalSent} 次事件：{evt}", Fsm?.Owner);
    }

    /// <summary>
    /// 收集目标 GameObject 上的 FSM（支持子物体和父物体）
    /// </summary>
    private static List<PlayMakerFSM> CollectFsms(GameObject root, string nameFilter, bool searchChildren, bool searchParents)
    {
        var result = new List<PlayMakerFSM>();

        // 先收集自己身上的 FSM
        AddFsmsFrom(root, nameFilter, result);

        // 是否收集子物体上的 FSM
        if (searchChildren)
        {
            var childFsms = root.GetComponentsInChildren<PlayMakerFSM>(true);
            foreach (var f in childFsms)
                if (!result.Contains(f)) AddIfMatch(f, nameFilter, result);
        }

        // 是否收集父物体上的 FSM
        if (searchParents)
        {
            var p = root.transform.parent;
            while (p != null)
            {
                AddFsmsFrom(p.gameObject, nameFilter, result);
                p = p.parent;
            }
        }

        return result;
    }

    /// <summary>
    /// 从指定 GameObject 收集 FSM
    /// </summary>
    private static void AddFsmsFrom(GameObject go, string nameFilter, List<PlayMakerFSM> list)
    {
        var fsms = go.GetComponents<PlayMakerFSM>();
        foreach (var f in fsms)
            AddIfMatch(f, nameFilter, list);
    }

    /// <summary>
    /// 根据名称筛选是否添加 FSM
    /// </summary>
    private static void AddIfMatch(PlayMakerFSM fsm, string nameFilter, List<PlayMakerFSM> list)
    {
        if (fsm == null) return;
        if (!string.IsNullOrEmpty(nameFilter))
        {
            if (!string.Equals(fsm.FsmName, nameFilter, System.StringComparison.Ordinal)) return;
        }
        list.Add(fsm);
    }
}

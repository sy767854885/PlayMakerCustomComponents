using System;
using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Boss")]
[HutongGames.PlayMaker.Tooltip("根据字符串key匹配并触发对应事件（仅在本FSM内）。")]
public class SendEventByStringKey_DDMZ : FsmStateAction
{
    [HutongGames.PlayMaker.Tooltip("输入的阶段/状态字符串，例如：Phase1、阶段1、Enrage等")]
    public FsmString key;

    [HutongGames.PlayMaker.Tooltip("是否忽略大小写（TRUE：忽略大小写）")]
    public FsmBool ignoreCase;

    [HutongGames.PlayMaker.Tooltip("是否允许模糊匹配（TRUE：包含匹配；FALSE：完全相等）")]
    public FsmBool allowPartialMatch;

    [Serializable]
    public class MapEntry
    {
        [HutongGames.PlayMaker.Tooltip("匹配用的字符串键，例如：Phase1 / 第一阶段 / P1")]
        public FsmString match;


        [HutongGames.PlayMaker.Tooltip("匹配成功时要发送的事件")]
        [UIHint(UIHint.FsmEvent)]
        public FsmEvent eventToSend;
    }

    [HutongGames.PlayMaker.Tooltip("字符串→事件 映射表（从上到下优先级）")]
    public MapEntry[] map;

    [UIHint(UIHint.FsmEvent)]
    [HutongGames.PlayMaker.Tooltip("当没有匹配到任何键时要触发的事件（可选）")]
    public FsmEvent notFoundEvent;

    public override void Reset()
    {
        key = "";
        ignoreCase = true;
        allowPartialMatch = false;
        map = new MapEntry[0];
        notFoundEvent = null;
    }

    public override void OnEnter()
    {
        var selected = ResolveEventByKey();

        if (selected != null)
        {
            Fsm.Event(selected);   // 仅在当前FSM触发
        }
        else if (notFoundEvent != null && !string.IsNullOrEmpty(notFoundEvent.Name))
        {
            Fsm.Event(notFoundEvent); // 兜底事件
        }

        Finish();
    }

    // —— 根据 key 在映射表里找事件 —— //
    private FsmEvent ResolveEventByKey()
    {
        string k = key.Value ?? "";
        k = k.Trim();

        if (ignoreCase.Value) k = k.ToLowerInvariant();

        // 按顺序查找匹配
        for (int i = 0; i < (map?.Length ?? 0); i++)
        {
            var entry = map[i];
            if (entry == null) continue;

            string m = entry.match?.Value ?? "";
            m = m.Trim();
            if (ignoreCase.Value) m = m.ToLowerInvariant();

            bool hit = allowPartialMatch.Value
                       ? (k.Contains(m) || m.Contains(k))
                       : (k == m);

            if (hit) return entry.eventToSend;
        }

        return null;
    }
}

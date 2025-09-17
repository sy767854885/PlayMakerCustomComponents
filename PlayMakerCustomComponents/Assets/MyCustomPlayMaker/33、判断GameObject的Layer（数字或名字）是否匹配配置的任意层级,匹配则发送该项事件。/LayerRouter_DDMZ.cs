using System;
using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Logic")]
[HutongGames.PlayMaker.Tooltip("判断 GameObject 的 Layer（数字或名字）是否匹配配置的任意层级；匹配则发送该项事件，未匹配可发送兜底事件。支持多层级、每帧检查、仅变化时触发。")]
public class LayerRouter_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要检查的对象（默认FSM宿主）")]
    public FsmOwnerDefault gameObject;

    [Serializable]
    public class LayerEventEntry
    {
        [HutongGames.PlayMaker.Tooltip("层级名字（优先使用；留空则使用数字）")]
        public FsmString layerName;

        [HutongGames.PlayMaker.Tooltip("层级数字（0~31；当名字留空时使用）")]
        public FsmInt layerNumber;

        [HutongGames.PlayMaker.Tooltip("当匹配到该层级时发送的事件")]
        public FsmEvent sendEvent;
    }

    [HutongGames.PlayMaker.Tooltip("层级→事件的映射列表（可添加多条）")]
    public LayerEventEntry[] entries;

    [HutongGames.PlayMaker.Tooltip("名字匹配是否忽略大小写（会遍历0~31层名做不区分大小写匹配）")]
    public FsmBool ignoreCaseForNames;

    [HutongGames.PlayMaker.Tooltip("当同一帧匹配多条时：TRUE=匹配到第一条就停止；FALSE=把所有匹配项的事件都发出")]
    public FsmBool stopOnFirstMatch;

    [HutongGames.PlayMaker.Tooltip("未匹配任何层级时要发送的事件（可选）")]
    public FsmEvent notMatchedEvent;

    [HutongGames.PlayMaker.Tooltip("是否每帧检查")]
    public FsmBool everyFrame;

    [HutongGames.PlayMaker.Tooltip("仅在“匹配结果/匹配项索引”发生变化时才触发，避免每帧重复发事件")]
    public FsmBool onlyOnChange;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("输出：是否匹配到了任何配置项")]
    public FsmBool storeMatched;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("输出：匹配到的第一个配置项的索引（未匹配为 -1）")]
    public FsmInt storeMatchedIndex;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("输出：被检测物体的当前 Layer 数字")]
    public FsmInt storeCurrentLayer;

    // 内部缓存
    private bool _hasLast;
    private int _lastMatchedIndex; // -1 表示未匹配

    public override void Reset()
    {
        gameObject = null;
        entries = new LayerEventEntry[0];
        ignoreCaseForNames = false;
        stopOnFirstMatch = true;

        notMatchedEvent = null;
        everyFrame = false;
        onlyOnChange = true;

        storeMatched = new FsmBool { UseVariable = true };
        storeMatchedIndex = new FsmInt { UseVariable = true };
        storeCurrentLayer = new FsmInt { UseVariable = true };

        _hasLast = false;
        _lastMatchedIndex = -1;
    }

    public override void OnEnter()
    {
        DoCheck();
        if (!everyFrame.Value) Finish();
    }

    public override void OnUpdate()
    {
        DoCheck();
    }

    private void DoCheck()
    {
        var go = Fsm.GetOwnerDefaultTarget(gameObject);
        if (!go)
        {
            FireNotMatched();
            return;
        }

        int currentLayer = go.layer;
        if (!storeCurrentLayer.IsNone) storeCurrentLayer.Value = currentLayer;

        // 计算所有匹配项索引
        int firstMatchedIndex = -1;
        bool firedAny = false;

        for (int i = 0; i < (entries?.Length ?? 0); i++)
        {
            var e = entries[i];
            if (e == null) continue;

            int targetLayer = ResolveLayer(e);
            if (targetLayer < 0) continue; // 无效配置，跳过

            if (currentLayer == targetLayer)
            {
                if (firstMatchedIndex < 0) firstMatchedIndex = i;

                // onlyOnChange：当且仅当“首个匹配索引”发生变化时才发送
                if (!onlyOnChange.Value || !_hasLast || _lastMatchedIndex != firstMatchedIndex)
                {
                    if (e.sendEvent != null) Fsm.Event(e.sendEvent);
                    firedAny = true;

                    if (stopOnFirstMatch.Value) break;
                    // 若不停止，继续找其他命中项
                }
            }
        }

        // 记录/输出匹配状态
        if (!storeMatched.IsNone) storeMatched.Value = firstMatchedIndex >= 0;
        if (!storeMatchedIndex.IsNone) storeMatchedIndex.Value = firstMatchedIndex;

        // 未匹配：根据 onlyOnChange 决定是否发未匹配事件
        if (firstMatchedIndex < 0)
        {
            if (!onlyOnChange.Value || !_hasLast || _lastMatchedIndex != -1)
            {
                FireNotMatched();
            }
        }

        _lastMatchedIndex = firstMatchedIndex;
        _hasLast = true;
    }

    private void FireNotMatched()
    {
        if (notMatchedEvent != null) Fsm.Event(notMatchedEvent);
        if (!storeMatched.IsNone) storeMatched.Value = false;
        if (!storeMatchedIndex.IsNone) storeMatchedIndex.Value = -1;
    }

    private int ResolveLayer(LayerEventEntry e)
    {
        // 优先使用名字
        if (e.layerName != null && !e.layerName.IsNone && !string.IsNullOrEmpty(e.layerName.Value))
        {
            string name = e.layerName.Value;
            int id = LayerMask.NameToLayer(name);
            if (id >= 0) return id;

            if (ignoreCaseForNames.Value)
            {
                for (int i = 0; i < 32; i++)
                {
                    string ln = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(ln) && string.Equals(ln, name, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return -1; // 名字没找到
        }

        // 其次使用数字
        if (e.layerNumber != null && !e.layerNumber.IsNone)
        {
            int n = e.layerNumber.Value;
            return (n >= 0 && n < 32) ? n : -1;
        }

        return -1;
    }
}

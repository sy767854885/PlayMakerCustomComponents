using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Sequence")]
[HutongGames.PlayMaker.Tooltip("把一组物体按固定时间间隔依次显示。支持是否保留之前已显示、是否循环、开始前是否全部隐藏，以及使用真实时间。")]
public class ShowGameObjectsSequentially_DDMZ : FsmStateAction
{
    [RequiredField]
    [ArrayEditor(VariableType.GameObject)]
    [HutongGames.PlayMaker.Tooltip("要依次显示的物体列表（FsmArray，元素类型为GameObject）")]
    public FsmArray objects;

    [HutongGames.PlayMaker.Tooltip("开始前的延时（秒）")]
    public FsmFloat startDelay;

    [HutongGames.PlayMaker.Tooltip("相邻两次显示之间的间隔（秒）")]
    public FsmFloat interval;

    [HutongGames.PlayMaker.Tooltip("开始时是否先把列表里的物体全部隐藏")]
    public FsmBool hideAllOnStart;

    [HutongGames.PlayMaker.Tooltip("显示新物体时，是否保留此前已显示的物体为可见（true=累计显示；false=只保留当前一个可见）")]
    public FsmBool keepPreviouslyShown;

    [HutongGames.PlayMaker.Tooltip("当到达末尾后是否循环")]
    public FsmBool loop;

    [HutongGames.PlayMaker.Tooltip("在循环模式下，每次重新从头开始之前是否清空（全部隐藏）")]
    public FsmBool clearOnLoop;

    [HutongGames.PlayMaker.Tooltip("使用真实时间（不受 Time.timeScale 影响）")]
    public FsmBool useUnscaledTime;

    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("输出：当前显示到的索引（0-based），可选")]
    public FsmInt storeCurrentIndex;

    [HutongGames.PlayMaker.Tooltip("序列播放完成时触发的事件（非循环时有效，可选）")]
    public FsmEvent finishedEvent;

    private int _count;
    private int _index;
    private float _nextTick;
    private bool _started;

    private float Now => useUnscaledTime.Value ? Time.unscaledTime : Time.time;

    public override void Reset()
    {
        objects = null;
        startDelay = 0f;
        interval = 0.5f;

        hideAllOnStart = true;
        keepPreviouslyShown = true;
        loop = false;
        clearOnLoop = false;
        useUnscaledTime = false;

        storeCurrentIndex = new FsmInt { UseVariable = true };
        finishedEvent = null;

        _count = 0;
        _index = 0;
        _started = false;
        _nextTick = 0f;
    }

    public override void OnEnter()
    {
        _count = objects != null ? objects.Length : 0;

        if (_count <= 0)
        {
            // 没有元素，直接结束
            if (finishedEvent != null) Fsm.Event(finishedEvent);
            Finish();
            return;
        }

        if (hideAllOnStart.Value)
            HideAll();

        _index = 0;
        WriteIndex();

        _started = false;
        _nextTick = Now + Mathf.Max(0f, startDelay.Value);
    }

    public override void OnUpdate()
    {
        if (_count <= 0) return;

        if (Now < _nextTick) return;

        if (!_started)
        {
            // 首次触发（开始/延时到达）
            ShowAt(_index);
            _started = true;
            ScheduleNext();
            return;
        }

        // 下一项
        _index++;

        if (_index >= _count)
        {
            if (loop.Value)
            {
                if (clearOnLoop.Value) HideAll();
                _index = 0;
            }
            else
            {
                if (finishedEvent != null) Fsm.Event(finishedEvent);
                Finish();
                return;
            }
        }

        ShowAt(_index);
        ScheduleNext();
    }

    private void ShowAt(int idx)
    {
        // 读元素
        var arr = objects.Values; // object[]，元素是 GameObject
        GameObject go = arr != null && idx >= 0 && idx < arr.Length ? arr[idx] as GameObject : null;

        if (!keepPreviouslyShown.Value)
        {
            // 只留当前一个可见
            HideAll();
        }

        if (go != null)
        {
            if (!go.activeSelf) go.SetActive(true);
        }

        WriteIndex();
    }

    private void HideAll()
    {
        var arr = objects.Values;
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            var go = arr[i] as GameObject;
            if (go != null && go.activeSelf) go.SetActive(false);
        }
    }

    private void ScheduleNext()
    {
        float step = Mathf.Max(0f, interval.Value);
        _nextTick = Now + step;
    }

    private void WriteIndex()
    {
        if (!storeCurrentIndex.IsNone) storeCurrentIndex.Value = _index;
    }
}

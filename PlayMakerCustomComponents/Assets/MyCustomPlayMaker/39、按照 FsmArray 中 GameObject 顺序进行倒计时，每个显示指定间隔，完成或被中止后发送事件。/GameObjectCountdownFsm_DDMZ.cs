using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Countdown")]
[HutongGames.PlayMaker.Tooltip("按照 FsmArray 中 GameObject 顺序进行倒计时，每个显示指定间隔，完成或被中止后发送事件。")]
public class GameObjectCountdownFsm_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("倒计时的 GameObject FsmArray，按照顺序显示。")]
    [HutongGames.PlayMaker.ArrayEditor(typeof(GameObject))]
    public FsmArray countdownObjects;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("每个 GameObject 显示的时间间隔（秒）。")]
    public FsmFloat interval = 1f;

    [HutongGames.PlayMaker.Tooltip("勾选：运行。取消勾选时立即中断并发送 stopEvent。")]
    public FsmBool run;

    [HutongGames.PlayMaker.Tooltip("倒计时完成后发送的事件。")]
    public FsmEvent finishEvent;

    [HutongGames.PlayMaker.Tooltip("被中止（run 变为 False）时发送的事件。")]
    public FsmEvent stopEvent;

    private int _currentIndex;
    private float _timer;

    public override void Reset()
    {
        countdownObjects = null;
        interval = 1f;
        run = true; // 默认勾选运行
        finishEvent = null;
        stopEvent = null;
        _currentIndex = 0;
        _timer = 0f;
    }

    public override void OnEnter()
    {
        if (countdownObjects == null || countdownObjects.Length == 0)
        {
            Finish();
            return;
        }

        _currentIndex = 0;
        _timer = 0f;

        // 初始化：隐藏所有对象
        HideAll();

        // 如果进入时 run 为 false，立即中断
        if (!run.Value)
        {
            if (stopEvent != null)
                Fsm.Event(stopEvent);
            Finish();
            return;
        }

        // 显示第一个
        GameObject first = countdownObjects.Get(_currentIndex) as GameObject;
        if (first != null)
            first.SetActive(true);
    }

    public override void OnUpdate()
    {
        if (countdownObjects == null || countdownObjects.Length == 0)
            return;

        // 检查运行开关：取消勾选则中断（隐藏所有并发送 stopEvent）
        if (!run.Value)
        {
            HideAll();
            if (stopEvent != null)
                Fsm.Event(stopEvent);
            Finish();
            return;
        }

        _timer += Time.deltaTime;
        if (_timer >= interval.Value)
        {
            _timer = 0f;

            // 隐藏当前
            GameObject current = countdownObjects.Get(_currentIndex) as GameObject;
            if (current != null)
                current.SetActive(false);

            _currentIndex++;

            if (_currentIndex >= countdownObjects.Length)
            {
                // 倒计时完成
                if (finishEvent != null)
                    Fsm.Event(finishEvent);
                Finish();
                return;
            }

            // 显示下一个
            GameObject next = countdownObjects.Get(_currentIndex) as GameObject;
            if (next != null)
                next.SetActive(true);
        }
    }

    private void HideAll()
    {
        for (int i = 0; i < countdownObjects.Length; i++)
        {
            GameObject go = countdownObjects.Get(i) as GameObject;
            if (go != null) go.SetActive(false);
        }
    }
}

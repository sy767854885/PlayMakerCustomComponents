using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Countdown")]
[HutongGames.PlayMaker.Tooltip("按照 FsmArray 中 GameObject 顺序进行倒计时，每个显示指定间隔，完成后发送事件。")]
public class GameObjectCountdownFsm_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("倒计时的 GameObject FsmArray，按照顺序显示。")]
    [HutongGames.PlayMaker.ArrayEditor(typeof(GameObject))]  // ✅ 指定类型
    public FsmArray countdownObjects;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("每个 GameObject 显示的时间间隔（秒）。")]
    public FsmFloat interval = 1f;

    [HutongGames.PlayMaker.Tooltip("倒计时完成后发送的事件。")]
    public FsmEvent finishEvent;

    private int _currentIndex;
    private float _timer;

    public override void Reset()
    {
        countdownObjects = null;  // 在 Inspector 中设置类型为 GameObject
        interval = 1f;
        finishEvent = null;
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
        for (int i = 0; i < countdownObjects.Length; i++)
        {
            GameObject go = countdownObjects.Get(i) as GameObject;
            if (go != null) go.SetActive(false);
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
}

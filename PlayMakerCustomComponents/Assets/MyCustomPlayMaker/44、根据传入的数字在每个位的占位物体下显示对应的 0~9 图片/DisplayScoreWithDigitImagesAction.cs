using System;
using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;

[ActionCategory("Custom/UI")]
[HutongGames.PlayMaker.Tooltip("根据传入的数字在每个位的占位物体下显示对应的 0~9 图片（按子物体索引 0-9）。" +
    "当 animateOnChange 为 True 时，只有位值发生变化的位会在切换到新数字时放大再缩小。")]
public class DisplayScoreWithDigitImagesAction : FsmStateAction
{
    [HutongGames.PlayMaker.Tooltip("按位的占位物体数组。按顺序拖入（例如：千万, 百万, 十万, 万, 千, 百, 十, 个）。")]
    [HutongGames.PlayMaker.ArrayEditor(typeof(GameObject))]
    public FsmArray digitPlaceObjects;

    [HutongGames.PlayMaker.Tooltip("要显示的分数（非负整数）。")]
    public FsmInt score;

    [HutongGames.PlayMaker.Tooltip("是否在位值发生变化时播放缩放动画（放大->缩小）。")]
    public FsmBool animateOnChange;

    [HutongGames.PlayMaker.Tooltip("当 animateOnChange 为真时的放大倍数（例如 1.3 表示放大到 130%）。")]
    public FsmFloat scaleFactor;

    [HutongGames.PlayMaker.Tooltip("放大再缩小的总时长（秒）。放大到顶点耗时为 total/2，接着缩小回原始耗时 total/2。")]
    public FsmFloat scaleCycleDuration;

    [HutongGames.PlayMaker.Tooltip("是否在 Action 结束或中断时把数字恢复到进入 State 时的颜色/scale（影响是否把 scale 归位）。")]
    public FsmBool restoreScaleOnExit;

    [HutongGames.PlayMaker.Tooltip("完成一次显示更新后的事件（可选）。")]
    public FsmEvent finishedEvent;

    // internal cached state
    private int[] _currentDigits;      // 当前显示的数字（-1 表示未知）
    private bool[] _animating;         // 各位是否在做动画
    private float[] _animTimer;        // 各位动画进度（0..scaleCycleDuration）
    private Vector3[] _animOriginalScale; // 被动画对象进场时记录的原始 scale
    private GameObject[] _activeDigitObjects; // 当前每个位激活的数字对象（用于还原scale）
    private int _placesCount;

    public override void Reset()
    {
        digitPlaceObjects = null;
        score = new FsmInt { Value = 0 };
        animateOnChange = new FsmBool { Value = true };
        scaleFactor = new FsmFloat { Value = 1.3f };
        scaleCycleDuration = new FsmFloat { Value = 0.2f };
        restoreScaleOnExit = new FsmBool { Value = true };
        finishedEvent = null;

        _currentDigits = null;
        _animating = null;
        _animTimer = null;
        _animOriginalScale = null;
        _activeDigitObjects = null;
        _placesCount = 0;
    }

    public override void OnEnter()
    {
        // 初始化缓存
        if (digitPlaceObjects == null || digitPlaceObjects.Length == 0)
        {
            Finish();
            return;
        }

        _placesCount = digitPlaceObjects.Length;

        _currentDigits = new int[_placesCount];
        _animating = new bool[_placesCount];
        _animTimer = new float[_placesCount];
        _animOriginalScale = new Vector3[_placesCount];
        _activeDigitObjects = new GameObject[_placesCount];

        // 尝试读取每个位当前正在显示的子物体索引（以 activeSelf 判断）
        for (int i = 0; i < _placesCount; i++)
        {
            _currentDigits[i] = -1;
            _animating[i] = false;
            _animTimer[i] = 0f;
            _animOriginalScale[i] = Vector3.one;
            _activeDigitObjects[i] = null;

            GameObject place = digitPlaceObjects.Get(i) as GameObject;
            if (place == null) continue;

            // 遍历子物体，查找 active 的（第一个 active 为当前显示）
            Transform t = place.transform;
            for (int c = 0; c < t.childCount; c++)
            {
                GameObject child = t.GetChild(c).gameObject;
                if (child.activeSelf)
                {
                    _currentDigits[i] = c; // 记录当前显示的数字索引
                    _activeDigitObjects[i] = child;
                    _animOriginalScale[i] = child.transform.localScale;
                    break;
                }
            }
        }

        // 执行一次更新显示（但不把所有位当作“变化”来触发动画）
        UpdateDisplayImmediate(score.Value);

        // 完成 OnEnter 后继续每帧驱动（用于动画）
    }

    public override void OnUpdate()
    {
        // 首先处理任何正在进行的缩放动画
        bool anyAnimating = false;
        float cycle = Mathf.Max(0.0001f, scaleCycleDuration.Value);
        for (int i = 0; i < _placesCount; i++)
        {
            if (!_animating[i]) continue;
            anyAnimating = true;

            _animTimer[i] += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer[i] / cycle);

            // 先放大到顶点（t 0->0.5），再缩小回原始（t 0.5->1）
            float half = 0.5f;
            float factor = 1f;
            if (t <= half)
            {
                float p = t / half; // 0..1
                factor = Mathf.Lerp(1f, scaleFactor.Value, p);
            }
            else
            {
                float p = (t - half) / half; // 0..1
                factor = Mathf.Lerp(scaleFactor.Value, 1f, p);
            }

            // 对当前激活的数字对象应用 scale（如果存在）
            GameObject obj = _activeDigitObjects[i];
            if (obj != null)
            {
                Vector3 baseScale = _animOriginalScale[i];
                obj.transform.localScale = baseScale * factor;
            }

            // 动画结束
            if (_animTimer[i] >= cycle)
            {
                _animating[i] = false;
                _animTimer[i] = 0f;
                // 确保恢复精确 scale
                if (_activeDigitObjects[i] != null)
                {
                    _activeDigitObjects[i].transform.localScale = _animOriginalScale[i];
                }
            }
        }

        // nothing else to do every frame except handle animation (score changes handled externally by calling UpdateDisplay)
        // 但我们仍然保留每帧循环，PlayMaker 的 Action 会一直运行直到 Finish() 被调用
    }

    /// <summary>
    /// 外部可通过 PlayMaker 的 Set Property / Call Method 等方式把 score.Value 改变并再次进入这个 State（或直接重复触发此 Action）。
    /// 如果你希望通过 Event 来触发更新，可以在 FSM 中把这个 Action 放在一个会重复执行的 State，然后通过 Set Property 改变 score 并重新触发该 State。
    /// 
    /// 这个方法用于根据当前 score 值来更新各位显示，并在 animateOnChange 为 true 且该位值发生变化时启动缩放动画。
    /// </summary>
    private void UpdateDisplayImmediate(int newScore)
    {
        if (digitPlaceObjects == null || digitPlaceObjects.Length == 0) return;

        // 计算每个位的新数字（从右到左：个位为 index = last）
        // 我们认为 digitPlaceObjects[0] 是最高位（例如千万），最后一项是个位（你 Inspector 的顺序应如此）
        int places = _placesCount;
        int remaining = Mathf.Max(0, newScore);

        for (int idx = places - 1; idx >= 0; idx--)
        {
            int power = places - 1 - idx; // 0-based from right: ones=0, tens=1, ...
            // 获取对应位的数值： (newScore / 10^power) % 10
            int div = 1;
            for (int p = 0; p < power; p++) div *= 10;
            int digit = (newScore / div) % 10;

            SetDigitAtPlace(idx, digit, animateOnChange.Value);
        }

        // 更新完后触发 finishedEvent（可选）
        if (finishedEvent != null)
            Fsm.Event(finishedEvent);
    }

    /// <summary>
    /// 在指定位 idx 设置显示 digit（0..9）。
    /// 若 animateIfChanged 为 true 且 digit 与 _currentDigits[idx] 不同，则在新激活的数字上播放放大缩小动画。
    /// </summary>
    private void SetDigitAtPlace(int placeIndex, int digit, bool animateIfChanged)
    {
        if (placeIndex < 0 || placeIndex >= _placesCount) return;

        GameObject place = digitPlaceObjects.Get(placeIndex) as GameObject;
        if (place == null) return;

        Transform t = place.transform;
        int childCount = t.childCount;

        // clamp digit to available children
        if (childCount == 0) return;
        int useDigit = Mathf.Clamp(digit, 0, childCount - 1);

        // 如果当前已经在显示相同索引，什么都不做
        if (_currentDigits[placeIndex] == useDigit)
        {
            return;
        }

        // 先关闭之前激活的（如果存在）
        for (int c = 0; c < childCount; c++)
        {
            GameObject child = t.GetChild(c).gameObject;
            child.SetActive(c == useDigit);
            if (c == useDigit)
            {
                // 记录当前激活对象与它的原始 scale，供动画使用
                _activeDigitObjects[placeIndex] = child;
                _animOriginalScale[placeIndex] = child.transform.localScale;
            }
        }

        // 标记当前已显示的数字索引
        int prev = _currentDigits[placeIndex];
        _currentDigits[placeIndex] = useDigit;

        // 如果需要动画并且与之前不同，启动缩放动画
        if (animateIfChanged && prev != -1 && prev != useDigit && animateOnChange.Value)
        {
            _animating[placeIndex] = true;
            _animTimer[placeIndex] = 0f;
            // original scale 已在上面记录
        }
    }

    public override void OnExit()
    {
        // 如果需要在退出时归位 scale，则把每个位当前激活对象的 scale 恢复为记录的原始值
        if (restoreScaleOnExit.Value)
        {
            for (int i = 0; i < _placesCount; i++)
            {
                GameObject obj = _activeDigitObjects[i];
                if (obj != null)
                {
                    obj.transform.localScale = _animOriginalScale[i];
                }
            }
        }

        // 不调用 Finish()，因为这是一个持续运行的 Action（但若你想把它做成一次性更新可在逻辑上改）
    }

    // 如果你想从外部直接调用（例如通过 PlayMaker 的 Call Method）来更新分数，请公开这个方法：
    public void SetScore(int newScore)
    {
        // 把 score 更新，然后立即应用显示
        if (score == null) score = new FsmInt();
        score.Value = newScore;
        UpdateDisplayImmediate(newScore);
    }
}

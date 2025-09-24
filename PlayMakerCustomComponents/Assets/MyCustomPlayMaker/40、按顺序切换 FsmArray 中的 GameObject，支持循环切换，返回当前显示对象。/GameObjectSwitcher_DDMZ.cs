using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Switch")]
[HutongGames.PlayMaker.Tooltip("按顺序切换 FsmArray 中的 GameObject，支持循环切换，返回当前显示对象。")]
public class GameObjectSwitcher_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("需要切换的 GameObject FsmArray")]
    [HutongGames.PlayMaker.ArrayEditor(typeof(GameObject))]
    public FsmArray objects;

    [HutongGames.PlayMaker.Tooltip("是否循环切换，勾选后最后一个显示下一项会从第一个开始")]
    public FsmBool loop = true;

    [HutongGames.PlayMaker.Tooltip("当前显示的 GameObject")]
    [UIHint(UIHint.Variable)]
    public FsmGameObject currentObject;

    public override void Reset()
    {
        objects = null;
        loop = true;
        currentObject = null;
    }

    public override void OnEnter()
    {
        Next();
        // 不隐藏或修改任何初始显示状态
        Finish(); // 立即结束，不阻塞 FSM
    }

    /// <summary>
    /// 执行下一项切换
    /// </summary>
    public void Next()
    {
        if (objects == null || objects.Length == 0) return;

        int currentIndex = -1;

        // 找到当前显示的对象索引
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects.Get(i) as GameObject;
            if (go != null && go.activeSelf)
            {
                currentIndex = i;
                break;
            }
        }

        // 没有任何显示对象，则默认第一个
        if (currentIndex == -1) currentIndex = 0;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= objects.Length)
        {
            if (loop.Value)
                nextIndex = 0;
            else
                nextIndex = currentIndex; // 停在最后
        }

        SwitchTo(nextIndex);
    }

    /// <summary>
    /// 切换到指定索引对象
    /// </summary>
    private void SwitchTo(int index)
    {
        if (objects == null || objects.Length == 0 || index < 0 || index >= objects.Length)
            return;

        // 隐藏当前显示的对象
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects.Get(i) as GameObject;
            if (go != null)
            {
                go.SetActive(i == index);
            }
        }

        // 更新返回变量
        GameObject currentGo = objects.Get(index) as GameObject;
        if (currentObject != null)
            currentObject.Value = currentGo;
    }
}

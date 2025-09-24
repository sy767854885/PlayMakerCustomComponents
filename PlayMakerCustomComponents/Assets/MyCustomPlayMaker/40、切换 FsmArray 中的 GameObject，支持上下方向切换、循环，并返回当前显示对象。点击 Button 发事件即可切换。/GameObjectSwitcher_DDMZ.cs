using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Switch")]
[HutongGames.PlayMaker.Tooltip("切换 FsmArray 中的 GameObject，支持上/下方向切换、循环，并返回当前显示对象。点击 Button 发事件即可切换。")]
public class GameObjectSwitcher_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("需要切换的 GameObject FsmArray")]
    [HutongGames.PlayMaker.ArrayEditor(typeof(GameObject))]
    public FsmArray objects;

    [HutongGames.PlayMaker.Tooltip("是否循环切换，勾选后最后一个/第一个显示时会循环")]
    public FsmBool loop = true;

    [HutongGames.PlayMaker.Tooltip("当前显示的 GameObject")]
    [UIHint(UIHint.Variable)]
    public FsmGameObject currentObject;

    [HutongGames.PlayMaker.Tooltip("切换方向：勾选为下一项，未勾选为上一项")]
    public FsmBool nextDirection = true;

    public override void Reset()
    {
        objects = null;
        loop = true;
        currentObject = null;
        nextDirection = true;
    }

    public override void OnEnter()
    {
        if (nextDirection.Value)
            Next();
        else
            Prev();

        Finish(); // 立即结束，不阻塞 FSM
    }

    private void Next()
    {
        if (objects == null || objects.Length == 0) return;

        int currentIndex = GetCurrentIndex();
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

    private void Prev()
    {
        if (objects == null || objects.Length == 0) return;

        int currentIndex = GetCurrentIndex();
        int prevIndex = currentIndex - 1;

        if (prevIndex < 0)
        {
            if (loop.Value)
                prevIndex = objects.Length - 1;
            else
                prevIndex = currentIndex; // 停在第一个
        }

        SwitchTo(prevIndex);
    }

    private int GetCurrentIndex()
    {
        int currentIndex = -1;
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects.Get(i) as GameObject;
            if (go != null && go.activeSelf)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex == -1) currentIndex = 0; // 没有任何显示对象时默认第一个
        return currentIndex;
    }

    private void SwitchTo(int index)
    {
        if (objects == null || objects.Length == 0 || index < 0 || index >= objects.Length) return;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject go = objects.Get(i) as GameObject;
            if (go != null)
                go.SetActive(i == index);
        }

        if (currentObject != null)
            currentObject.Value = objects.Get(index) as GameObject;
    }
}

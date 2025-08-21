using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Object")]
[HutongGames.PlayMaker.Tooltip("将多个物体显示或隐藏。")]
public class SetObjectsActive_DDMZ : FsmStateAction
{
    [RequiredField]
    [UIHint(UIHint.Variable)]
    [HutongGames.PlayMaker.Tooltip("要操作的物体数组 (FsmArray: GameObject)")]
    [ArrayEditor(VariableType.GameObject)]
    public FsmArray targetObjects;

    [HutongGames.PlayMaker.Tooltip("是否启用这些物体 (true=显示, false=隐藏)")]
    public FsmBool setActive;

    public override void Reset()
    {
        targetObjects = null;
        setActive = true;
    }

    public override void OnEnter()
    {
        if (targetObjects != null && targetObjects.Length > 0)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                var go = targetObjects.Get(i) as GameObject;
                if (go != null)
                {
                    go.SetActive(setActive.Value);
                }
            }
        }

        Finish();
    }
}

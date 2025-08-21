using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Object")]
[HutongGames.PlayMaker.Tooltip("生成一个物体，并设置父物体，且在父物体下归零。")]
public class CreateObjectWithParentZero_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要生成的物体预制体或GameObject")]
    public FsmGameObject prefab;

    [HutongGames.PlayMaker.Tooltip("父物体")]
    public FsmGameObject parent;

    [HutongGames.PlayMaker.Tooltip("存储生成出来的物体")]
    [UIHint(UIHint.Variable)]
    public FsmGameObject storeObject;

    public override void Reset()
    {
        prefab = null;
        parent = null;
        storeObject = null;
    }

    public override void OnEnter()
    {
        if (prefab.Value == null)
        {
            Finish();
            return;
        }

        // 实例化物体
        GameObject obj = Object.Instantiate(prefab.Value);

        // 如果有父物体
        if (parent.Value != null)
        {
            obj.transform.SetParent(parent.Value.transform);

            // 在父物体下归零
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
        }

        // 存储引用
        storeObject.Value = obj;

        Finish();
    }
}

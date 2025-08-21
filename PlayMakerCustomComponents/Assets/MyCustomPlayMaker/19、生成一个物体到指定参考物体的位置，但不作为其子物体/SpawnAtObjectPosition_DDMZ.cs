using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Spawn")]
[HutongGames.PlayMaker.Tooltip("生成一个物体到指定参考物体的位置，但不作为其子物体。")]
public class SpawnAtObjectPosition_DDMZ : FsmStateAction
{
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("要生成的预制体")]
    public FsmGameObject prefab;

    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("参考物体（生成位置将与它相同）")]
    public FsmGameObject referenceObject;

    [HutongGames.PlayMaker.Tooltip("生成后的实例（可选输出）")]
    [UIHint(UIHint.Variable)]
    public FsmGameObject storeResult;

    public override void Reset()
    {
        prefab = null;
        referenceObject = null;
        storeResult = null;
    }

    public override void OnEnter()
    {
        if (prefab == null || prefab.Value == null || referenceObject == null || referenceObject.Value == null)
        {
            Finish();
            return;
        }

        // 取参考位置
        Vector3 spawnPos = referenceObject.Value.transform.position;

        // 仅位置对齐，不设父物体
        GameObject inst = Object.Instantiate(prefab.Value, spawnPos, prefab.Value.transform.rotation);

        if (storeResult != null)
            storeResult.Value = inst;

        Finish();
    }
}

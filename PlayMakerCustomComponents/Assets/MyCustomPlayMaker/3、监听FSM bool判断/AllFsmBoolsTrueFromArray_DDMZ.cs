using System;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Custom/Logic")]
[HutongGames.PlayMaker.Tooltip("监听FsmArray中的一组GameObject，每个目标FSM里的Bool变量都为true时，触发事件。")]
public class AllFsmBoolsTrueFromArray_DDMZ : FsmStateAction
{
    [UIHint(UIHint.Variable)]
    [RequiredField]
    [HutongGames.PlayMaker.Tooltip("FSM中的FsmArray（类型必须为GameObject），存放要监听的对象。")]
    [ArrayEditor(VariableType.GameObject)]
    public FsmArray targetObjects;  // 目标对象数组（类型为GameObject）

    [HutongGames.PlayMaker.Tooltip("要检查的Bool变量名（在所有目标FSM里都必须有这个变量）。")]
    public FsmString variableName;  // 要检查的布尔变量名

    [HutongGames.PlayMaker.Tooltip("目标FSM名称（留空=该物体的第一个FSM）。")]
    public FsmString fsmName;       // FSM 名称（为空则取第一个 PlayMakerFSM）

    [HutongGames.PlayMaker.Tooltip("全部为true时要触发的事件。")]
    public FsmEvent allTrueEvent;   // 当所有目标的变量为 true 时要触发的事件

    [HutongGames.PlayMaker.Tooltip("是否每帧监听。")]
    public FsmBool everyFrame;      // 是否每一帧都进行检测

    [HutongGames.PlayMaker.Tooltip("只触发一次。触发后Action会结束。")]
    public FsmBool fireOnce;        // 是否只触发一次

    private bool hasFired;          // 记录是否已经触发过事件


    // 初始化
    public override void Reset()
    {
        targetObjects = null;
        variableName = "";
        fsmName = "";
        allTrueEvent = null;
        everyFrame = true;   // 默认每帧监听
        fireOnce = true;     // 默认只触发一次
        hasFired = false;    // 尚未触发
    }

    // 进入状态时执行一次
    public override void OnEnter()
    {
        EvaluateAndMaybeFire(); // 检查变量并可能触发事件

        // 如果不需要每帧检测，或者已经触发过一次并且只触发一次，则结束 Action
        if (!everyFrame.Value || (fireOnce.Value && hasFired))
        {
            Finish();
        }
    }

    // 每帧更新
    public override void OnUpdate()
    {
        if (!everyFrame.Value) return; // 如果不需要每帧执行则直接返回

        EvaluateAndMaybeFire(); // 检查变量并可能触发事件

        // 如果只需要触发一次，并且已经触发了，就结束 Action
        if (fireOnce.Value && hasFired)
        {
            Finish();
        }
    }

    /// <summary>
    /// 检查所有目标对象上的指定布尔变量是否都为 true，
    /// 如果全部为 true，则触发事件。
    /// </summary>
    private void EvaluateAndMaybeFire()
    {
        // 如果数组为空或没有元素，直接返回
        if (targetObjects == null || targetObjects.Length == 0)
        {
            return;
        }

        bool allTrue = true; // 假设所有变量都为 true

        // 遍历数组中的所有对象
        foreach (var obj in targetObjects.Values)
        {
            var go = obj as GameObject;
            if (go == null) continue; // 忽略空对象

            PlayMakerFSM targetFsm = null;

            // 如果指定了 FSM 名称，则查找该物体上所有 FSM 并匹配
            if (!string.IsNullOrEmpty(fsmName.Value))
            {
                var fsms = go.GetComponents<PlayMakerFSM>();
                foreach (var f in fsms)
                {
                    if (f != null && f.FsmName == fsmName.Value)
                    {
                        targetFsm = f;
                        break;
                    }
                }
            }
            else
            {
                // 如果没有指定 FSM 名称，就取第一个 PlayMakerFSM
                targetFsm = go.GetComponent<PlayMakerFSM>();
            }

            // 如果目标 FSM 不存在，则认为检查失败
            if (targetFsm == null)
            {
                allTrue = false;
                continue;
            }

            // 获取目标 FSM 的布尔变量
            var fsmBool = targetFsm.Fsm.Variables.GetFsmBool(variableName.Value);

            // 如果变量不存在，或者值为 false，则检查失败
            if (fsmBool == null || !fsmBool.Value)
            {
                allTrue = false;
                break;
            }
        }

        // 如果所有变量都为 true，并且还未触发过事件
        if (allTrue && !hasFired)
        {
            hasFired = true;
            Fsm.Event(allTrueEvent); // 发送事件
        }
    }
}

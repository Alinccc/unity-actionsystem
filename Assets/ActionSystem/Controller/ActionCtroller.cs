﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldActionSystem
{

    public class ActionCtroller
    {
        protected ActionCommand trigger { get; set; }
        protected List<int> queueID = new List<int>();
        protected IActionObj[] actionObjs { get; set; }
        protected bool isForceAuto;
        private ControllerType commandType { get { return trigger.commandType; } }
        private List<IActionCtroller> commandList = new List<IActionCtroller>();
        private Queue<ActionObj> actionQueue = new Queue<ActionObj>();
        private List<ActionHook> hookList = new List<ActionHook>();
        private List<IActionObj> startedActions = new List<IActionObj>();
        protected Coroutine coroutine;
        public static bool log = false;
        public ActionCtroller(ActionCommand trigger)
        {
            this.trigger = trigger;
            actionObjs = trigger.ActionObjs;
            ChargeQueueIDs();
            InitController();
        }
        private void InitController()
        {
            if ((commandType & ControllerType.Click) == ControllerType.Click)
            {
                var clickCtrl = new ClickContrller();
                clickCtrl.UserError = trigger.UserError;
                commandList.Add(clickCtrl);
            }
            if ((commandType & ControllerType.Connect) == ControllerType.Connect)
            {
                var lineRender = trigger.GetComponent<LineRenderer>();
                if (lineRender == null){
                    lineRender = trigger.gameObject.AddComponent<LineRenderer>();
                }
                var objs = Array.ConvertAll<IActionObj, ConnectObj>(Array.FindAll<IActionObj>(trigger.ActionObjs, x => x is ConnectObj), x => x as ConnectObj);
                var connectCtrl = new ConnectCtrl(lineRender, objs, trigger.lineMaterial, trigger.lineWight, trigger.pointDistence);
                connectCtrl.onError = trigger.UserError;
                commandList.Add(connectCtrl);
            }
            if ((commandType & ControllerType.Match) == ControllerType.Match)
            {
                var matchObjs = Array.ConvertAll<IActionObj, MatchObj>(Array.FindAll<IActionObj>(trigger.ActionObjs, x => x is MatchObj), x => x as MatchObj);
                var matchCtrl = new MatchCtrl( matchObjs);
                matchCtrl.UserError = trigger.UserError;
                commandList.Add(matchCtrl);
            }
            if ((commandType & ControllerType.Install) == ControllerType.Install)
            {
                var installObjs = Array.ConvertAll<IActionObj, InstallObj>(Array.FindAll<IActionObj>(trigger.ActionObjs, x => x is InstallObj), x => x as InstallObj);
                var installCtrl = new InstallCtrl(installObjs);
                installCtrl.UserError = trigger.UserError;
                commandList.Add(installCtrl);
            }
            if ((commandType & ControllerType.Rotate) == ControllerType.Rotate)
            {
                var rotAnimCtrl = new RotateAnimController();
                rotAnimCtrl.UserError = trigger.UserError;
                commandList.Add(rotAnimCtrl);
            }
            if((commandType & ControllerType.Rope) == ControllerType.Rope)
            {
                var ropeObjs = Array.ConvertAll<IActionObj, RopeObj>(Array.FindAll<IActionObj>(trigger.ActionObjs, x => x is RopeObj), x => x as RopeObj);
                var ropCtrl = new RopeController(ropeObjs);
                ropCtrl.UserError = trigger.UserError;
                commandList.Add(ropCtrl);
            }
        }

        public virtual void OnStartExecute(bool forceAuto)
        {
            this.isForceAuto = forceAuto;
            if (coroutine == null){
                coroutine = trigger.StartCoroutine(Update());
            }
            ForEachAction((ctrl) => {
                ctrl.OnStartExecute(forceAuto);
            });
            ExecuteAStep();
        }
        private void ChargeQueueIDs()
        {
            actionQueue.Clear();
            hookList.Clear();
            queueID.Clear();
            foreach (var item in actionObjs)
            {
                if (!queueID.Contains(item.QueueID))
                {
                    queueID.Add(item.QueueID);
                }
            }
            queueID.Sort();
        }
        public virtual void OnEndExecute()
        {
            CompleteQueues();
            ForEachAction((ctrl) =>
            {
                ctrl.OnEndExecute();
            });

            foreach (var item in actionObjs)
            {
                if(!item.Started)
                {
                    item.OnStartExecute(isForceAuto);
                }
                if (!item.Complete)
                {
                    item.OnEndExecute(true);
                }
            }

            StopUpdateAction();
        }

        public virtual void OnUnDoExecute()
        {
            UnDoQueues();
            ChargeQueueIDs();
           
            ForEachAction((ctrl) =>{
                ctrl.OnUnDoExecute();
            });
            foreach (var item in actionObjs){
                if(item.Started)
                {
                    item.OnUnDoExecute();
                }
            }
            StopUpdateAction();
        }

        public virtual IEnumerator Update()
        {
            while (true)
            {
                ForEachAction((ctrl) => {
                    ctrl.Update();
                });
                yield return null;
            }
        }

        private void OnCommandObjComplete(IActionObj obj)
        {
            startedActions.Remove(obj);
            var notComplete = Array.FindAll<IActionObj>(actionObjs, x => x.QueueID == obj.QueueID && !x.Complete);
            if (notComplete.Length == 0)
            {
                if (!ExecuteAStep())
                {
                    if (!trigger.Completed)
                        trigger.Complete();
                }
            }
            else if(actionQueue.Count > 0)//正在循环执行
            {
                QueueExectueActions();
            }
        }

        public void CompleteOneStarted()
        {
            if(startedActions.Count > 0)
            {
                var action = startedActions[0];
                startedActions.RemoveAt(0);
                action.OnEndExecute(true);
            }
            else
            {
                Debug.Log("startedActions.Count == 0");
            }
        }
        private void CompleteQueues()
        {
            while (actionQueue.Count > 0)
            {
                var action = actionQueue.Dequeue();
                if (!action.Complete)
                {
                    action.OnEndExecute(true);
                }
            }
            while (hookList.Count > 0)
            {
                var hook = hookList[0];
                hookList.RemoveAt(0);
                if (!hook.Complete)
                {
                    hook.OnEndExecute(true);
                }
            }
        }
        private void UnDoQueues()
        {
            while (actionQueue.Count > 0)
            {
                var action = actionQueue.Dequeue();
                if (action.Started){
                    action.OnUnDoExecute();
                }
            }
            while (hookList.Count > 0)
            {
                var hook = hookList[0];
                    hookList.RemoveAt(0);
                if (hook.Started)
                {
                    hook.OnUnDoExecute();
                }
            }

        }
        protected bool ExecuteAStep()
        {
            if (queueID.Count > 0)
            {
                var id = queueID[0];
                queueID.RemoveAt(0);
                var neetActive = Array.FindAll<IActionObj>(actionObjs, x => x.QueueID == id && !x.Started);
                if (isForceAuto)
                {
                    hookList.Clear();
                    actionQueue.Clear();
                    foreach (var item in neetActive)
                    {
                        if(item is ActionObj)
                        {
                            actionQueue.Enqueue(item as ActionObj);
                        }
                        else /*if(item is ActionHook)*/
                        {
                            hookList.Add(item as ActionHook);
                            startedActions.Add(item);
                        }
                    }
                    QueueExectueActions();
                }
                else
                {
                    foreach (var item in neetActive)
                    {
                        var obj = item;
                        TryStartAction(obj);
                    }
                }
                return true;
            }
            return false;
        }

        protected void QueueExectueActions()
        {
            if(actionQueue.Count > 0)
            {
                var actionObj = actionQueue.Dequeue();
                if(log) Debug.Log("QueueExectueActions" + actionObj);
                TryStartAction(actionObj);
            }
            ///最后执行hook
            if (actionQueue.Count == 0)
            {
                while (hookList.Count > 0)
                {
                    var item = hookList[0];
                    hookList.RemoveAt(0);
                    TryStartAction(item);
                }
            }
        }
        private void TryStartAction(IActionObj obj)
        {
            if(log) Debug.Log("Start A Step:" + obj);
            if(!obj.Started)
            {
                if(obj.CameraID == null)
                {
                    StartAction(obj);
                }
                else
                {
                    CameraController.SetViewCamera(() =>
                    {
                        StartAction(obj);
                    }, GetCameraID(obj));
                }
            }
            else
            {
                Debug.LogError(obj + " allready started");
            }
           
        }

        private void StartAction(IActionObj obj)
        {
            if (!obj.Started)
            {
                obj.onEndExecute = () => OnCommandObjComplete(obj);
                obj.OnStartExecute(isForceAuto);
                startedActions.Add(obj);
            }
        }


        private string GetCameraID(IActionObj obj)
        {
            //不需要改变相机状态的钩子
            if (obj is ActionHook)
            {
                return null;
            }
            //忽略匹配相机
            else if (Setting.ignoreMatch && obj is MatchObj && !(obj as MatchObj).ignorePass)
            {
                return null;
            }
            else if(Setting.ignoreInstall && obj is InstallObj && !(obj as InstallObj).ignorePass)
            {
                return null;
            }
            //除要求使用特殊相机或是动画步骤,都用主摄像机
            else if (Setting.useOperateCamera || obj is AnimObj)
            {
                if (string.IsNullOrEmpty(obj.CameraID))
                {
                    return trigger.CameraID;
                }
                else
                {
                    return obj.CameraID;
                }
            }
            //默认是主相机
            else
            {
                return CameraController.defultID;
            }
        }

        private void ForEachAction(UnityEngine.Events.UnityAction<IActionCtroller> OnRetive)
        {
            foreach (var item in commandList)
            {
                OnRetive(item);
            }
        }

        private void StopUpdateAction()
        {
            CameraController.StopLastCoroutine();
            if (coroutine != null)
            {
                trigger.StopCoroutine(coroutine);
                coroutine = null;
            }
        }
    }
}
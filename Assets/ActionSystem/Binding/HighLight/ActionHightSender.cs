﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using WorldActionSystem;

namespace WorldActionSystem
{
    public class ActionHightSender : ActionObjBinding
    {
        public string key;
        private bool noticeAuto { get { return Setting.highLightNotice; } }
        private string highLight { get { return "HighLightObjects"; } }
        private string unhighLight { get { return "UnHighLightObjects"; } }

        protected void Update()
        {
            if (!noticeAuto) return;
            if (actionObj.Complete) return;
            if (actionObj.Started & !actionObj.Complete)
            {
                SetElementState(true);
            }
            else
            {
                SetElementState(false);
            }
        }
        protected override void OnBeforeActive()
        {
            if (noticeAuto)
            {
                SetElementState(true);
            }
        }
        protected override void OnBeforeComplete()
        {
            if (noticeAuto)
            {
                SetElementState(false);
            }
        }
        protected override void OnBeforeUnDo()
        {
            if (noticeAuto)
            {
                SetElementState(false);
            }
        }
        protected void SetElementState(bool open)
        {
            if (open)
            {
                EventController.NotifyObserver<string>(highLight, key);
            }
            else
            {
                EventController.NotifyObserver<string>(unhighLight, key);
            }
        }

    }

}
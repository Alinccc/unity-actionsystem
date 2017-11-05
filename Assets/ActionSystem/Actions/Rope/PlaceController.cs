﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;

namespace WorldActionSystem
{
    public abstract class PlaceObj : ActionObj
    {
        public bool autoInstall;//自动安装
        public bool ignorePass;//反忽略
        public Transform passBy;//路过
        public bool straightMove;//直线移动
        public bool ignoreMiddle;//忽略中间点
        public bool hideOnInstall;//安装完后隐藏

        public virtual GameObject Go { get { return gameObject; } }
        public virtual bool AlreadyPlaced { get { return obj != null; } }
        public abstract int layer { get; }
        public virtual PickUpAbleElement obj { get; protected set; }

        protected virtual void Awake()
        {
            gameObject.layer = layer;
            onBeforeStart.AddListener(OnBeforeStart);
            onBeforeComplete.AddListener(OnBeforeComplete);
            onBeforeUnDo.AddListener(OnBeforeUnDo);
        }
       protected virtual void OnBeforeStart(bool auto)
        {
            ElementController.ActiveElements(this);
        }
        protected virtual void OnBeforeComplete(bool force)
        {
            ElementController.CompleteElements(this, false);
        }
        protected virtual void OnBeforeUnDo()
        {
            ElementController.CompleteElements(this, true);
        }
        public override void OnStartExecute(bool auto = false)
        {
            base.OnStartExecute(auto);
            if (auto || autoInstall){
                OnAutoInstall();
            }
        }

        protected abstract void OnAutoInstall();
        
        public virtual void Attach(PickUpAbleElement obj)
        {
            if(this.obj != null) {
                Detach();
            }

            this.obj = obj;
            obj.onInstallOkEvent += OnInstallComplete;
            obj.onUnInstallOkEvent += OnUnInstallComplete;
        }

        protected virtual void OnInstallComplete() { }

        protected virtual void OnUnInstallComplete() { }

        public virtual PickUpAbleElement Detach()
        {
            PickUpAbleElement old = obj;
            old.onInstallOkEvent -= OnInstallComplete;
            old.onUnInstallOkEvent -= OnUnInstallComplete;
            obj = default(PickUpAbleElement);
            return old;
        }
    }

    public abstract class PlaceController : IActionCtroller
    {
        public UnityAction<string> UserError { get; set; }
        protected IHighLightItems highLight;
        protected PickUpAbleElement pickedUpObj;
        protected bool pickedUp;
        protected PlaceObj installPos;
        protected Ray ray;
        protected RaycastHit hit;
        protected RaycastHit[] hits;
        protected bool installAble;
        protected string resonwhy;
        protected float hitDistence { get { return Setting.hitDistence; } }
        protected Camera viewCamera { get { return CameraController.ActiveCamera; } }
        protected bool activeNotice { get { return Setting.highLightNotice; } }
        protected Ray disRay;
        protected RaycastHit disHit;
        protected float elementDistence;
        protected abstract int PlacePoslayerMask { get; }//1 << Setting.installPosLayer

        public PlaceController()
        {
            highLight = new ShaderHighLight();
        }

        #region 鼠标操作事件
        public virtual void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnLeftMouseClicked();
            }
            else if (pickedUp)
            {
                UpdatePlaceState();
                MoveWithMouse(elementDistence += Input.GetAxis("Mouse ScrollWheel"));
            }
        }

        protected virtual void OnLeftMouseClicked()
        {
            if (!pickedUp)
            {
                SelectAnElement();
            }
            else
            {
                TryPlaceObject();
            }
        }

        /// <summary>
        /// 在未屏幕锁的情况下选中一个没有元素
        /// </summary>
        void SelectAnElement()
        {
            ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, hitDistence, (1 << Setting.pickUpElementLayer)))
            {
                var pickedUpObj = hit.collider.GetComponent<PickUpAbleElement>();
                if (pickedUpObj != null && pickedUpObj.Started)
                {
                    this.pickedUpObj = pickedUpObj;
                    if (pickedUpObj.HaveBinding)
                    {
                        pickedUpObj.NormalUnInstall();
                    }
                    pickedUpObj.OnPickUp();
                    pickedUp = true;
                    elementDistence = Vector3.Distance(viewCamera.transform.position, pickedUpObj.transform.position);
                }

            }
        }

        public void UpdatePlaceState()
        {
            ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, hitDistence, PlacePoslayerMask);
            if (hits != null || hits.Length > 0)
            {
                bool hited = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.name == pickedUpObj.name)
                    {
                        hited = true;
                        installPos = hits[i].collider.GetComponent<PlaceObj>();
                        installAble = CanPlace(installPos, pickedUpObj, out resonwhy);
                    }
                }
                if (!hited)
                {
                    installAble = false;
                    resonwhy = "零件放置位置不正确";
                }
            }

            if (installAble)
            {
                //可安装显示绿色
                if (activeNotice) highLight.HighLightTarget(pickedUpObj.Render, Color.green);
            }
            else
            {
                //不可安装红色
                if (activeNotice) highLight.HighLightTarget(pickedUpObj.Render, Color.red);
            }
        }

        /// <summary>
        /// 尝试安装元素
        /// </summary>
        void TryPlaceObject()
        {
            ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (installAble)
            {
                PlaceObject(installPos, pickedUpObj);
            }
            else
            {
                PlaceWrong(pickedUpObj);
                UserError(resonwhy);
            }

            pickedUp = false;
            installAble = false;
            if (activeNotice) highLight.UnHighLightTarget(pickedUpObj.Render);
        }

        protected abstract bool CanPlace(PlaceObj placeItem, PickUpAbleElement element, out string why);

        protected abstract void PlaceObject(PlaceObj pos, PickUpAbleElement pickup);

        protected abstract void PlaceWrong(PickUpAbleElement pickup);

        /// <summary>
        /// 跟随鼠标
        /// </summary>
        void MoveWithMouse(float dis)
        {
            disRay = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(disRay, out disHit, dis, 1 << Setting.obstacleLayer))
            {
                pickedUpObj.transform.position = disHit.point;
            }
            else
            {
                pickedUpObj.transform.position = viewCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, dis));
            }
        }

        #endregion

    }
}
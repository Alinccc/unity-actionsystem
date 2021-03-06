﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;

namespace WorldActionSystem
{
    /// <summary>
    /// 将ropeItem安装到指定RopeObj上
    /// 然后安装内部的点到对就的RopeObj上
    /// </summary>
    public class RopeCtrl : OperateController
    {
        public override ControllerType CtrlType { get { return ControllerType.Rope; } }
        private RopeObj ropeTarget { get { if (ropeSelected == null) return null; return ropeSelected.BindingTarget; } }
        private RopeItem ropeSelected;
        private Collider pickUpedRopeNode;
        private bool pickDownAble;
        private float elementDistence;
        private Ray ray;
        private Ray disRay;

        private RaycastHit hit;
        private RaycastHit disHit;
        private RaycastHit[] hits;
        private string resonwhy;
        private int ropePosLayerMask { get { return LayerMask.GetMask(Layers.ropePosLayer); } }
        private int obstacleLayerMask { get { return LayerMask.GetMask(Layers.obstacleLayer); } }
        private int ropeNodeLayerMask { get { return LayerMask.GetMask(Layers.ropeNodeLayer); } }
        private float hitDistence { get { return Config.hitDistence; } }
        private PickUpController pickCtrl { get { return ActionSystem.Instence.pickupCtrl; } }

        public RopeCtrl()
        {
            pickCtrl.onPickup += (OnPickUp);
            pickCtrl.onPickdown += (OnPickDown);
            pickCtrl.onPickStay += (OnPickStay);
            pickCtrl.onPickTwinceLeft += (OnPickTwince);
        }

        private void OnPickTwince(PickUpAbleItem arg0)
        {
           
        }

        private void OnPickStay(PickUpAbleItem arg0)
        {
           if(ropeSelected != null && ropeSelected.BindingTarget != null)
            {
                ropeSelected.BindingTarget.TryPlaceRope(ropeSelected);
            }
        }

        private void OnPickDown(PickUpAbleItem arg0)
        {
        }

        private void OnPickUp(PickUpAbleItem arg0)
        {
            if(arg0 is RopeItem){
                ropeSelected = arg0 as RopeItem;
            }
        }

        public override void Update()
        {
            if (ropeSelected == null) return;

            if (pickUpedRopeNode == null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                   TrySelectNode();
                }
            }
            else
            {
                RopeNodeMoveWithMouse(elementDistence += Input.GetAxis("Mouse ScrollWheel"));
                UpdateInstallRopeNode();
            }
        }
        private void TrySelectNode()
        {
            ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, hitDistence, ropeNodeLayerMask))
            {
                var obj = hit.collider.GetComponentInParent<RopeItem>();
                if (obj != null && obj.Active && obj.BindingTarget != null)//正在进行操作
                {
                    obj.BindingTarget.PickupCollider(hit.collider);
                    ropeSelected = obj;
                    pickUpedRopeNode = hit.collider;
                    Debug.Log("Select: " + pickUpedRopeNode);
                    elementDistence = Vector3.Distance(viewCamera.transform.position, pickUpedRopeNode.transform.position);
                }
            }
        }

        private void UpdateInstallRopeNode()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceNode();
            }
            else
            {
                ray = viewCamera.ScreenPointToRay(Input.mousePosition);
                hits = Physics.RaycastAll(ray, hitDistence, ropePosLayerMask);
                if (hits != null || hits.Length > 0)
                {
                    bool hited = false;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (pickUpedRopeNode == null) return;
                        var target = hits[i].collider.GetComponentInParent<RopeObj>();

                        if (hits[i].collider.name == pickUpedRopeNode.name && target == ropeTarget)
                        {
                            hited = true;
                            //ropeTarget = hits[i].collider.GetComponentInParent<RopeObj>();
                            pickDownAble = CanPlaceNode(ropeTarget, ropeSelected, pickUpedRopeNode, out resonwhy);
                        }
                    }
                    if (!hited)
                    {
                        pickDownAble = false;
                        resonwhy = "零件放置位置不正确";
                    }
                }
            }

        }
        private void RopeNodeMoveWithMouse(float distence)
        {
            disRay = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(disRay, out disHit, distence, obstacleLayerMask))
            {
                if (!ropeSelected.TryMoveToPos(pickUpedRopeNode, disHit.point))
                {
                    ropeTarget.PickDownCollider(pickUpedRopeNode);
                    pickUpedRopeNode = null;
                    //ropeSelected = null;
                }
            }
            else
            {
                var pos = disRay.GetPoint(elementDistence);
                if (!ropeSelected.TryMoveToPos(pickUpedRopeNode, pos))
                {
                    ropeTarget.PickDownCollider(pickUpedRopeNode);
                    pickUpedRopeNode = null;
                    //ropeSelected = null;
                }
            }
        }

        private void TryPlaceNode()
        {
            ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (pickDownAble)
            {
                PlaceNode(pickUpedRopeNode);
            }
            else
            {
                PlaceNodeWrong(ropeTarget, pickUpedRopeNode);
                userErr(resonwhy);
            }
            pickUpedRopeNode = null;
            //ropeSelected = null;
            pickDownAble = false;
        }

        private static bool CanPlaceNode(RopeObj ropeTarget, RopeItem ropeSelected, Collider collider, out string resonwhy)
        {
            resonwhy = null;
            if (ropeSelected.BindingTarget != ropeTarget)
            {
                resonwhy = "对象不匹配";
            }
            else if (ropeTarget == null)
            {
                resonwhy = "目标点父级没有挂RopeObj脚本";
            }
            else if (ropeTarget.Connected)
            {
                resonwhy = "目标点已经完成连接";
            }
            else if (!ropeTarget.CanInstallCollider(collider))
            {
                resonwhy = "坐标点已经占用";
            }
            return resonwhy == null;
        }

        private void PlaceNode(Collider collider)
        {
            Debug.Log("PlaceNode");
            ropeTarget.QuickInstallRopeItem(collider);
        }

        private void PlaceNodeWrong(RopeObj ropeItem, Collider collider)
        {
            Debug.Log("PlaceNodeWrong");
            ropeItem.PickDownCollider(collider);
        }
    }
}
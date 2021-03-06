﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System;

namespace WorldActionSystem
{
    public class AnimItem : AnimPlayer
    {
        public Animation anim;
        public string animName;
        private AnimationState state;
        private float animTime;
        private Coroutine coroutine;

        protected override void Awake()
        {
            base.Awake();
            if (anim == null)
                anim = GetComponentInChildren<Animation>();

            if (string.IsNullOrEmpty(animName))
                animName = anim.clip.name;
        }

        void Init()
        {
            gameObject.SetActive(true);
            anim.playAutomatically = false;
            anim.wrapMode = WrapMode.Once;
            RegisterEvent();
        }

        void RegisterEvent()
        {
            state = anim[animName];
            animTime = state.length;
            anim.cullingType = AnimationCullingType.AlwaysAnimate;
            anim.clip = anim.GetClip(animName);
        }


        IEnumerator DelyStop()
        {
            float waitTime = animTime / state.speed;
            yield return new WaitForSeconds(waitTime);
            onAutoPlayEnd.Invoke();
        }

        private void SetCurrentAnim(float time)
        {
            anim.clip = anim.GetClip(animName);
            state = anim[animName];
            state.normalizedTime = time;
            state.speed = 0;
            anim.Play();
        }

        public override void StepActive()
        {
            Init();
            state.normalizedTime = 0f;
            state.speed = duration;
            anim.Play();
            if (coroutine == null)
                coroutine = StartCoroutine(DelyStop());
        }

        public override void StepComplete()
        {
            SetCurrentAnim(1);
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = null;
        }

        public override void StepUnDo()
        {
            SetCurrentAnim(0);
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = null;
        }

        public override void SetPosition(Vector3 pos)
        {
            transform.position = pos;
        }

        public override void SetVisible(bool visible)
        {
            if (anim != null)
            {
                anim.gameObject.SetActive(visible);
            }
        }
    }
}
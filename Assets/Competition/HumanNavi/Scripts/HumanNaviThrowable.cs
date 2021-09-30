using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Valve.VR;
using Valve.VR.InteractionSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SIGVerse.Competition.HumanNavigation
{
	public class HumanNaviThrowable : Throwable
	{
		public bool useSIGVerseDefault = true;

		protected override void Awake()
		{
			if(this.useSIGVerseDefault)
			{
				this.SetSIGVerseDefault();
			}

			base.Awake();
		}

		private void SetSIGVerseDefault()
		{
//			this.attachmentFlags = Hand.AttachmentFlags.SnapOnAttach | Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.VelocityMovement | Hand.AttachmentFlags.TurnOffGravity;
			this.attachmentFlags = Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.VelocityMovement | Hand.AttachmentFlags.TurnOffGravity;

			this.releaseVelocityStyle = ReleaseStyle.AdvancedEstimation;
		}
		//protected override void OnHandHoverBegin(Hand hand)
		//{
		//	this.attachmentOffset = hand.transform; // If this line set in OnAttachedToHand, only the first grasp will have unnatural behavior.

		//	base.OnHandHoverBegin(hand);
		//}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(HumanNaviThrowable))]
	public class HumanNaviThrowableEditor : Editor
	{
		private HumanNaviThrowable humanNaviThrowable;

		private void Awake()
		{
			this.humanNaviThrowable = (HumanNaviThrowable)target;
		}

		public override void OnInspectorGUI()
		{
			HumanNaviThrowable humanNaviThrowable = (HumanNaviThrowable)target;

			this.humanNaviThrowable.useSIGVerseDefault = EditorGUILayout.ToggleLeft("Use SIGVerse Default", this.humanNaviThrowable.useSIGVerseDefault);

			if(!humanNaviThrowable.useSIGVerseDefault)
			{
				base.OnInspectorGUI();
			}
		}
	}
#endif
}


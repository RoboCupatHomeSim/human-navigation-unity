using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Valve.VR;

namespace SIGVerse.Competition.HumanNavigation
{
	public class GraspingMotionManager : MonoBehaviour
	{
		public enum HandType
		{
			LeftHand,
			RightHand,
		}

		[HeaderAttribute("Bones for visualize")]
		public HandType  handType;


		private SteamVR_Input_Sources inputSource;

		// for Grasping
		private Transform thumb1, index1, middle1, ring1, pinky1;
		private Transform thumb2, index2, middle2, ring2, pinky2;
		private Transform thumb3, index3, middle3, ring3, pinky3;
		private Transform thumb4, index4, middle4, ring4, pinky4;

		private Vector3 thumb1Pos , thumb2Pos , thumb3Pos , thumb4Pos;
		private Vector3 index1Pos , index2Pos , index3Pos , index4Pos;
		private Vector3 middle1Pos, middle2Pos, middle3Pos, middle4Pos;
		private Vector3 ring1Pos  , ring2Pos  , ring3Pos  , ring4Pos;
		private Vector3 pinky1Pos , pinky2Pos , pinky3Pos , pinky4Pos;

		private Quaternion thumb1Rot , thumb2Rot , thumb3Rot;
		private Quaternion index1Rot , index2Rot , index3Rot;
		private Quaternion middle1Rot, middle2Rot, middle3Rot;
		private Quaternion ring1Rot  , ring2Rot  , ring3Rot;
		private Quaternion pinky1Rot , pinky2Rot , pinky3Rot;

		private Quaternion thumb1Start , thumb1End , thumb2Start , thumb2End , thumb3Start , thumb3End;
		private Quaternion index1Start , index1End , index2Start , index2End , index3Start , index3End;
		private Quaternion middle1Start, middle1End, middle2Start, middle2End, middle3Start, middle3End;
		private Quaternion ring1Start  , ring1End  , ring2Start  , ring2End  , ring3Start  , ring3End;
		private Quaternion pinky1Start , pinky1End , pinky2Start , pinky2End , pinky3Start , pinky3End;

		void OnEnable()
		{
			// for Grasping
			string typeStr = (this.handType == HandType.LeftHand)? "Left" : "Right";

			this.thumb1  = this.transform.Find("Ethan"+typeStr+"HandThumb1");
			this.thumb2  = this.transform.Find("Ethan"+typeStr+"HandThumb1/Ethan"+typeStr+"HandThumb2");  
			this.thumb3  = this.transform.Find("Ethan"+typeStr+"HandThumb1/Ethan"+typeStr+"HandThumb2/Ethan"+typeStr+"HandThumb3");
			this.thumb4  = this.transform.Find("Ethan"+typeStr+"HandThumb1/Ethan"+typeStr+"HandThumb2/Ethan"+typeStr+"HandThumb3/Ethan"+typeStr+"HandThumb4");

			this.index1  = this.transform.Find("Ethan"+typeStr+"HandIndex1");
			this.index2  = this.transform.Find("Ethan"+typeStr+"HandIndex1/Ethan"+typeStr+"HandIndex2");  
			this.index3  = this.transform.Find("Ethan"+typeStr+"HandIndex1/Ethan"+typeStr+"HandIndex2/Ethan"+typeStr+"HandIndex3");
			this.index4  = this.transform.Find("Ethan"+typeStr+"HandIndex1/Ethan"+typeStr+"HandIndex2/Ethan"+typeStr+"HandIndex3/Ethan"+typeStr+"HandIndex4");

			this.middle1 = this.transform.Find("Ethan"+typeStr+"HandMiddle1");
			this.middle2 = this.transform.Find("Ethan"+typeStr+"HandMiddle1/Ethan"+typeStr+"HandMiddle2");  
			this.middle3 = this.transform.Find("Ethan"+typeStr+"HandMiddle1/Ethan"+typeStr+"HandMiddle2/Ethan"+typeStr+"HandMiddle3");
			this.middle4 = this.transform.Find("Ethan"+typeStr+"HandMiddle1/Ethan"+typeStr+"HandMiddle2/Ethan"+typeStr+"HandMiddle3/Ethan"+typeStr+"HandMiddle4");

			this.ring1   = this.transform.Find("Ethan"+typeStr+"HandRing1");
			this.ring2   = this.transform.Find("Ethan"+typeStr+"HandRing1/Ethan"+typeStr+"HandRing2");  
			this.ring3   = this.transform.Find("Ethan"+typeStr+"HandRing1/Ethan"+typeStr+"HandRing2/Ethan"+typeStr+"HandRing3");
			this.ring4   = this.transform.Find("Ethan"+typeStr+"HandRing1/Ethan"+typeStr+"HandRing2/Ethan"+typeStr+"HandRing3/Ethan"+typeStr+"HandRing4");

			this.pinky1  = this.transform.Find("Ethan"+typeStr+"HandPinky1");
			this.pinky2  = this.transform.Find("Ethan"+typeStr+"HandPinky1/Ethan"+typeStr+"HandPinky2");  
			this.pinky3  = this.transform.Find("Ethan"+typeStr+"HandPinky1/Ethan"+typeStr+"HandPinky2/Ethan"+typeStr+"HandPinky3");  
			this.pinky4  = this.transform.Find("Ethan"+typeStr+"HandPinky1/Ethan"+typeStr+"HandPinky2/Ethan"+typeStr+"HandPinky3/Ethan"+typeStr+"HandPinky4");  
		}


		// Use this for initialization
		void Start()
		{
			if(this.handType==HandType.LeftHand) { this.inputSource = SteamVR_Input_Sources.LeftHand; }
			if(this.handType==HandType.RightHand){ this.inputSource = SteamVR_Input_Sources.RightHand; }

			this.thumb1Pos  = this.thumb1 .localPosition;  this.thumb2Pos  = this.thumb2 .localPosition;  this.thumb3Pos  = this.thumb3 .localPosition;  this.thumb4Pos  = this.thumb4 .localPosition;
			this.index1Pos  = this.index1 .localPosition;  this.index2Pos  = this.index2 .localPosition;  this.index3Pos  = this.index3 .localPosition;  this.index4Pos  = this.index4 .localPosition;
			this.middle1Pos = this.middle1.localPosition;  this.middle2Pos = this.middle2.localPosition;  this.middle3Pos = this.middle3.localPosition;  this.middle4Pos = this.middle4.localPosition;
			this.ring1Pos   = this.ring1  .localPosition;  this.ring2Pos   = this.ring2  .localPosition;  this.ring3Pos   = this.ring3  .localPosition;  this.ring4Pos   = this.ring4  .localPosition;
			this.pinky1Pos  = this.pinky1 .localPosition;  this.pinky2Pos  = this.pinky2 .localPosition;  this.pinky3Pos  = this.pinky3 .localPosition;  this.pinky4Pos  = this.pinky4 .localPosition;


			// for Grasping
			this.thumb1Start  = this.thumb1 .localRotation;  this.thumb2Start  = this.thumb2 .localRotation;  this.thumb3Start  = this.thumb3 .localRotation;
			this.index1Start  = this.index1 .localRotation;  this.index2Start  = this.index2 .localRotation;  this.index3Start  = this.index3 .localRotation;
			this.middle1Start = this.middle1.localRotation;  this.middle2Start = this.middle2.localRotation;  this.middle3Start = this.middle3.localRotation;
			this.ring1Start   = this.ring1  .localRotation;  this.ring2Start   = this.ring2  .localRotation;  this.ring3Start   = this.ring3  .localRotation;
			this.pinky1Start  = this.pinky1 .localRotation;  this.pinky2Start  = this.pinky2 .localRotation;  this.pinky3Start  = this.pinky3 .localRotation;

			float xySign = (this.handType == HandType.LeftHand)? 1.0f : -1.0f;

			//this.thumb1End  = Quaternion.Euler(xySign * (+49.32f), xySign * (-125.52f), -39.94f);
			//this.index1End  = Quaternion.Euler(xySign * (+ 8.43f), xySign * (- 28.12f), +28.70f);
			//this.middle1End = Quaternion.Euler(xySign * (- 5.20f), xySign * (-  4.96f), +23.35f);
			//this.middle2End = Quaternion.Euler(xySign * (- 1.57f), xySign * (- 10.52f), - 8.52f);
			//this.ring1End   = Quaternion.Euler(xySign * (+ 2.71f), xySign * (- 13.94f), +19.27f);
			//this.pinky1End  = Quaternion.Euler(xySign * (+14.85f), xySign * (- 11.87f), - 8.14f);

			this.thumb1End  = Quaternion.Euler(xySign * (+57.448f), xySign * (-123.113f), -43.472f);
			this.thumb2End  = Quaternion.Euler(xySign * (-1.708f),  xySign * (-4.082f),   +0.989f);
			this.thumb3End  = Quaternion.Euler(xySign * (+13.376f), xySign * (-11.068f), -28.712f);

			//this.index1End  = Quaternion.Euler(xySign * (+ 5.809f), xySign * (- 29.475f), +27.081f);
			//this.index2End  = Quaternion.Euler(xySign * (+ 0.0f), xySign * (- 0.0f), -5.299f);
			this.index1End  = Quaternion.Euler(xySign * (+ 8.43f), xySign * (- 28.12f), +28.70f);
			this.index2End  = this.index2Start;
			this.index3End  = this.index3Start;

			this.middle1End = Quaternion.Euler(xySign * (- 5.20f), xySign * (-  4.96f), +24.382f);
			this.middle2End = Quaternion.Euler(xySign * (- 1.57f), xySign * (- 10.52f), + 1.004f);
			this.middle3End = this.middle3Start;

			this.ring1End   = Quaternion.Euler(xySign * (+ 8.372001f), xySign * (- 10.528f), +19.901f);
			this.ring2End   = this.ring2Start;
			this.ring3End   = this.ring3Start;

			this.pinky1End  = Quaternion.Euler(xySign * (+13.613f), xySign * (+ 2.055f), +2.769f);
			this.pinky2End  = this.pinky2Start;
			this.pinky3End  = this.pinky3Start;
		}


		void LateUpdate()
		{
			float handTrigger1D = this.GetHandTrigger1D();

			this.thumb1 .localPosition = this.thumb1Pos;  this.thumb2 .localPosition = this.thumb2Pos;  this.thumb3 .localPosition = this.thumb3Pos;  this.thumb4 .localPosition = this.thumb4Pos;
			this.index1 .localPosition = this.index1Pos;  this.index2 .localPosition = this.index2Pos;  this.index3 .localPosition = this.index3Pos;  this.index4 .localPosition = this.index4Pos;
			this.middle1.localPosition = this.middle1Pos; this.middle2.localPosition = this.middle2Pos; this.middle3.localPosition = this.middle3Pos; this.middle4.localPosition = this.middle4Pos;
			this.ring1  .localPosition = this.ring1Pos;   this.ring2  .localPosition = this.ring2Pos;   this.ring3  .localPosition = this.ring3Pos;   this.ring4  .localPosition = this.ring4Pos;
			this.pinky1 .localPosition = this.pinky1Pos;  this.pinky2 .localPosition = this.pinky2Pos;  this.pinky3 .localPosition = this.pinky3Pos;  this.pinky4 .localPosition = this.pinky4Pos;

			this.thumb1Rot = Quaternion.Slerp(this.thumb1Start, this.thumb1End, handTrigger1D);
			this.thumb2Rot = Quaternion.Slerp(this.thumb2Start, this.thumb2End, handTrigger1D);
			this.thumb3Rot = Quaternion.Slerp(this.thumb3Start, this.thumb3End, handTrigger1D);

			this.index1Rot = Quaternion.Slerp(this.index1Start, this.index1End, handTrigger1D);
			this.index2Rot = Quaternion.Slerp(this.index2Start, this.index2End, handTrigger1D);
			this.index3Rot = Quaternion.Slerp(this.index3Start, this.index3End, handTrigger1D);

			this.middle1Rot = Quaternion.Slerp(this.middle1Start, this.middle1End, handTrigger1D);
			this.middle2Rot = Quaternion.Slerp(this.middle2Start, this.middle2End, handTrigger1D);
			this.middle3Rot = Quaternion.Slerp(this.middle3Start, this.middle3End, handTrigger1D);

			this.ring1Rot = Quaternion.Slerp(this.ring1Start, this.ring1End, handTrigger1D);
			this.ring2Rot = Quaternion.Slerp(this.ring2Start, this.ring2End, handTrigger1D);
			this.ring3Rot = Quaternion.Slerp(this.ring3Start, this.ring3End, handTrigger1D);

			this.pinky1Rot = Quaternion.Slerp(this.pinky1Start, this.pinky1End, handTrigger1D);
			this.pinky2Rot = Quaternion.Slerp(this.pinky2Start, this.pinky2End, handTrigger1D);
			this.pinky3Rot = Quaternion.Slerp(this.pinky3Start, this.pinky3End, handTrigger1D);


			this.thumb1 .localRotation = this.thumb1Rot;  this.thumb2 .localRotation = this.thumb2Rot;  this.thumb3 .localRotation = this.thumb3Rot;
			this.index1 .localRotation = this.index1Rot;  this.index2 .localRotation = this.index2Rot;  this.index3 .localRotation = this.index3Rot;
			this.middle1.localRotation = this.middle1Rot; this.middle2.localRotation = this.middle2Rot; this.middle3.localRotation = this.middle3Rot;
			this.ring1  .localRotation = this.ring1Rot;   this.ring2  .localRotation = this.ring2Rot;   this.ring3  .localRotation = this.ring3Rot;
			this.pinky1 .localRotation = this.pinky1Rot;  this.pinky2 .localRotation = this.pinky2Rot;  this.pinky3 .localRotation = this.pinky3Rot;
		}


		public float GetHandTrigger1D()
		{
			float handTrigger1D = 0.0f;

//			handTrigger1D = (this.handType == HandType.LeftHand)? OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) : OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);
			handTrigger1D = SteamVR_Actions.sigverse.SqueezeMiddle.GetAxis(this.inputSource);

//			Debug.LogError("handTrigger1D="+handTrigger1D);

			return handTrigger1D;
		}
	}
}

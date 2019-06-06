using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets_1_1_2.CrossPlatformInput;

#pragma warning disable CS0414

public class HumanNaviAvatarController : MonoBehaviour
{
	public float walkSpeed = 0.01f;
	public float turnSpeed = 0.25f;

	public Transform eyeAnchor;
	public Transform bodyAnchor;

	public GameObject bodyWithIK;
	public GameObject bodyWithAnimation;

#if ENABLE_VRIK
	private RootMotion.FinalIK.VRIK vrik;
#endif

	private Vector3 targetPos;

	private bool isInitializing = false;

	// Use this for initialization
	void Start()
	{
		this.targetPos = this.transform.localPosition;

#if ENABLE_VRIK
		this.vrik = this.bodyWithIK.GetComponent<RootMotion.FinalIK.VRIK>();
#endif

	}

	// Update is called once per frame
	void Update()
	{
		Vector2 l_thumb_stick;
		l_thumb_stick.x = CrossPlatformInputManager.GetAxis("Horizontal");
		l_thumb_stick.y = CrossPlatformInputManager.GetAxis("Vertical");

		if (Vector2.SqrMagnitude(l_thumb_stick) > 0.01)
		{
#if ENABLE_VRIK
			if (this.vrik != null)
			{
				this.vrik.solver.leftLeg.positionWeight = 1.0f;
				this.vrik.solver.rightLeg.positionWeight = 1.0f;
			}
#endif
			Vector3 virtical = Vector3.Scale(eyeAnchor.forward * l_thumb_stick.y, new Vector3(1, 0, 1));
			Vector3 horizontal = Vector3.Scale(eyeAnchor.right * l_thumb_stick.x, new Vector3(1, 0, 1));

			this.targetPos += (virtical + horizontal) * walkSpeed;

			this.transform.localPosition += (virtical + horizontal) * Time.deltaTime * walkSpeed;
		}
		else
		{
#if ENABLE_VRIK
			if (this.vrik != null && !this.isInitializing)
			{
				this.vrik.solver.leftLeg.positionWeight = 0.0f;
				this.vrik.solver.rightLeg.positionWeight = 0.0f;
			}
#endif
		}

	}

	public void StartInitializing()
	{
		base.StartCoroutine(this.OnInitializing());
	}

	private IEnumerator OnInitializing(float waitTime = 1.0f)
	{
		this.isInitializing = true;
#if ENABLE_VRIK
		this.vrik.solver.leftLeg.positionWeight = 1.0f;
		this.vrik.solver.rightLeg.positionWeight = 1.0f;
#endif

		yield return new WaitForSeconds(waitTime);

		this.isInitializing = false;
	}
}

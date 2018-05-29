using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncAvatarRotation : MonoBehaviour
{
	public GameObject target;

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		this.transform.rotation = Quaternion.AngleAxis(target.transform.rotation.eulerAngles.y, new Vector3(0, 1, 0));
	}
}

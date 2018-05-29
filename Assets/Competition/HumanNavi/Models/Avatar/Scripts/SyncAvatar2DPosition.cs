using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncAvatar2DPosition : MonoBehaviour
{
	public GameObject target;
	public Vector3 offset;

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		this.transform.position = Vector3.Scale(target.transform.position, new Vector3(1, 0, 1)) + this.offset;
	}
}

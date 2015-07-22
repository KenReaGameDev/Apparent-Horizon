using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FleetLeader {
	
	private AiShip ship;
	List<AiShip> fleet = new List<AiShip>();
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	public void Update () {
		
	}
	
	public  void DetermineTarget()
	{
		// if Target danger is > than self danger, send fleet wide attack order.
		// else fight solo.
	}

	public  void DetermineSpeed ()
	{
		
	}

}

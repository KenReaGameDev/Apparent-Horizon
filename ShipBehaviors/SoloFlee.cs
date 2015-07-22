using UnityEngine;
using System.Collections;

public class SoloFlee : AiBehavior {

	bool hasAggressors = false;
	BeaconScript beaconScript;
	Vector3 fleeDirection;
	
	public SoloFlee(Ship inShip)
	{
		ship = inShip;
		if (ship.Aggressors.Count > 0)
			hasAggressors = true;
			
		// Creates a random direction to flee in.
		RandomPsuedoUpVector();
		ship.photonView.RPC("SoloFleeRPC", PhotonTargets.Others, fleeDirection);
	}

	// Update is called once per frame
	protected override void Update () {
	
		if (ship.Aggressors.Count == 0)
			SwitchToRepair();
			
		DetermineSpeed();
	}
	
	protected override void WorkUpdate()
	{
		
	}
	
	protected override void WaitUpdate()
	{
		
	}

	public override void DetermineTarget()
	{

	}
	
	public override void DetermineSpeed ()
	{
		float boost = (ship.health * 5) / ship.maxHealth;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( psuedoUpVector ), Time.deltaTime );
		ship.rigidbody.AddForce(ship.transform.forward * ship.GetAttributes().maxAcceleration * boost);		
	}
	
	public override void RequestBehavior(int PlayerID)
	{		
		if (PlayerID > -1)
			ship.photonView.RPC("SoloFleeRPC", PhotonPlayer.Find(PlayerID), psuedoUpVector);
		else
			ship.photonView.RPC("SoloFleeRPC", PhotonTargets.Others, psuedoUpVector);
	}
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		hasAggressors = false;
		Attributes subjectStats = subject.getAttributes();
		
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();	

		if (subject.Aggressors.Count > 0)
			hasAggressors = true;
		
		// If still too close or too damaged, keep running.
		if (health < 35 && hasAggressors)
			return true;
		
		return false;
	}


	void SwitchToRepair()
	{
		ship.behavior = new Repair(ship);
	}

}

using UnityEngine;
using System.Collections;

public class SoloEngage : AiBehavior {
	
	PositionPrediction predictTarget;
	// Use this for initialization
	public SoloEngage(Ship inShip)
	{
		ship = inShip;
		if (ship == null || ship.target == null)
		{
			return;
		}
		leader = inShip.getFleetLeader();
		predictTarget = new PositionPrediction(inShip);
		RandomPsuedoUpVector();
		if(ship.photonView.isMine)
			ship.photonView.RPC("SoloEngageRPC", PhotonTargets.Others, psuedoUpVector, ship.target.GetComponent<PhotonView>().viewID);
		
	}


	// Update is called once per frame
	protected override void Update () {
			
		UpdateTime();
		if (ship.target == null)
			DetermineTarget();		

		try
		{				
			ship.GetAttributes().fireRate += Time.deltaTime;
			deltaUpdateDistance += Time.deltaTime;
			deltaOrbitUpdate += Time.deltaTime;				
	
			//predictTarget.Update();
			//FireWeapon();		
			DetermineSpeed();		
		}
		catch
		{
			if (ship == null)
				return;
			
			if (ship.target == null)
				DetermineTarget();			
		}
	}
	
	protected override void WorkUpdate()
	{
		
	}
	
	protected override void WaitUpdate()
	{
		
	}


	public override void DetermineTarget()
	{
		float health = (ship.GetAttributes().structureHealth * 100) / ship.GetAttributes().maxStructureHealth;		
		if (health > 25)
			RequestTargetChange();
		else
			leader.determiner.RequestBehaviorChange(ship);
	}
	
	public override void DetermineSpeed ()
	{

		if (distance > ship.attributes.range * 2 && ship.target != null)
			Approach();
		else
			Orbit();				
	}
	
	public override void RequestBehavior(int PlayerID)
	{		
		if(PlayerID > -1)
			ship.photonView.RPC("SoloEngageRPC", PhotonPlayer.Find(PlayerID), psuedoUpVector, ship.target.GetComponent<PhotonView>().viewID);
		else
			ship.photonView.RPC("SoloEngageRPC", PhotonTargets.Others, psuedoUpVector, ship.target.GetComponent<PhotonView>().viewID);
	}
	
	void Orbit()
	{
		Vector3 targetDirection = ship.target.position - ship.transform.position;
		distance = targetDirection.magnitude;
		Vector3 orbitDirection = Vector3.Cross(targetDirection, psuedoUpVector);
		float angleChange = Vector3.Angle(ship.transform.forward, orbitDirection);
		float change = ship.degreesPerSecond / angleChange;
		change *= Time.deltaTime;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( orbitDirection ), change );
		ship.rigidbody.AddForce(ship.transform.forward * ship.attributes.maxAcceleration );
	}
	
	void Approach()
	{
		Vector3 targetDirection = ship.target.position - ship.transform.position;
		distance = targetDirection.magnitude;
		float angleChange = Vector3.Angle(ship.transform.forward, targetDirection);
		float change = ship.degreesPerSecond / angleChange;
		change *= Time.deltaTime;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( targetDirection ), change );
		float boost = (distance * 10) / 20000;
		ship.rigidbody.AddForce(ship.transform.forward * ship.attributes.maxAcceleration * boost);
	}

	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		// Optimize -- Make sure there is a target first.
		if (subject.GetTarget() == null)
			return false;
			
		Attributes subjectStats = subject.getAttributes();
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();

		//Debug.Log("MAYBE TRUE");	
		// comparison of factions may not work, check later.
		if (health >= 15)
			return true;
		
		Debug.Log("SOLO FALSE");
		return false;
	}
	
	void RequestTargetChange()
	{
		if (requestWaitTimer < 5)
			return;

		leader.determiner.RequestTargetChange(ship);
		requestWaitTimer = 0;
	}

}

using UnityEngine;
using System.Collections;

public class TeamEngage : AiBehavior {
	

	float distance;
	PositionPrediction predictTarget;
	
	float deltaUpdateDistance = 0;
	float deltaOrbitUpdate = 1;
	
	Vector3 orbitDirection;
	public TeamEngage(Ship inShip)
	{
		ship = inShip;
		leader = inShip.getFleetLeader();
		
		if (ship != leader)
			ship.SetTargetTransform(leader.GetTarget());
			
		predictTarget = new PositionPrediction(inShip);
	
		if (leader.target == null)
		{
			LeaderChecks();
		}
		
		if(ship.photonView.isMine && leader.target != null)
		{
			RandomPsuedoUpVector();
			ship.photonView.RPC("TeamEngageRPC", PhotonTargets.Others, psuedoUpVector, ship.fleetLeader.target.GetComponent<PhotonView>().viewID);
		}
	}
	// Use this for initialization

	
	// Update is called once per frame
	protected override void Update () {
		
		LeaderChecks();
		
		if (ship.target == null)
		{
			ship.SetBehavior(new Regroup(ship));
		}
		
		if (leader == null || leader.GetTarget() == null || leader.targetFleet == null || leader.targetFleet.fleetLeader == null)
		{
			Debug.LogWarning("returning before team engage can work");
			return;
			
		}
		
		try
		{
			// Place Holder -- Bug determining Code.
			if (leader.target.gameObject != leader.targetFleet.fleet[0].gameObject)
				leader.target = leader.targetFleet.fleet[0].transform;
		}
		catch
		{
			if (leader == null)
				Debug.Log("Leader for Ship: " + ship.gameObject.name + " is Null");
			
			if (leader.target.gameObject == null)
				Debug.Log("Leader Targer GO is Null");
			
			if (leader.targetFleet == null)
			{
				Debug.Log("Target Fleet is Null");
				leader.target = null;
				return;
			}
			
			if (leader.targetFleet.isEmpty)
			{
				leader.targetFleet = null;
				leader.target = null;
				return;
			}
			
			if (leader.targetFleet.fleetLeader == null)
				Debug.Log("Target Fleet Fleet Leader is Null");
		}
		
		//predictTarget.Update();		
		ship.GetAttributes().fireRate += Time.deltaTime;
		deltaUpdateDistance += Time.deltaTime;
		deltaOrbitUpdate += Time.deltaTime;
		
		if (deltaUpdateDistance > 10 && ship.isFleetLeader)
			TargetOpportunityCheck();
			
		if (ship.target == null && ship != leader)
			ship.SetTargetTransform(leader.GetTarget());
		
			
		if (leader == null)
				return;	
		
		DetermineSpeed();
	}
	
	void LeaderChecks()
	{		
		// Leader has to have target.
		if (ship.isFleetLeader && ship.target == null)
		{
		
			if (leader.targetFleet == null)
			{
				ship.SetBehavior(new DoNothing(ship));
				return;
			}
			
			if (leader.targetFleet.fleet.Count != 0)
			{
				ship.SetTargetShip(leader.targetFleet.fleet[0]); 
				return;
			}
			
			if (leader.targetFleet.pFleet.Count != 0)
			{
				ship.SetTargetShip(leader.targetFleet.pFleet[0]); 
				return;
			}
			
			ship.SetBehavior(new DoNothing(ship));
			return;
		}
	}
	
	void TargetOpportunityCheck()
	{	
		if (ship.target == null)
			return;
				
		if (Vector3.Distance(ship.target.position, ship.transform.position) > 15000)
		{
			foreach (GameObject go in ship.Aggressors.Keys)
			{
				if (Vector3.Distance(go.transform.position, ship.transform.position) < 15000)
				{
					ship.SetTargetShip(go.GetComponent<Ship>());					
					break;
				}
			}
		}
		
		deltaUpdateDistance = 0;	
	}
	
	protected override void WorkUpdate()
	{
		
	}
	
	protected override void WaitUpdate()
	{
		
	}

	public override void DetermineTarget()
	{
		// if Target danger is > than self danger, send fleet wide attack order.
		// else fight solo.
	}
	
	public override void DetermineSpeed ()
	{
		
		if (distance > ship.GetAttributes().range * 0.4f && ship.target != null)
			Approach();
		else
			Orbit();
		
	}
	
	public override void RequestBehavior(int PlayerID)
	{
		if (PlayerID > -1)
			ship.photonView.RPC("TeamEngageRPC", PhotonPlayer.Find(PlayerID), psuedoUpVector, ship.fleetLeader.target.GetComponent<PhotonView>().viewID);
		else
			ship.photonView.RPC("TeamEngageRPC", PhotonTargets.Others, psuedoUpVector, ship.fleetLeader.target.GetComponent<PhotonView>().viewID);
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
		ship.rigidbody.angularDrag = 3.0f;
	}
	
	void Approach()
	{
		Vector3 targetDirection = ship.target.position - ship.transform.position;
		distance = targetDirection.magnitude;
		float angleChange = Vector3.Angle(ship.transform.forward, targetDirection);
		float change = ship.degreesPerSecond / angleChange;
		float boost = (distance * 10) / 20000;
		change *= Time.deltaTime;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( targetDirection ), change );
		ship.rigidbody.AddForce(ship.transform.forward * ship.attributes.maxAcceleration * boost);
		ship.rigidbody.angularDrag = 1.0f;
	}

	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		try
		{
			// Optimize -- Make sure the target is the same as the leaders.
			if (leader.GetTarget() == null)
				return false;
			
			if (leader.GetTarget().gameObject.name != subject.GetTarget().gameObject.name)
				return false;
				
			Attributes subjectStats = subject.getAttributes();
			// Get Percent / 100% using cross multiply
			double health = GetCurrentHealthPercentage();		
			// comparison of factions may not work, check later.
			if (health >= 25 && leader.GetTarget() != null)
				return true;
		}
		catch
		{
			if (!subject)
				return false;
			
			if (subject.GetMyFleet().fleetLeader is PlayerShip)
				return false;
			
			if (!leader)
				leader = (AiShip)subject.GetMyFleet().fleetLeader;
		}
		
		return false;
	}

}

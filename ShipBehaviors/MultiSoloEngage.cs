using UnityEngine;
using System.Collections;

//For ships that have multiple Weapon Systems.
// has own determine target functionality for each weapon.
// can target entire fleets at once.

public class MultiSoloEngage : AiBehavior {
	
	Ship leader;
	Ship ship;
	float distance;
	PositionPrediction predictTarget;
	
	float deltaUpdateDistance = 0;
	float deltaOrbitUpdate = 1;
	
	Vector3 orbitDirection;
	// Use this for initialization
	public MultiSoloEngage(Ship inShip)
	{
		ship = inShip;
		leader = inShip.getFleetLeader();
		predictTarget = new PositionPrediction(inShip);
		
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
			if (deltaUpdateDistance > 3)
			{
				distance = Vector3.Distance(ship.target.position, ship.transform.position);
				deltaUpdateDistance = 0;
			}			
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

		//  TODO: 
		// Determine movement type based on ships maximum speed.
		// This will essentially be the ships preferred combat movement.
		// EG: Dreadnaughts will not orbit target but remain in a position and
		// rotate to best execute their attacks.

		if (distance > 8000)
			ApproachTarget();
		else
			OrbitTarget();				
	}
	
	public override void RequestBehavior(int PlayerID)
	{		
		
		if (PlayerID > -1)
			ship.photonView.RPC("MultiSoloEngageRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("MultiSoloEngageRPC", PhotonTargets.Others, null);
	}
	
	// Determine each weapons target.
	void DetermineTargetWeapons()
	{
		
	}

	void OrbitTarget()
	{
		if (deltaOrbitUpdate > 1)
		{
			// Get the perpendicular to an approach path.
			Vector3 dir = ship.transform.position - ship.target.position;
			dir.Normalize();
			orbitDirection = Vector3.Cross(dir, Vector3.up);
			deltaOrbitUpdate = 0;
		}
		
		float forceNeeded = ship.GetMyFleet().minimumAccel * ship.rigidbody.mass;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( orbitDirection - ship.transform.position ), Time.deltaTime );
		//ship.transform.LookAt(orbitDirection);
		ship.rigidbody.AddForce(orbitDirection * forceNeeded);
	}
	
	void ApproachTarget()
	{
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( ship.GetTarget().position - ship.transform.position ), Time.deltaTime );;
		float minAccell = ship.GetMyFleet().minimumAccel;
		float forceNeeded = ship.GetMyFleet().minimumAccel * ship.rigidbody.mass;
		Vector3 direction = ship.target.transform.position - ship.rigidbody.transform.position;
		float distance = direction.magnitude;
		direction = direction / distance;
		direction = Vector3.Normalize(direction);
		ship.rigidbody.AddForce(direction * forceNeeded);		
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

using UnityEngine;
using System.Collections;

public class SearchFollow : AiBehavior {
	
	private Ship ship;
	Transform target;
	
	// Use this for initialization
	public SearchFollow(Ship inShip)
	{
//		if (ship.isFleetLeader)
//		{
//			//Debug.Log("Is Fleet Leader");
//			//Debug.LogWarning("Hot Swapping Fleet Leader in SearchFollow");
//			ship.SetBehavior(new Search(ship));
//			return;			
//		}
		
		////Debug.Log(inShip.gameObject.name + " is now following " + ship.getFleetLeader().gameObject.name);
		
		ship = inShip;
		
		//Debug.Log("Setting Target, Should be fleet leader.");
		target = ship.getFleetLeader().transform;
		
		//Debug.Log("Sending RPC to Searchfollow");
		
		if(ship.photonView.isMine)
			ship.photonView.RPC("SearchFollowRPC", PhotonTargets.Others, null);
	}
	
	// Update is called once per frame
	protected override void Update () 
	{
		
		//Debug.Log("Checking if fleet leader");
		if (ship.isFleetLeader)
		{
			//Debug.Log("Is Fleet Leader");
			//Debug.LogWarning("Hot Swapping Fleet Leader in SearchFollow");
			ship.SetBehavior(new Search(ship));
		}
		
		//Debug.Log("Updating Time");
		UpdateTime();
		
		//Debug.Log("If not Fleet Leader Determine Speed");
		if (ship.fleetLeader != null)
			DetermineSpeed();
			
		//ship.recentCommands.Add(debugString);
		// If Target != Leader. Make leader target;
	}
	
	protected override void WorkUpdate()
	{
		
	}
	
	protected override void WaitUpdate()
	{
		
	}

	public override void DetermineTarget()
	{
		target = ship.getFleetLeader().getShipObject().transform;
		//Debug.Log("Target Determined");
	}
	
	public override void DetermineSpeed ()
	{
		//Debug.Log("Determining Speed");
		if (distance > orbitRange && ship.target != null)
			Approach();
		else
			ApproachNoBoost();
	}
	
	void Approach()
	{
		//Debug.Log("Approaching");
		Vector3 targetDirection = ship.fleetLeader.transform.position - ship.transform.position;
		distance = targetDirection.magnitude;
		float angleChange = Vector3.Angle(ship.transform.forward, targetDirection);
		float change = ship.degreesPerSecond / angleChange;
		float boost = (distance * 10) / 20000;
		change *= Time.deltaTime;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( targetDirection ), change );
		ship.rigidbody.AddForce(ship.transform.forward * ship.attributes.maxAcceleration * boost);
	}
	
	void ApproachNoBoost()
	{
		//Debug.Log("Approaching No Boost");
		Vector3 targetDirection = ship.fleetLeader.transform.position - ship.transform.position;
		distance = targetDirection.magnitude;
		float angleChange = Vector3.Angle(ship.transform.forward, targetDirection);
		float change = ship.degreesPerSecond / angleChange;
		change *= Time.deltaTime;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( targetDirection ), change );
		ship.rigidbody.AddForce(ship.transform.forward * ship.attributes.maxAcceleration);
	}
	
	public override void RequestBehavior(int PlayerID)
	{		
		if (PlayerID > -1)
			ship.photonView.RPC("SearchFollowRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("SearchFollowRPC", PhotonTargets.Others, null);
	}
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		// Make sure leader is searching.
		//Object leaderBehavior = leader.GetBehavior();
		//Object SearchBehavior = new Search(null);
		if (ship.getFleetLeader().GetBehavior() is Search)
			return true;

		//subject.SetBehavior(new DoNothing());
		return false;
	}


}

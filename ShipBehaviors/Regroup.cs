using UnityEngine;
using System.Collections;

public class Regroup : AiBehavior {
	
	Transform target;
	
	// Use this for initialization
	public Regroup(Ship inShip)
	{
		ship = inShip;
		target = ship.fleetLeader.transform;
		if(ship.photonView.isMine)
			ship.photonView.RPC("RegroupRPC", PhotonTargets.Others, null);
	}
	
	// Update is called once per frame
	protected override void Update () {
		if (target == ship.transform)
			return;
		
		if (!target)
		{
			target = ship.fleetLeader.transform;
		}
		
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
	
	public override void RequestBehavior(int PlayerID)
	{	
		if (PlayerID > -1)
			ship.photonView.RPC("RegroupRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("RegroupRPC", PhotonTargets.Others, null);
	}
	
	public override void DetermineSpeed ()
	{	
		Vector3 direction = target.position - ship.transform.position;
		if (direction != Vector3.zero)
			ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime );
		ship.rigidbody.AddForce(ship.transform.forward * ship.GetAttributes().maxAcceleration * 10);		
	}
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		// Make sure the ship hasn't become the faction leader. 
		if (leader.ID == subject.ID)
			return false;
			
		Attributes subjectStats = subject.getAttributes();
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();	
		float distance  = Vector3.Magnitude(subject.gameObject.transform.position - leader.gameObject.transform.position);
		
		// If still strong enough to fight and is far away from leader, regroup.
		if (health >= 25 && distance > 1100)
			return true;
		
		return false;
	}
	


}

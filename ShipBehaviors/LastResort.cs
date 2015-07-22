using UnityEngine;
using System.Collections;

public class LastResort : AiBehavior {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	protected override void Update () {
	
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
	
	public override void RequestBehavior(int PlayerID)
	{
		if (PlayerID > -1)
			ship.photonView.RPC("LastResortRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("LastResortRPC", PhotonTargets.Others, null);
	}
	
	public override void DetermineSpeed ()
	{
		
	}	
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		//Check if the behavior of the leader is the same as the subject.
		if (leader.GetBehavior().Equals(subject.GetBehavior()) && leader.ID != subject.ID)
			return true;
			
		AiShip enemyShip = leader.GetTarget().gameObject.GetComponent("AiShip") as AiShip;
		// For Leaders eyes Only
		if (leader.GetMyFleet().threat < (enemyShip.GetMyFleet().threat * 10))
			return true;
			
		return false;
	}

}

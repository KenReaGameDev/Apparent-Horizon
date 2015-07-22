using UnityEngine;
using System.Collections;

public class Protect : AiBehavior {

	public Protect(Ship inShip)
	{
		ship = inShip;
		if(ship.photonView.isMine)
			ship.photonView.RPC("ProtectRPC", PhotonTargets.Others, null);
	}
	
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
	
	public override void DetermineSpeed ()
	{
		
	}
	
	public override void RequestBehavior(int PlayerID)
	{
		if (PlayerID > -1)
			ship.photonView.RPC("ProtectRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("ProtectRPC", PhotonTargets.Others, null);	
	}
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		// Optimize -- Make sure there is a target first.
		if (subject.GetTarget() == null)
			return false;
			
		Attributes subjectStats = subject.getAttributes();
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();	
		AiShip targetShip = subject.GetTarget().gameObject.GetComponent("AiShip") as AiShip;
		string targetFaction = targetShip.getFaction();

			
		// comparison of factions may not work, check later.
		if (health >= 25 &&  subject.getFaction() == targetFaction)
			return true;
		
		return false;
	}

}

using UnityEngine;
using System.Collections;

public class Repair : AiBehavior {
	
	Ship ship;
	bool moving = false;
	float regenStructure;
	float regenShield;
	float regenArmor;
	
	public Repair(Ship inShip)
	{
		ship = inShip;	
		if(ship.photonView.isMine)
			ship.photonView.RPC("RepairRPC", PhotonTargets.Others, null);
	}
	
	protected override void WorkUpdate()
	{
		
	}
	
	protected override void WaitUpdate()
	{
		
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	protected override void Update () {
		Regen();
	}
	
	public override void DetermineTarget()
	{
	
	}
		
	public override void DetermineSpeed ()
	{	
		
	}	
	
	public override void RequestBehavior(int PlayerID)
	{
		if (PlayerID > -1)
			ship.photonView.RPC("RepairRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("RepairRPC", PhotonTargets.Others, null);
	}
	
	void Regen()
	{
		Attributes shipStats = ship.GetAttributes();
		ship.buffs.SetBuff("regen", 3, 999, true);
	}	
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		Attributes subjectStats = subject.getAttributes();
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();	
		
		// If ship is not damaged and has no target, search. (For Fleet leaders and Solo Ships only).
		if (health < 65 &&  subject.Aggressors.Count == 0)
			return true;
		
		return false;
	}

}

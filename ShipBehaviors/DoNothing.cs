using UnityEngine;
using System.Collections;

public class DoNothing : AiBehavior {
	
	Ship ship;
	
	public DoNothing(Ship inShip)
	{
		ship = inShip;
		ship.behaviorName = this.ToString();
	}
	
	// Use this for initialization
	void Start () {
		Debug.Log(ship.name + " has started Do Nothing @ " + Time.timeSinceLevelLoad);
		
	}
	
	// Update is called once per frame
	protected override void Update () {

		UpdateTime();

		if (timeInBehavior > 10)
			SwitchToSearch();
	}

	public override void DetermineTarget()
	{
		// if Target danger is > than self danger, send fleet wide attack order.
		// else fight solo.
	}
	
	public override void RequestBehavior(int PlayerID)
	{
		if (PlayerID > -1)
			ship.photonView.RPC("DoNothingRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("DoNothingRPC", PhotonTargets.Others, null);
		
	}
	
	public override void DetermineSpeed ()
	{
		
	}
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		//Debug.Log(ship.name + " Checking Behavior for DoNothing @ " + Time.timeSinceLevelLoad);
		return false;
	}

	protected override void WorkUpdate()
	{

	}

	protected override void WaitUpdate()
	{

	}

	void SwitchToSearch()
	{
		Switch();

		if (ship.fleetLeader == ship)
		{
			ship.behavior = new Search(ship);
			return;
		}

		if (ship.fleetLeader.behavior.GetType() is DoNothing)
		{
			ship.fleetLeader.SetBehavior(new Search(ship.fleetLeader));
			return;
		}

	}
}

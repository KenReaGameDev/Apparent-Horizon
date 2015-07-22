using UnityEngine;
using System.Collections;

public class TeamFlee : AiBehavior {

	public TeamFlee(Ship inShip)
	{
		ship = inShip;
		if(ship.photonView.isMine)
			ship.photonView.RPC("TeamFleeRPC", PhotonTargets.Others, null);
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
			ship.photonView.RPC("TeamFleeRPC", PhotonPlayer.Find(PlayerID), null);
		else
			ship.photonView.RPC("TeamFleeRPC", PhotonTargets.Others, null);
	}
	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		Attributes subjectStats = subject.getAttributes();
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();	
		float distance = 999999999999;
		
		// Figure out how close we are from the closest Aggressors
		foreach (GameObject go in subject.Aggressors.Keys)
		{			
			float distanceBetween = Vector3.Magnitude(subject.gameObject.transform.position - go.transform.position);
			if (distance > distanceBetween)
				distance = distanceBetween;
		}
		
		// If still too close or too damaged, keep running.
		if (health < 15 && distance < 15000)
			return true;
		
		return false;
	}

}

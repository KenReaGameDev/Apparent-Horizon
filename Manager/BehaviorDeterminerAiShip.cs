using UnityEngine;
using System.Collections;
using System.Threading;

// Leader determines behaviors for entire fleet. 
public class BehaviorDeterminerAiShip {
	
	Ship leader;
	public float behaviorChecks = 15;
	float counterThreat = -999999999;
	int fleetUpdated = 0;
	int iterations = 0;
	System.Random rnd = new System.Random();
	int[] threatArray;
	int[] threatArrayPlayers;
	
	public BehaviorDeterminerAiShip(Ship inShip)
	{
		leader = inShip;
		//Debug.Log("Ship Determiner: " + inShip.name);
	}
	
	// Use this for initialization
	void Start () {
		System.Random rnd = new System.Random();
		behaviorChecks = rnd.Next(0,10);

	}
	
	// Update is called once per frame
	public void Update () {
		////Debug.Log("------------------------------FRAME------------------------------");
		////Debug.Log("Updating Determiner");
		behaviorChecks += Time.deltaTime;
		
		if (leader.behavior is DoNothing)
		{
			DetermineBehavior(leader, leader);
		}
		
		if (behaviorChecks > 13)
			DetermineBehaviors();
	}
	
	void DetermineBehaviors()
	{
		// If time since last frame is lower than 60 FPS threshhold, wait for next frame.
		if (Time.deltaTime > 0.06)
			return;
		// Gets how many ships are in the fleet for Array use
		int fcount = leader.GetMyFleet().fleet.Count - 1;

			
		// If we've updated all the fleet, reset the fleet update counter.
		// Adjust count to relate to non-array amount. 
		if (fleetUpdated >= fcount + 1)
		{
			behaviorChecks = rnd.Next(0, 3);
			fleetUpdated = 0;
			return;
		}

		// if the amount updated is equal to the array adjusted count, do 1. Else do 2 (should be lower than fcount)
		if (fleetUpdated == fcount)
			iterations = 1;
		else
			iterations = 2;
		
		////Debug.Log("Determining Beaviors");
		
		// Don't do every fleet per frame.
//		foreach (Ship ship in leader.GetMyFleet().fleet)
//		{
//			//Thread behaviorThread = new Thread(() => DetermineBehavior(leader, ship));
//			//behaviorThread.Start();
//			DetermineBehavior(leader, ship);
//		}
		
		for(int ndx = fleetUpdated; ndx < fleetUpdated + iterations; ndx++)
		{
			try
			{
				DetermineBehavior(leader, leader.GetMyFleet().fleet[ndx].gameObject.GetComponent<AiShip>());
			}
			catch
			{
				if (!leader)
					return;
				if (!leader.GetMyFleet().fleet[ndx])
					continue;
			}
		}
		
		fleetUpdated += iterations;
	}
	
	public void SetLeader(Ship inShip)
	{
		leader = inShip;
	}
	
	public AiBehavior DetermineLeaderBehavior(AiShip inLeader)
	{
		return null;
	}
	
	public void UpdateThreatArray (Fleet inFleet)
	{
		threatArray = new int[inFleet.fleet.Count];
		for (int ndx = 0; ndx < inFleet.fleet.Count; ++ndx)
		{
			threatArray[ndx] = inFleet.fleet[ndx].GetAttributes().threatLevel;	
		}
		
		if (inFleet.pFleet != null)
		{
			threatArrayPlayers= new int[inFleet.pFleet.Count];
			for (int ndx = 0; ndx < inFleet.pFleet.Count; ++ndx)
			{
				threatArrayPlayers[ndx] = inFleet.pFleet[ndx].GetAttributes().threatLevel;	
			}			
		}
	}
	
	public void DetermineBehavior (Ship leader, Ship subject)
	{
		if (subject.behavior == null)
		{
			//Debug.LogWarning("Behavior for " + subject.name + " is null.");
			subject.SetBehavior(new DoNothing(subject));
		}
		////Debug.Log("Determining Behavior for " + subject.name + " fleet leader: " + subject.isFleetLeader);
		
		// Check if current should stay the same. 
		if (subject.GetBehavior().KeepBehaviorCheck((AiShip)leader, (AiShip)subject))
			return;
		
		////Debug.Log("Passed initial check");
		subject.GetBehavior().CallSwitch();
		//subject.SetBehavior(new DoNothing(subject));
		
		if (leader.GetBehavior() is Search && !subject.isFleetLeader)
		{
			Debug.Log("Search Follow");
			subject.SetBehavior(new SearchFollow(subject));
			Debug.Log("Setting Behavior Name");
			subject.behaviorName = subject.GetBehavior().ToString();
			return;
		}
		

		
		bool isHealthy = false;
		bool hasTarget = false;
		bool canSearch = false;
		bool canMultiTarget = false;
		bool hasAggressors = false;
		
		double health = ((subject.GetAttributes().structureHealth + subject.GetAttributes().armorHealth + subject.GetAttributes().shieldHealth) * 100) / subject.maxHealth;
		if (health > 51)
			isHealthy = true;

		// This is for MultiSoloEngage possiblity. Determine the range later.
		/*
		if (subject.type is Between certain Range)
			canMultiTarget = true;
		*/

		if (subject.GetTarget() != null)
			hasTarget = true;
		
		if (subject.Aggressors.Count > 0)
			hasAggressors = true;
		
		if (subject.isFleetLeader == true && !hasAggressors && isHealthy && !hasTarget)
			canSearch = true;
		
		// Healthy Behaviors	
		if (isHealthy)
		{
			if (leader.GetBehavior() is TeamEngage && leader.target != null)
			{
				subject.SetBehavior(new TeamEngage(subject));
				subject.behaviorName = subject.GetBehavior().ToString();
				return;
			}	
		
			if (leader.targetFleet != null)
				counterThreat = RealThreatDeterminer(subject.GetMyFleet(), leader.targetFleet);
			else
				goto SkipHealthy;
			

			
			// If target is threatening, Fight with team.
			if (hasTarget && counterThreat < 20 && subject.isFleetLeader)
			{
				////Debug.Log("Team Engaging");
				subject.SetBehavior(new TeamEngage(subject));// Team Engage
				subject.behaviorName = subject.GetBehavior().ToString();
				return;
			}	
			
			// Solo Engage
			if (counterThreat >= 20 )
			{
				////Debug.Log("Solo Engaging " + subject.gameObject.name);
				int count = 0;
				int threatLevel = subject.GetAttributes().threatLevel;
				float threatHigh = threatLevel * 2;
				float threatLow = -5;
				
				// Attacks any players in fleet first.
				int pcount = leader.targetFleet.pFleet.Count;
				if (leader.targetFleet.pFleet.Count > 0)
				{					
					subject.SetTargetShip(leader.targetFleet.pFleet[rnd.Next(0, pcount - 1)]);
					subject.SetBehavior(new SoloEngage(subject));
					subject.behaviorName = subject.GetBehavior().ToString();
					return;			
				}
				
				count = 0;
				
				int acount = leader.targetFleet.fleet.Count;
				if (acount > 0)
				{
					subject.SetTargetShip(leader.targetFleet.fleet[rnd.Next(0, acount - 1)]);
					subject.SetBehavior(new SoloEngage(subject));
					subject.behaviorName = subject.GetBehavior().ToString();
					return;
				}

			}
		}
		SkipHealthy:
		
		// Damaged Behaviors
		if (!isHealthy)
		{			

			if (hasAggressors)
			{
				subject.SetBehavior(new SoloFlee(subject));
				subject.behaviorName = subject.GetBehavior().ToString();
				return;			
			}
			
			if (!hasAggressors)
			{
				subject.SetBehavior(new Repair(subject));
				subject.behaviorName = subject.GetBehavior().ToString();
				return;				
			}
		}
		
		// if We have aggressors and we are healthy but havent gotten a target yet, tell the leader to do this.
		if (hasAggressors && isHealthy)
		{
			GameObject newTarget = null;
			
			foreach (GameObject agg in subject.Aggressors.Keys)
			{
				if (agg == null)
					continue;
					
				if (agg.transform != null)
				{
					newTarget = agg;
					break;
				}
			}
			
			if (newTarget != null)
			{
				leader.SetTargetShip(newTarget.GetComponent<Ship>());			
				leader.SetBehavior(new TeamEngage(leader));	
				return;	
			}
		}
		
		// If we've gotten this far and there is nothing to do as the leader, start searching.
		if (canSearch && subject.isFleetLeader)
		{
			////Debug.Log("SEARCH! for " + subject.gameObject.name);
			leader.SetBehavior(new Search(subject));	
			subject.behaviorName = subject.GetBehavior().ToString();
			return;
		}

		// Otherwise just regroup.
		if (subject != leader)
		{
			subject.SetBehavior(new Regroup(subject));
			return;
		}
		
		//Debug.Log("Ship: " + subject.name + "Healthy: " + isHealthy + " / hasAggressors: " + hasAggressors + " / hasTarget: " + hasTarget + " / CanSearch: " + canSearch + " / Is Leader: " + subject.isFleetLeader);
		subject.target = null;
	}
	
	//TODO: Bug! Doesn't take into account the fleet may no longer exist or have any ships.
	//TODO: Make requests for targets limited per second per ship.
	public void RequestTargetChange(Ship subject)
	{
		if (!leader || leader.targetFleet == null || leader.targetFleet.isEmpty)
			return;
		
		int count = 0;
		int threatLevel = subject.GetAttributes().threatLevel;
		float threatHigh = threatLevel * 2;
		float threatLow = -5;
		
		if (leader.targetFleet.pFleet.Count > 0)
		{
			int pcount = leader.targetFleet.pFleet.Count;
			subject.SetTargetShip(leader.targetFleet.pFleet[rnd.Next(0, pcount)]);
			subject.SetBehavior(new SoloEngage(subject));
			subject.behaviorName = subject.GetBehavior().ToString();
			return;	
		}
		
		try
		{
			subject.target = null;

			for (int ndx = 0; ndx < leader.targetFleet.pFleet.Count; ndx++)
			{
				if (Vector3.Distance(subject.transform.position, leader.targetFleet.pFleet[ndx].transform.position) > subject.attributes.range * 3)
					continue;
				else
					subject.SetTargetShip(leader.targetFleet.pFleet[ndx]);
			}	

			if (subject.target != null)
				return;

			for (int ndx = 0; ndx < leader.targetFleet.fleet.Count; ndx++)
			{
				if (Vector3.Distance(subject.transform.position, leader.targetFleet.fleet[ndx].transform.position) > subject.attributes.range * 3)
					continue;
				else
					subject.SetTargetShip(leader.targetFleet.fleet[ndx]);
			}
			if (subject.target != null)
				return;
			
		}
		catch
		{
			if (!subject)
			{
				//Debug.Log("RTC Subject is Dead");
				return;
			}
			
			if (!leader)
			{
				//Debug.Log("RTC Leader is Dead");
				return;
			}
			
			if (leader.targetFleet == null)
			{
				//Debug.Log("RTC TargetFleet is Dead");
				return;				
			}
	
			if (leader.targetFleet.fleet.Count < 1)
			{
				//Debug.Log("RTC TargetFleet Fleet is Empty");
				leader.targetFleet = null;
				return;				
			}
		}

		if (subject.target == null)
		{
			//Debug.Log("No Potential Targets");
		}
	}
	
	public void RequestBehaviorChange(Ship inShip)
	{
		DetermineBehavior(leader, inShip);
	}
	
	

	/// <summary>
	/// Determine what the real threat of the opposing fleet is. The Higher the value the more chance (myfleet) has to win.
	/// > 20 Solo Engage
	/// -20 -> 20 Team Engage
	/// < -20 Last Resort
	/// </summary>
	/// <returns>Determined .</returns>
	/// <param name="myFleet">My fleet.</param>
	/// <param name="enemytFleet">Enemy fleet.</param>
	public static float RealThreatDeterminer(Fleet myFleet, Fleet enemyFleet)
	{		 
		////Debug.Log("Fleet 1 Threat: " + forFleet.threat + " / Fleet 2 Threat: " + againstFleet.threat);
		int relations = FactionTracker.Instance.GetRelationFor(myFleet.faction, enemyFleet.faction);
		// 30 - 50 + 5 = -25
		float realThreat = myFleet.threat - relations + enemyFleet.threat;
		return realThreat;
	}
	

}

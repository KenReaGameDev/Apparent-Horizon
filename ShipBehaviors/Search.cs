using UnityEngine;
//using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Search : AiBehavior {

	Ship ship;
	// Beacon will have ship it belongs to. 
	GameObject beacon;
	BeaconScript beaconScript;
	float deltaTime = 0;
	float distanceTime = 0;
	float distace = 9999;
	
	List<Fleet> potentialFleets = new List<Fleet>();
	bool playerBeacon = false;
	
	public Search(Ship inShip)
	{
		ship = inShip;
		fleetManager = ship.manager;
		beacon = GameObject.Instantiate(Resources.Load("Ai Objects/SearchBeacon")) as GameObject;
		beacon.name = "Beacon_Flee_" + ship.gameObject.name;
		//Debug.Log(beacon.name);
		beaconScript = beacon.GetComponent<BeaconScript>();
		beaconScript.SetOwner(ship);
		RenewBeacon();
		
		ship.SetTargetTransform(beacon.transform);
		
		if(ship.photonView.isMine)
			ship.photonView.RPC("SearchRPC", PhotonTargets.Others, beacon.transform.position);
			
		//Debug.Log("Beacon / Search Initialized");
	}

	public Search(Ship inShip, Vector3 bPos)
	{		
		ship = inShip;
		fleetManager = ship.manager;
		beacon = GameObject.Instantiate(Resources.Load("Ai Objects/SearchBeacon")) as GameObject;
		beacon.name = "Beacon_Flee_" + ship.gameObject.name;
		//Debug.Log(beacon.name);
		//beaconScript = beacon.GetComponent<BeaconScript>();
		//beaconScript.SetOwner(ship);
		//RenewBeacon();
		beacon.transform.position = bPos;
		ship.SetTargetTransform(beacon.transform);
		
	}
	
	// Update is called once per frame
	protected override void Update () {	
		deltaTime += Time.deltaTime;
		distanceTime += Time.deltaTime;
		debugString = "UpdateStart";
		DefaultBehaviorSwitch();
		
	}

	protected override void WorkUpdate()
	{		
	
		if (!playerBeacon && fleetManager.playerShips.Count > 0)
			RenewBeacon();	
			
		//Debug.Log("Work Update Search");
		//FactionTracker.Instance.GetFaction(leader.faction)
		if (ship.GetTarget() == null)
		{	
			//Debug.Log(" No Target ");
			if (!beacon)
			{
				//Debug.Log("No Beacon, Creating");
				CreateBeacon();
			}
			
			ship.SetTargetTransform(beacon.gameObject.transform);
		}	
		else if (beacon != null && ship.GetTarget() == beacon.transform)
		{
			//Debug.Log(" Determining Speed ");
			DetermineSpeed();			
		}
		else if (ship.GetTarget() != null && ship.GetTarget() != beacon.transform)
		{		
			// If it has a target, it shouldn't be searching. It should be fighting.
			
			if (ship.GetTarget().GetComponent<Ship>() != null)
				ship.SetBehavior(new TeamEngage(ship));
				
			return;
		}

		if (distanceTime > 3)
		{
			//Debug.Log("Checking Distance");
			DistanceCheck();
		}
		
		if (ship.photonView.isMine)
			MasterClientUpdate();
			

	}
	
	void MasterClientUpdate()
	{
		//Debug.Log("In Master Update");
		if (beaconScript != null && beaconScript.GetTriggered())
			RenewBeacon();
		
		if (deltaTime > 5)
		{
			DetermineTargetSurvival();
		}
		
		//Debug.Log("Leaving Master Update");
	}

	protected override void WaitUpdate()
	{
		if (ship.GetTarget().GetComponent<Ship>() != null)
			ship.SetBehavior(new TeamEngage(ship));
			
		if ((waitTimer += Time.deltaTime) >= 30)
		{
			if (!beacon)
				CreateBeacon();

			ship.target = beacon.transform;
			bCommand = BehaviorCommand.WORK;
		}
	}
	
	void DistanceCheck()
	{
		if (beacon != null)
			distance = Vector3.Distance(ship.transform.position, beacon.transform.position);
			
		distanceTime = 0;	
	}
	
	// For search, Fly at speed of slowest ship.
	// This algorithm will probably be changed later for more realistic movement.
	public override void DetermineSpeed ()
	{
	
		Vector3 targetDirection = beacon.transform.position - ship.transform.position;
		targetDirection = Vector3.Normalize(targetDirection);
		float angleChange = Vector3.Angle(ship.transform.forward, targetDirection);
		float change = ship.degreesPerSecond / angleChange;
		change *= Time.deltaTime;
		ship.transform.rotation = Quaternion.Slerp( ship.transform.rotation, Quaternion.LookRotation( targetDirection ), change );
		float forceNeeded = 10;
		//Debug.Log("mid Way in Determine Speed");
		if (distance < 3000)
			forceNeeded = 1;
		ship.rigidbody.AddForce(ship.transform.forward * ship.attributes.maxAcceleration * forceNeeded);
		
		float speedCur = Vector3.Magnitude(ship.transform.forward * ship.attributes.maxAcceleration * forceNeeded);
		//Debug.Log(" Determined Speed " + speedCur + " dir " + targetDirection.magnitude + " mAx " + ship.attributes.maxAcceleration + " fN " + forceNeeded);
	}
	
	public override void RequestBehavior(int PlayerID)
	{
		if(PlayerID > -1)
			ship.photonView.RPC("SearchRPC", PhotonPlayer.Find(PlayerID), beacon.transform.position);
		else
			ship.photonView.RPC("SearchRPC", PhotonTargets.Others, beacon.transform.position);
	}
	
	private void RenewBeacon()
	{
		//Debug.Log( " Renewing Beacon ");
		////Debug.Log("BEACON MOVED");
		beacon.transform.position = PositionBeacon();
		ship.photonView.RPC("UpdateBeaconRPC", PhotonTargets.Others, beacon.transform.position);
		beaconScript.triggered = false;
		//Debug.Log(" Renewed Beacon ");
	}	
	
	// Set the beacon to a new position to last know player Ship Coordinate or within 15000 if no player ships available.
	private Vector3 PositionBeacon()
	{
		int pCount = fleetManager.playerShips.Count;
		System.Random rnd = new System.Random();
		Vector3 pos = Vector3.zero;
		Debug.Log("Player count for search: " + pCount);
		if (pCount > 0)
		{
			int pickedship = rnd.Next(0, fleetManager.playerShips.Count);
			
			if (fleetManager.playerShips[pickedship] == null)
				pickedship = 0;
			
			if (fleetManager.playerShips[pickedship] != null)
			{
				pos = fleetManager.playerShips[pickedship].transform.position;
				playerBeacon = true;
			}
			else
			{
				pos = new Vector3(rnd.Next(-15000,15000), rnd.Next(-15000,15000), rnd.Next(-15000,15000));
				pos += ship.transform.position;
				playerBeacon = false;	
			}
		}
		else
		{
			pos = new Vector3(rnd.Next(-15000,15000), rnd.Next(-15000,15000), rnd.Next(-15000,15000));
			pos += ship.transform.position;
			playerBeacon = false;	
		}
		
		beaconScript.ResetCollider();
		return pos;
	}
	
	public override void SyncBeaconPosition(Vector3 inVector)
	{
		beacon.transform.position = inVector;
	}
	
	/// <summary>
	/// Determines the target in Survival Mode
	/// </summary>
	void DetermineTargetSurvival()
	{
		if (fleetManager == null)
		{
			fleetManager =  ship.manager;
		}
		
		int playerShipCount = fleetManager.playerShips.Count;
		
		if (playerShipCount <= 0)
		{
			//Debug.LogWarning("No Playerships Available");
			deltaTime = 0;
			return;
		}
		
		int pickedShip = UnityEngine.Random.Range(0, playerShipCount - 1);
		
		if(!WithinRange(ship.GetAttributes().range * 3, ship.transform.position, fleetManager.playerShips[pickedShip].transform.position))
			return;
		
		if (fleetManager.playerShips[pickedShip] != null)
		{
			ship.SetTargetShip(fleetManager.playerShips[pickedShip]);
			ship.SetBehavior(new TeamEngage(ship));
			// Change Behavior.
		}
		
		Debug.Log("targeting: " + ship.target.name);
		deltaTime = 0;
	}
	
	public override void DetermineTarget()
	{	
		
		debugString = " Determining Target ";
		if (!ship.isFleetLeader || Game.gameState == Game.Gamestate.Loading)
		{
			////Debug.Log("Ship is not fleet leader, returning :: " + ship.name);
			return;
		}
		debugString = " Checked Fleet Leader / Checking fleetManager ";
		////Debug.Log("Is Determining Search Target: " + ship.name);
		
		////Debug.Log("Determining TargeT");
		// The required object has the fleet manager per system.
		if (fleetManager == null)
		{
			fleetManager =  ship.manager;
		}
		debugString = " Got Fleet Manager / Checking Fleet Manager  ";
		
		Fleet fl = null;
		
		//fleetManager.RequestFleetShuffle();
		
		RequestFleetShuffle();
		
		////Debug.Log("CheckingFleets");
		int fleetCount = fleetManager.GetFleetListCount(); 
		int toProcess = fleetCount ;
		ManualResetEvent resetEvent = new ManualResetEvent(false);		
		
		potentialFleets.Clear();
		for (int checkFleets = 0; checkFleets < fleetCount; checkFleets++)
		{
			////Debug.Log("Checking Fleet " + checkFleets + " of " + fleetCount);
			
//			if (potentialFleets.Count > 2)
//			{
//			
//				//Debug.Log("Checked ");
//				resetEvent.Set();
//				resetEvent.WaitOne();
//				break;
//			}
			
			Fleet thisFleet = null;
			
			try
			{
				thisFleet = fleetManager.GetFleetList()[shuffleArray[checkFleets]];		
			}
			catch
			{
				if (Interlocked.Decrement(ref toProcess) == 0) resetEvent.Set();
				////Debug.Log("Fleet " + checkFleets + " Out of Bounds.");
				continue;
			}
			
			if (thisFleet == null || thisFleet.faction == ship.getFaction() || thisFleet.fleetLeader == null)
			{
				if (Interlocked.Decrement(ref toProcess) == 0) resetEvent.Set();
				////Debug.Log("Fleet " + checkFleets + " is null ");
				continue;
			}
				
			Vector3 shipPos = ship.transform.position;
			Vector3 fleetPos = thisFleet.fleetLeader.transform.position;	
			
			ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state){
				CheckFleet(thisFleet, shipPos, ship.GetMyFleet(), fleetPos);
				if (Interlocked.Decrement(ref toProcess) == 0) resetEvent.Set();
			}), null);	
			
			////Debug.Log("Fleet " + checkFleets + " threaded. toProcess: " + toProcess);
		}	
		
		resetEvent.WaitOne();
		
		// Picks a fleet from one of the potential fleets.		
		if (potentialFleets.Count > 0)
		{
			int pickFleet = UnityEngine.Random.Range(0, potentialFleets.Count);
			ship.SetTargetShip(potentialFleets[pickFleet].fleetLeader);
			ship.determiner.UpdateThreatArray(potentialFleets[pickFleet]);
		}		
		
		deltaTime = UnityEngine.Random.Range(-10.0f, 0.0f);	
		debugString = " Finished checking fleets for targets. ";

	}

	void CheckFleet(Fleet inFleet, Vector3 shipPos, Fleet shipFleet, Vector3 flPos)
	{
		// Check to see if ship is within combat range.
		bool withinRange = WithinRange(ship.GetAttributes().range * 3, shipPos, flPos);
		
		if (!withinRange)
			return;
		
		// Get the threat of other ship.
		float threatDet = BehaviorDeterminerAiShip.RealThreatDeterminer(shipFleet, inFleet);

		// If other ship thread is > -20 fight.
		if (threatDet > -20)
		{
			lock (potentialFleets)
				potentialFleets.Add(inFleet);
		}
//		else
//		{
//			//Vector3 range = ship.transform.position - inFleet.fleetLeader.transform.position;
//			////Debug.Log("Within Range? :: " + withinRange + "Range :: " + range.magnitude + " FL POS :: " + fl.fleetLeader.transform.position + " Threat Determined :: " + threatDet);
//		}
	}



	private void CreateBeacon()
	{
		beacon = GameObject.Instantiate(Resources.Load("Ai Objects/SearchBeacon")) as GameObject;
		beaconScript = beacon.GetComponent<BeaconScript>();
		beaconScript.SetOwner(ship);
		RenewBeacon();
	}

	
	public override bool KeepBehaviorCheck(AiShip leader, AiShip subject)
	{
		
		if (ship.Aggressors.Count > 0 )
		{
			//Debug.Log("Search has aggressors :: " + ship.name);
			return false;
		}
		
		if (ship.target != beacon.transform && ship.target != null)
		{
//			if (ship.target != beacon.transform)
//				//Debug.Log("Search has target other than beacon");
//			else
//				//Debug.Log("Search has target is null");
			return false;
		}

		Attributes subjectStats = subject.getAttributes();
		// Get Percent / 100% using cross multiply
		double health = GetCurrentHealthPercentage();
		
		// If ship is not damaged and has no target, search. (For Fleet leaders and Solo Ships only).
		if (health >= 75 &&  subject.GetTarget() == beacon.transform)
			return true;
		
		//Debug.Log("Behavior Not Kept");
		return false;
	}
	
	protected override void Switch()
	{	
		timeInBehavior = 0;
		if (ship.target.gameObject == beacon)
			ship.target = null;
		
		DestroyBeacon();
	}
	
	public void DestroyBeacon()
	{
		GameObject.Destroy(beacon);	
	}

}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;

// 1.5.2014
// TODO: 
// Implement Loading of actual ships into game.
// Maybe load all ships to outside of map -- then clone!
// DESPAWN SHIPS ON CHANGE!
// Generate Fleets based on ships
// Begin AI Behavior algorithms.

// Extended:: Continue to multithread things like Asteroid Generation!
// Fleet Manager hangs out in..
public class FleetManager : MonoBehaviour {	

	public int[] fleetShuffleAry;
	public int[] fleetSeed = new int[10]; 
	public int systemID = 0;
	public string[] factionNames;
	public int factionCount;
	public int startWave = 1;
	public GameObject warpEffect;
	
	public List<Fleet> allFleets = new List<Fleet>();
	// Holds the AiShip for every Ship.
	
	/// <summary>
	/// Holds all A.I. Ships that are non Player
	/// </summary>
	public List<Ship> allShips = new List<Ship>();
	
	/// <summary>
	///  Holds All Player + Player A.I. Ships
	/// </summary>
	public List<Ship> playerShips = new List<Ship>();
	
	public static int aiShipCount = 0;
	// For Preloading all Factions ship specs. 
	static public List<XDocument> allShipSpecs = new List<XDocument>();
	static public string resourcePath;
	
	// Thread Safe Variables.
	private readonly object syncLock = new object();
	private readonly object xmlLock = new object();
	private bool shuffling = false;
	public static int threads;
	public ConcurrentQueue<AiShip> ThreadQueue = new ConcurrentQueue<AiShip>();
	
	int fleetUpdate = 0;
	int fleetShuffle = 0;
	float delaySuffleRequests = 0;
	
	public struct shipInfo {
		public string name;
		public string faction;
		public int index;
		public int photonViewID;
	};
	
	List<shipInfo> shipConstructor = new List<shipInfo>();
	public bool host = false;
	
	public PhotonView photonView;
	
	// List that has the fleets.
	// Fleets them-selves are a list of ships in the fleet.
	// fleet ship 0 == fleet leader. This is determined by strongest ship in fleet.
	// fleet leader cannot change until leader is dead.
	
	// BUG: This is being called too many times.
	public void FleetManager_New (int[] seed, int inID)
	{
		Debug.Log("Fleet Manager has been Created");
		fleetSeed = seed;
		systemID = inID;	
		resourcePath = Application.dataPath;
		allShipSpecs = FactionTracker.Instance.GetAllFactionShipDatas();
		allShips = new List<Ship>();
		allFleets = new List<Fleet>();
		
		Run ();
		// If system is old...
		// Load old system information!
	}
	
	// Called during system creation.
	void Run () {		
	
		foreach (PlayerShip p in GetComponent<Game>().GetPlayerShipList())
		{
			p.currentSystemSeed = systemID;
		}
		
		NextWave(startWave);
		
	}
	
	void Start() {
		allShipSpecs = FactionTracker.Instance.GetAllFactionShipDatas();
		ThreadPool.SetMaxThreads(8, 8);
	}
	
	// Update is called once per frame
	public void Update () {
		
		CheckBadPlayers();
		
		if (Game.gameState != Game.Gamestate.Playing)
			return;
			
		if (fleetUpdate > 30)
		{
			try
			{
				foreach(Fleet fl in allFleets)
					fl.UpdateFleet();
					
				RequestFleetShuffle();
				fleetUpdate = 0;
			}
			catch
			{
				fleetUpdate = 30;	
			}
		}
		
		delaySuffleRequests += Time.deltaTime;
		
		++fleetUpdate;
		++fleetShuffle;
		
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			StartCoroutine(DestroyShipsForWave());
		}
	}
	
	public void CheckBadPlayers()
	{
		playerShips.RemoveAll(item => item == null);
	}
	
	IEnumerator DestroyShipsForWave()
	{
		Debug.LogWarning("Destroying Ships");
		
		int killcount = allShips.Count;
		
		for (int i = 0; i < killcount; i++)
		{
			allShips[i].DestroyThisShip = true;
			yield return new WaitForSeconds(0.25f);
		}
		// Minus 1 for player fleet		
//		int checkcount = allFleets.Count - 1;
//		int newcount = 0;
//		while (newcount < checkcount)
//		{
//			int checkPlayerfleet = 0;
//			
//			while (allFleets[checkPlayerfleet].pFleet.Count > 0)
//				checkPlayerfleet++;
//			
//			if (checkPlayerfleet < allFleets.Count)
//			{
//				int shipC = allFleets[checkPlayerfleet].fleet.Count;
//				
//				for (int ndx = 0; ndx < shipC; ndx++)
//				{
//					allFleets[checkPlayerfleet].fleet[0].DestroyThisShip = true;
//					yield return new WaitForSeconds(0.25f);
//				}
//				
//				
//			}
//			
//			newcount++;
//		}
		
		//yield return new WaitForSeconds(10);
		//StartCoroutine(DestroyShipsForWave());
	}
	
	public int[] ShuffleArray(int[] array)
	{
		System.Random r = new System.Random();
		for (int i = array.Length; i > 0; i--)
		{
			int j = r.Next(i);
			int k = array[j];
			array[j] = array[i - 1];
			array[i - 1]  = k;
		}
		return array;
	}	

	// Safely shuffle array using ints.
	public void RequestFleetShuffle()
	{
		if (delaySuffleRequests < 1)
			return;

		delaySuffleRequests = 0;

		while (shuffling)
		{
			continue;
		}

		shuffling = true;
		if (fleetShuffleAry == null || fleetShuffleAry.Length < allFleets.Count)
		{
			Debug.Log("Resizing fleet shuffle array");
			int[] temp = new int[allFleets.Count];
			for (int ndx = 0; ndx < allFleets.Count; ndx++)
				temp[ndx] = ndx;

			fleetShuffleAry = temp;
		}

		int[] temp2 = fleetShuffleAry;
		temp2 = ShuffleArray(temp2);
		fleetShuffleAry = temp2;
		shuffling = false;
	}
	

	
	public void Shuffle<T>(IList<T> list)  
	{  
	    System.Random rng = new System.Random();  
	    int n = list.Count;  
	    while (n > 1) {  
	        n--;  
	        int k = rng.Next(n + 1);  
	        T value = list[k];  
	        list[k] = list[n];  
	        list[n] = value;  
	    }  
	}
	

	IEnumerator LoadShips()
	{
		shipInfo info;
		int infoCount = shipConstructor.Count;
		int starRadius = (int)GetComponent<SystemInformation>().GetStarRadius();
		for (int ndx = 0; ndx < infoCount; ndx++)
		{
			info = shipConstructor[ndx];
			
			if (info.faction == null || info.name == null || info.index == null)
			{
				Debug.LogWarning("Ship info was null ShipConstructor Count :: " + shipConstructor.Count);
				continue;
			}
			
			Attributes att = Ships.LoadAiShip(info.faction, info.name);
			
			
			float radMin = 5000.0f;
			float radMax = 100000.0f;
			float posx = UnityEngine.Random.Range(radMin, radMax);
			float posy = UnityEngine.Random.Range(radMin, radMax);
			float posz = UnityEngine.Random.Range(radMin, radMax);
			
			Vector3 pos = new Vector3(posx, posy, posz);
			
			if (UnityEngine.Random.value > 0.5f)
				pos.x *= -1;
			if (UnityEngine.Random.value > 0.5f)
				pos.y *= -1;
			if (UnityEngine.Random.value > 0.5f)
				pos.z *= -1;	
				
			Debug.Log("Ships/" + info.faction + "/" + att.name + "/" + att.modelName + "/" + pos.ToString());
			GameObject ship =  PhotonNetwork.Instantiate(("Ships/" + info.faction + "/" + att.name + "/" + att.modelName), pos, Quaternion.identity, 0) as GameObject;
			//ship.transform.position = pos;
			Debug.Log("Adding AI Ship");
			ship.AddComponent<AiShip>();
			ship.GetComponent<AiShip>().CompleteShip(att, allShips.Count, this);
			info.photonViewID = ship.GetComponent<PhotonView>().viewID;
			Debug.Log(ship.GetComponent<PhotonView>().viewID);
//			TrailRenderer tr = ship.AddComponent<TrailRenderer>();
//			tr.time = 5;
			allShips.Add(ship.GetComponent<AiShip>());
			shipConstructor[ndx] = info;
			SetFleet_Ai(ship.GetComponent<AiShip>());
			
			int waits = 5;
			while (waits > 0)
			{
				yield return null;
				waits--;
			}
		}
		
		shipConstructor.Clear();
		RequestFleetShuffle();
		NetworkManager.LoadingIncrement();
	}
	
	/// <summary>
	/// Loads ships for next wave.
	/// </summary>
	/// <returns>The ships wave.</returns>
	IEnumerator LoadShipsWave()
	{
		shipInfo info;
		int infoCount = shipConstructor.Count;
		int starRadius = (int)GetComponent<SystemInformation>().GetStarRadius();
		for (int ndx = 0; ndx < infoCount; ndx++)
		{
			info = shipConstructor[ndx];
			
			if (info.faction == null || info.name == null || info.index == null)
			{
				Debug.LogWarning("Ship info was null ShipConstructor Count :: " + shipConstructor.Count);
				continue;
			}
			
			Attributes att = Ships.LoadAiShip(info.faction, info.name);
			
			
			float radMin = 5000.0f;
			float radMax = 100000.0f;
			float posx = UnityEngine.Random.Range(radMin, radMax);
			float posy = UnityEngine.Random.Range(radMin, radMax);
			float posz = UnityEngine.Random.Range(radMin, radMax);
			
			Vector3 pos = new Vector3(posx, posy, posz);
			
			if (UnityEngine.Random.value > 0.5f)
				pos.x *= -1;
			if (UnityEngine.Random.value > 0.5f)
				pos.y *= -1;
			if (UnityEngine.Random.value > 0.5f)
				pos.z *= -1;	
			
			Debug.Log("Ships/" + info.faction + "/" + att.name + "/" + att.modelName + "/" + pos.ToString());
			GameObject ship =  PhotonNetwork.Instantiate(("Ships/" + info.faction + "/" + att.name + "/" + att.modelName), pos, Quaternion.identity, 0) as GameObject;
			//ship.transform.position = pos;
			Debug.Log("Adding AI Ship");
			ship.AddComponent<AiShip>();
			ship.GetComponent<AiShip>().CompleteShip(att, allShips.Count, this);
			info.photonViewID = ship.GetComponent<PhotonView>().viewID;
			Debug.Log(ship.GetComponent<PhotonView>().viewID);
			//			TrailRenderer tr = ship.AddComponent<TrailRenderer>();
			//			tr.time = 5;
			allShips.Add(ship.GetComponent<AiShip>());
			shipConstructor[ndx] = info;
			SetFleet_Ai(ship.GetComponent<AiShip>());
			
			int waits = 5;
			while (waits > 0)
			{
				yield return null;
				waits--;
			}
			
			if (ndx < infoCount)
				Debug.Log("Next Ship");
			else
				Debug.Log("Ships Loading Finished");
		}
		
		shipConstructor.Clear();
		RequestFleetShuffle();
		yield return StartCoroutine(SyncWaveFleetsRoutine());
		if (NetworkManager.loadingIndex < 4)
			NetworkManager.LoadingIncrement();
	}
	
	/// <summary>
	/// Generates the next wave based on which wave you are on.
	/// </summary>
	/// <param name="wave">Next wave #</param>
	public void NextWave(int wave)
	{
		Game.currentWave = wave;
		Debug.LogWarning("Next Wave Initiating");
		// 5 is Minimum amount of enemies.		
		int shipsNeeded = 5 + Mathf.FloorToInt(1.33f * PhotonNetwork.playerList.Length);
		// WaveLimit is used to increase enemies based on how many waves since last difficulty increase
		float waveLimit = 5 - (wave % 5);		
		
		// Make sure it is an integer. 
		int difficulty = Mathf.FloorToInt(wave / 5);
		
		// Algorithm to determine how many ships.
		shipsNeeded += Mathf.FloorToInt((shipsNeeded * (0.5f * difficulty)) * (0.25f * waveLimit));		
		Debug.Log("Ships needed" +  shipsNeeded);
		// Make sure factions are loaded and ready.
		factionNames = FactionTracker.Instance.GetFactionNames();
		factionCount = FactionTracker.Instance.GetFactionCount();
		
		// Create ships.
		// 20% strongest / 30% Minus 1 Difficulty / 50% Minus 2 Difficulty
//		ThreadShips(Mathf.FloorToInt(shipsNeeded * 0.2f), difficulty);
//		ThreadShips(Mathf.FloorToInt(shipsNeeded * 0.3f), difficulty - 1);
//		ThreadShips(Mathf.FloorToInt(shipsNeeded * 0.5f), difficulty - 2);
		
		StartCoroutine(CoRootShips(Mathf.FloorToInt(shipsNeeded * 0.2f), difficulty, false));
		StartCoroutine(CoRootShips(Mathf.FloorToInt(shipsNeeded * 0.3f), difficulty - 1, false));
		// Most amount of ships goes last.
		StartCoroutine(CoRootShips(Mathf.FloorToInt(shipsNeeded * 0.5f), difficulty - 2, true));	
		//StartCoroutine(LoadShipsWave());
		PayPlayersWaveCompletion(wave);
	}
	
	public void PayPlayersWaveCompletion(int wave)
	{
		foreach(Ship pship in playerShips)
		{
			if (pship.GetComponent<PhotonView>().isMine && pship is PlayerShip)
			{
				pship.AddResoruce(100 + (5 * wave));
				return;
			}
		}
	}
	void ThreadShips(int inAmount, int inDifficulty)
	{
		Debug.Log("Threading Ships");
		if (inDifficulty < 1)
			inDifficulty = 1;
			
		if (inDifficulty > 5)
			inDifficulty = 5;
			
		int toProcess = inAmount;
		ManualResetEvent resetEvent = new ManualResetEvent(false);
		
		for (int createShips = 0; createShips < inAmount; createShips++)
		{
			Debug.LogWarning("Threading Ship " + createShips + " of " + inAmount);
			ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state){
				WaveShipGenerationThread(inDifficulty);
				if (Interlocked.Decrement(ref toProcess) == 0) resetEvent.Set();
			}), null);	
		}	
		Debug.LogWarning("Waiting for Threads");
		resetEvent.WaitOne(10000);
	}
	
	IEnumerator CoRootShips(int inAmount, int inDifficulty, bool NextStep)
	{	
		if (inDifficulty < 1)
			inDifficulty = 1;
		
		if (inDifficulty > 5)
			inDifficulty = 5;
		
		Debug.LogWarning("Ships to Create" + inAmount);
		for (int createShips = 0; createShips < inAmount; createShips++)
		{
			WaveShipGenerationThread(inDifficulty);
			yield return new WaitForSeconds(0.1f);
		}	
		
		if (NextStep)
			StartCoroutine(LoadShipsWave());
		else
		{
			yield return new WaitForSeconds(1);
			SyncWave();
			yield return new WaitForSeconds(1);
			SyncWaveFleetsRoutine();
		}
			
	}
	void CreateShipInfos()
	{
		Debug.Log("###########################ENTERING CREATE SHIPS ###########################");
		// Difficulty is first 4 numbers of seed. 9999 is max.
		int difficulty = 0;		
		for (int i = 0; i < 4; i++)
		{
			difficulty += fleetSeed[i] * System.Convert.ToInt32(System.Math.Pow(10, 4-i-1));
		}
		difficulty = System.Math.Abs(difficulty);	
		
		// [NOTE]
		// Use difficulty to determine type of ships. The higher the difficulty, the larger the amount of ships
		// and higher the chance of them being stronger.
		// Less than 5 ships should be all scouts.
		
		int shipAmount = 105; // difficulty / 100
		int toProcess = shipAmount;
		ManualResetEvent resetEvent = new ManualResetEvent(false);
		Debug.Log("Ship Amount " + shipAmount);
		factionNames = FactionTracker.Instance.GetFactionNames();
		factionCount = FactionTracker.Instance.GetFactionCount();
		
		Debug.Log("Iterating Ships");
		for (int createShips = 0; createShips < shipAmount; createShips++)
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state){
				ShipGenerationThread(difficulty, factionCount);
				if (Interlocked.Decrement(ref toProcess) == 0) resetEvent.Set();
			}), null);	
		}	
		
		// Wait 10 seconds.
		resetEvent.WaitOne(10000);
	}
	
	//BUG: Thread may not be returning to 0. This will cause hang. 
	void ShipGenerationThread(int inDifficulty, int inFactionCount)
	{
		
		Debug.Log("starting thread");
		threads++;
		Debug.Log("New Shipinfo");
		shipInfo tempI = new shipInfo();
		// Determine the Faction
		Debug.Log("FactionDeterminer");
		tempI.faction = ShipFactionDeterminer();
		Debug.Log("ShipDeterminer");
		tempI = ShipShipDeterminer(tempI, inDifficulty);
		// MAKE THREAD SAFE LATER.
		Debug.Log("Adding to constructor");
		shipConstructor.Add(tempI);		
		Debug.Log("Ending thread");
		threads--;
	}
	
	/// <summary>
	/// Creates a ship for the new wave and adds it to the constructor.
	/// </summary>
	/// <param name="inDifficulty">Difficulty.</param>
	void WaveShipGenerationThread(int inDifficulty)
	{		
		threads++;
		shipInfo tempI = new shipInfo();
		// Determine the Faction
		tempI.faction = ShipFactionDeterminer();
		tempI = DetermineShipUsingDifficulty(tempI, inDifficulty);
		shipConstructor.Add(tempI);		
		threads--;
	}
		
	public void RequestFleetForPlayer(PlayerShip ship)
	{
		SetFleet_Player(ship);
	}
	
	void SetFleet_Player(PlayerShip ship)
	{

		Debug.Log("Setting Player Ship");
		//Debug.Log("Fleeting Ship : " + ship.getShipObject().name + " - Faction " + ship.getFaction() + " / Threat " + ship.GetAttributes().threatLevel);
		// Check if the ship is a fleet leader
		// If this ship has no fleet leader then...
		if (ship.getFleetLeader() == null)
		{
			Debug.Log("Leader is null");
			//FactionTracker.Factions tempFaction = new FactionTracker.Factions();
			// If there is such a faction, it returns it based on name.
			//tempFaction = FactionTracker.Instance.GetFaction(ship.getFaction());
			
			// Check for fleets with faction tag being the same.
			foreach (Fleet fl in allFleets)
			{
				Debug.Log("There is a fleet");
				Debug.Log("FLEET FACTION: " + fl.faction + " / SHIP FACTION: " + ship.faction);
				// Adds ship to fleet if all requirements are met.
				if (fl.faction.Equals(ship.getFaction()) && fl.fleet.Count < 5)//tempFaction.maxShipsInFleet)
				{		
					Debug.Log("ADDED TO FLEET");
					ship.setFleetLeader(fl.fleetLeader);
					Debug.Log("Fleet Leader is now " + ship.getFleetLeader());
					if (ship.getFleetLeader() == null)
						Debug.Log("Ship has no fleet leader Bug. Leader should be " + fl.fleetLeader.getShipObject().name);
					
					ship.SetMyFleet(fl);
					fl.pFleet.Add((PlayerShip)ship);
					// TODO: Determine threat elsewhere because foreach doesn't allow modification of variables.
					//fl.threat += ship.getAttributes().threatLevel;
					break;	
				}
			}

			
			if (ship.getFleetLeader() != null)
			{					
				Fleet fl = ship.GetMyFleet();	
//				if (fl == null)
//				{
//					Debug.Log("Error in setting accel code");
//					Debug.Log("No Fleet: " + ship.getShipObject());
//					Debug.Log("However Leader is: " + ship.getFleetLeader());
//				}
				float accel = ship.GetAttributes().maxAcceleration;					
				
				if (fl.minimumSpeed > ship.GetAttributes().maxSpeed)
					fl.minimumSpeed = ship.GetAttributes().maxSpeed;
				
				if (fl.minimumAccel > accel)
					fl.minimumAccel = accel;
				
				return;
			}
			
			Debug.Log("Creating new Fleet");
			// If no fleet matches criteria or no fleets exist, create new fleet.
			Fleet newFleet = new Fleet(this, ship);
			newFleet.fleet = new List<AiShip>();
			newFleet.pFleet = new List<PlayerShip>();
			newFleet.pFleet.Add(ship);
			newFleet.faction = ship.getFaction();
			Debug.Log(ship.name);
			Debug.Log(newFleet.fleetLeader);
			//TODO: Figure out why this isn't calculating / doesn't exist. 
			newFleet.threat += ship.GetAttributes().threatLevel;
			newFleet.minimumSpeed = ship.GetAttributes().maxSpeed;
			newFleet.minimumAccel = ship.GetAttributes().maxAcceleration;
			Debug.Log("Ship Mass: " + ship.GetAttributes().mass + " and MaxAccell: " + ship.GetAttributes().maxAcceleration);
			Debug.Log("New Fleet MACC = " + newFleet.minimumAccel + " and MinSpeed = " + newFleet.minimumSpeed);
			//newFleet.position = ship.transform.position;
			ship.SetIsFleetLeader(true, ship);
			newFleet.position = ship.transform.position;
			ship.setFleetLeader(newFleet.fleetLeader);
			ship.SetMyFleet(newFleet);
			allFleets.Add(newFleet);
			Debug.Log("Reaches End of Loop ITERATION");
		}
		
//		if (ship.GetMyFleet() == null)
//		{
//			Debug.Log("No ship was ever added to fleet");
//			Debug.Log("Faction is: " + ship.getFaction());
//			Debug.Log("Ship is: " + ship.getShipObject());
//			Debug.Log("Fleet Leader Is: " + ship.getFleetLeader());
//		}		
	}
	
	// Set fleet for AI ship.
	void SetFleet_Ai(AiShip ship)
	{
		aiShipCount++;
		// BUG: Error if no ship. Unknown Cause.
		ship = ship.gameObject.GetComponent<AiShip>();
		
		Debug.Log("Fleeting Ship : " + ship.getShipObject().name + " - Faction " + ship.getFaction() + " / Threat " + ship.GetAttributes().threatLevel);
		// Check if the ship is a fleet leader
		// If this ship has no fleet leader then...
		if (ship.getFleetLeader() == null)
		{
			//FactionTracker.Factions tempFaction = new FactionTracker.Factions();
			// If there is such a faction, it returns it based on name.
			//tempFaction = FactionTracker.Instance.GetFaction(ship.getFaction());
			
			// Check for fleets with faction tag being the same.
			int fleet = 0;
			foreach (Fleet fl in allFleets)
			{
				
				Debug.Log("There is a fleet");
				if (fl.faction.Equals(ship.getFaction()) && fl.fleet.Count < 5)//tempFaction.maxShipsInFleet)
				{	
					// Fleet is broken.
					if (fl.fleetLeader == null)
						continue;
						
					Debug.Log("ADDED TO FLEET");
					ship.setFleetLeader(fl.fleetLeader);
					Debug.Log("Fleet Leader is now " + ship.getFleetLeader());
					
					// Fleet is broken.
					if (ship.getFleetLeader() == null && ship.fleetLeader == null)
					{
						Debug.Log("Fleet is Broken");
						ship.setFleetLeader(null);
						break;
					}
					
					ship.SetMyFleet(fl);
					ship.gameObject.name = "Fleet" + fleet + "_" + ship.gameObject.name;
					System.Random rnd = new System.Random();
					Vector3 pos = new Vector3(rnd.Next(-500,500), rnd.Next(-500,500), rnd.Next(-500,500));
					ship.gameObject.transform.position = fl.fleetLeader.gameObject.transform.position + pos;
					fl.fleet.Add(ship);
					Debug.Log("new Fleet succesfully created");
					// TODO: Determine threat elsewhere because foreach doesn't allow modification of variables.
					//fl.threat += ship.getAttributes().threatLevel;
					break;	
				}
				++fleet;
			}
			
			if (ship.getFleetLeader() != null)
			{					
				Fleet fl = ship.GetMyFleet();	
//				if (fl == null)
//				{
//					Debug.Log("Error in setting accel code");
//					Debug.Log("No Fleet: " + ship.getShipObject());
//					Debug.Log("However Leader is: " + ship.getFleetLeader());
//				}
				
				float accel = ship.GetAttributes().maxAcceleration;					
				
				if (fl.minimumSpeed > ship.GetAttributes().maxSpeed)
					fl.minimumSpeed = ship.GetAttributes().maxSpeed;
				
				if (fl.minimumAccel > accel)
					fl.minimumAccel = accel;
				
				fl.threat += ship.GetAttributes().threatLevel;
				return;
			}
			
			Debug.Log("Creating new Fleet");
			// If no fleet matches criteria or no fleets exist, create new fleet.
			Fleet newFleet = new Fleet(this, ship);
			newFleet.fleet = new List<AiShip>();
			newFleet.pFleet = new List<PlayerShip>();
			newFleet.fleet.Add(ship);
			newFleet.faction = ship.getFaction();
			Debug.Log(ship.getShipObject().name);
			newFleet.threat += ship.GetAttributes().threatLevel;
			newFleet.minimumSpeed = ship.GetAttributes().maxSpeed;
			newFleet.minimumAccel = ship.GetAttributes().maxAcceleration;
			//newFleet.position = ship.transform.position;
			ship.SetIsFleetLeader(true, ship);
			//ship.getShipObject().GetComponent<AiShip>().determiner = new BehaviorDeterminerAiShip();
			newFleet.position = ship.getShipObject().transform.position;
			ship.setFleetLeader(newFleet.fleetLeader);
			ship.SetMyFleet(newFleet);
			ship.gameObject.name = "Fleet" + allFleets.Count + "_" + ship.gameObject.name;
			allFleets.Add(newFleet);
			GameObject.Instantiate(warpEffect, ship.transform.position, Quaternion.identity);
			Debug.Log("Reaches End of Loop ITERATION");
		}
		
//		if (ship.GetMyFleet() == null)
//		{
//			Debug.Log("No ship was ever added to fleet");
//			Debug.Log("Faction is: " + ship.getFaction());
//			Debug.Log("Ship is: " + ship.getShipObject());
//			Debug.Log("Fleet Leader Is: " + ship.getFleetLeader());
//		}				
	}
	
	
	[RPC] public void SyncShipRPC(int shipID, string name, string faction, int index, int playerID)
	{
		if (PhotonNetwork.player.ID != playerID)
			return;
		
		Debug.Log("syncing ship " + shipID);
		
		GameObject ship = PhotonView.Find(shipID).gameObject;
		
		Debug.Log(ship.name);
		Attributes att = Ships.LoadAiShip(faction, name);
		Debug.Log(att.rigPointSize.Length);
		ship.AddComponent<AiShip>().enabled = false;
		AiShip aship = ship.GetComponent<AiShip>();
		aship.CompleteShip(att, allShips.Count, this);
		//TrailRenderer tr = ship.AddComponent<TrailRenderer>();
		//tr.time = 5;
		aship.enabled = true;
		allShips.Add(aship);
	}
	

	
//	[RPC] public void SyncPlayerShipRPC(int shipID, string name, string faction, int index, int playerID)
//	{
//		GameObject ship = PhotonView.Find(shipID).gameObject;
//		Attributes att = Ships.LoadAiShip(faction, name);
//		ship.AddComponent<PlayerShip>().enabled = false;
//		ship.GetComponent<PlayerShip>().attributes = att;
//		allShips.Add(ship.GetComponent<PlayerShip>());
//	}
	
	// Called first.
	[RPC] public void SyncNewPlayerShipsRPC(int playerID)
	{
		if (!host)
			return;
		
		Debug.Log("Syncing AI + Player ships for new Player");
		int sendAmount = allShips.Count + playerShips.Count;
		int[] viewIDs = new int[sendAmount];
		string[] factions = new string[sendAmount];
		int[] types = new int[sendAmount];
		string[] mNames = new string[sendAmount];
		bool[] player = new bool[sendAmount];
		
		try
		{
			int ndx = 0;
			
			Debug.Log("Adding player Ships");
			// Bug Happens somewhere in here.
			foreach (Ship pinfo in playerShips)
			{
				Debug.Log("Adding player viewID" + ndx);				
				viewIDs[ndx] = pinfo.photonView.viewID;
				Debug.Log("Adding player faction" + ndx);
				factions[ndx] = pinfo.faction;
				Debug.Log("Adding player type" + ndx);
				types[ndx] = pinfo.ID;
				Debug.Log("Adding player model" + ndx);
				mNames[ndx] = pinfo.attributes.modelName;
				Debug.Log("Adding player is true" + ndx);
				player[ndx] = true;					
				ndx++;
			}
			
			Debug.Log("Adding AI Ships");
			foreach (Ship info in allShips)
			{
				viewIDs[ndx] = info.photonView.viewID;
				factions[ndx] = info.attributes.faction;
				types[ndx] = info.ID;
				mNames[ndx] = info.attributes.modelName;
				player[ndx] = false;					
				ndx++;
			}
		}
		catch
		{
			SyncNewPlayerShipsRPC(playerID);
			return;
		}
		
		Debug.Log("Sending RPC");
		Debug.LogWarning("Sending ship information to other player");
		photonView.RPC("SyncShipsRPC", PhotonTargets.Others, viewIDs, mNames, factions, types, player, playerID);
		
		if (PhotonNetwork.connected)
			Debug.LogWarning("Sent ship information to other player");		
			
		Debug.Log("RPC Sent");
	}
	
	public void SyncWave()
	{
		int sendAmount = allShips.Count;
		int[] viewIDs = new int[sendAmount];
		string[] factions = new string[sendAmount];
		int[] types = new int[sendAmount];
		string[] mNames = new string[sendAmount];
		bool[] player = new bool[sendAmount];
		
		try
		{
			int ndx = 0;

						foreach (Ship info in allShips)
			{
				viewIDs[ndx] = info.photonView.viewID;
				factions[ndx] = info.attributes.faction;
				types[ndx] = info.ID;
				mNames[ndx] = info.attributes.modelName;
				player[ndx] = false;					
				ndx++;
			}
		}
		catch
		{
			SyncWave();
			return;
		}
		
		photonView.RPC("SyncWaveRPC", PhotonTargets.Others, viewIDs, mNames, factions, types, player);
		if (PhotonNetwork.connected)
			Debug.LogWarning("Sent ship information to other player");
	}
	
	[RPC] public void SyncWaveRPC(int[] shipIDs, string[] mNames, string[] factions, int[] indexes, bool[] player)
	{
		StartCoroutine(ShipSyncCoroot(shipIDs, mNames, factions, indexes, player));
	}
	
	
	// Syncs All Ships passed in using Co-Routine. 
	[RPC] public void SyncShipsRPC(int[] shipIDs, string[] mNames, string[] factions, int[] indexes, bool[] player, int playerID)
	{
		if (PhotonNetwork.player.ID != playerID)
			return;
		
		Debug.Log("Accepting Ship Information");
		//PhotonNetwork.isMessageQueueRunning = false;
		StartCoroutine(ShipSyncCoroot(shipIDs, mNames, factions, indexes, player));
	}
	
	/// <summary>
	/// Sets up AI/Player Ships and anything to do with them.
	/// </summary>
	/// <returns>The sync coroot.</returns>
	/// <param name="shipIDs">Ship viewIDs.</param>
	/// <param name="mNames">Model Names (from XML).</param>
	/// <param name="factions">Factions.</param>
	/// <param name="indexes">Indexes.</param>
	/// <param name="player">Player.</param>
	IEnumerator ShipSyncCoroot(int[] shipIDs, string[] mNames, string[] factions, int[] indexes, bool[] player)
	{
		int sentAmount = shipIDs.Length;
		Debug.Log("Starting Ship Information");
		for (int ndx = 0; ndx < sentAmount; ndx++)
		{
			GameObject ship = PhotonView.Find(shipIDs[ndx]).gameObject;
			
			if (ship == null)
				continue;
			
			// A.I. Ship 
			if (!player[ndx])
			{
				Attributes att = Ships.LoadAiShip(factions[ndx], mNames[ndx]);				
				ship.AddComponent<AiShip>().enabled = false;
				AiShip aship = ship.GetComponent<AiShip>();
				aship.CompleteShip(att, indexes[ndx], this);
				aship.faction = factions[ndx];
				
				TrailRenderer tr = ship.AddComponent<TrailRenderer>();
				tr.time = 5;
				allShips.Add(aship);
				aship.enabled = true;
			}
			else
			{ 
				PositionPredict ped = ship.GetComponent<PositionPredict>();
				
				if (ped == null)
				{
					Attributes att = Ships.LoadAiShip(factions[ndx], mNames[ndx]);
					ship.AddComponent<PlayerShip>().enabled = false;
					ship.AddComponent<PositionPredict>();
					ship.GetComponent<PlayerShip>().attributes = att;
					ship.GetComponent<PlayerShip>().faction = "Player";
					TrailRenderer tr = ship.AddComponent<TrailRenderer>();
					tr.time = 5;
				}
			}
			
			yield return null;
		}
		Debug.Log("Finished Ship Information");
		Game.shipSynced = true;
		//PhotonNetwork.isMessageQueueRunning = true;
	}
	
	// Syncs Fleets One By One using Co-Routine
	[RPC] public void SyncNewPlayerFleetsRPC(int PlayerID)
	{
		if (PhotonNetwork.isMasterClient)
			return;
		
		StartCoroutine(SyncFleetsRoutine(PlayerID));
	}
	
	IEnumerator SyncFleetsRoutine(int PlayerID)
	{
		// Foreach fleet, sync entire fleet to new player.	
		foreach (Fleet fl in allFleets)
		{
			int i = 0;
			int[] shipIDArray = new int[fl.fleet.Count];
			int[] playerIDArray = new int[fl.pFleet.Count];
			int leaderID;
			
			leaderID = fl.fleetLeader.gameObject.GetComponent<PhotonView>().viewID;
			
			foreach (AiShip aiship in fl.fleet)
			{
				shipIDArray[i] = aiship.gameObject.GetComponent<PhotonView>().viewID;
				i++;
			}
			
			i = 0;
			
			foreach (PlayerShip pship in fl.pFleet)
			{
				playerIDArray[i] = pship.gameObject.GetComponent<PhotonView>().viewID;
				i++;
			}
			
			Debug.LogWarning("Seninding fleet information to other player");
			photonView.RPC("SyncPlayerFleetsRPC", PhotonTargets.Others, shipIDArray, playerIDArray, leaderID, PlayerID);
			yield return null;
		}
		
		photonView.RPC("FinishSyncFleetRPC", PhotonTargets.Others, PlayerID);
	}
	
	/// <summary>
	/// Syncs All New Wave Fleets + Behaviors
	/// </summary>
	IEnumerator SyncWaveFleetsRoutine()
	{
		// Foreach fleet, sync entire fleet to new player.	
		foreach (Fleet fl in allFleets)
		{
			if (fl.pFleet.Count > 0)
				continue;
				
			int i = 0;
			int[] shipIDArray = new int[fl.fleet.Count];
			int leaderID;
			
			leaderID = fl.fleetLeader.gameObject.GetComponent<PhotonView>().viewID;
			
			foreach (AiShip aiship in fl.fleet)
			{
				shipIDArray[i] = aiship.gameObject.GetComponent<PhotonView>().viewID;
				i++;
			}
			
			
			//Debug.LogWarning("Seninding fleet information to other player");
			photonView.RPC("SyncNewWaveFleetRPC", PhotonTargets.Others, shipIDArray, leaderID);
			yield return null;
		}
		
		// -1 Broadcasts to all players behavior.
		foreach (Fleet fl in allFleets)
		{
			foreach (AiShip ais in fl.fleet)
			{
				ais.behavior.RequestBehavior(-1);
				ais.settingUp = false;
			}
				
			yield return null;
		}
	}
	
	[RPC] public void FinishSyncFleetRPC(int playerID)
	{
		if (PhotonNetwork.player.ID != playerID)
			return;
			
		Game.fleetSynced = true;
		
		foreach (Fleet fl in allFleets)
		{
			foreach (AiShip ais in fl.fleet)
				ais.photonView.RPC("RequestBehaviorRPC", PhotonTargets.MasterClient, PhotonNetwork.player.ID);
		}
	}
	
	/// <summary>
	/// Syncs A Wave Fleet with provided information.
	/// </summary>
	/// <param name="shipIDs">Ship IDs.</param>
	/// <param name="leaderID">Leader ID.</param>
	[RPC] public void SyncNewWaveFleetRPC(int[] shipIDs, int leaderID)
	{
		
		Debug.Log("Fleeting :: " + leaderID);	
		Fleet newFleet = new Fleet(this, PhotonView.Find(leaderID).gameObject.GetComponent<Ship>());
		newFleet.faction = newFleet.fleetLeader.getFaction();
		newFleet.fleetLeader.SetIsFleetLeader(true, newFleet.fleetLeader);
		
		int finalIndex = shipIDs.Length;
		
		newFleet.fleet = new List<AiShip>();
		newFleet.pFleet = new List<PlayerShip>();
		
		for (int i = 0; i < finalIndex; i++)
		{
			Debug.Log("Fleeting :: " + shipIDs[i]);
			newFleet.fleet.Add(PhotonView.Find(shipIDs[i]).gameObject.GetComponent<AiShip>());
			newFleet.threat += newFleet.fleet[i].GetAttributes().threatLevel;
			newFleet.fleet[i].SetMyFleet(newFleet);
			newFleet.fleet[i].gameObject.name = "Fleet" + allFleets.Count + "_" + newFleet.fleet[i].gameObject.name;
			
		}
		
		allFleets.Add(newFleet);
	}
	
	[RPC] public void SyncPlayerFleetsRPC(int[] shipIDs, int[] playerIDs, int leaderID, int playerID)
	{
		if (PhotonNetwork.player.ID != playerID)
			return;
			
		Debug.Log("Fleeting :: " + leaderID);	
		Fleet newFleet = new Fleet(this, PhotonView.Find(leaderID).gameObject.GetComponent<Ship>());
		newFleet.faction = newFleet.fleetLeader.getFaction();
		newFleet.fleetLeader.SetIsFleetLeader(true, newFleet.fleetLeader);
		
		int finalIndex = shipIDs.Length;
		
		newFleet.fleet = new List<AiShip>();
		newFleet.pFleet = new List<PlayerShip>();
		
		for (int i = 0; i < finalIndex; i++)
		{
			Debug.Log("Fleeting :: " + shipIDs[i]);
			newFleet.fleet.Add(PhotonView.Find(shipIDs[i]).gameObject.GetComponent<AiShip>());
			newFleet.threat += newFleet.fleet[i].GetAttributes().threatLevel;
			newFleet.fleet[i].SetMyFleet(newFleet);
			newFleet.fleet[i].gameObject.name = "Fleet" + allFleets.Count + "_" + newFleet.fleet[i].gameObject.name;
		
		}
		
		finalIndex = playerIDs.Length;

		for (int i = 0; i < finalIndex; i++)
		{
			newFleet.pFleet.Add(PhotonView.Find(playerIDs[i]).gameObject.GetComponent<PlayerShip>());
			// TODO: Sync Player Ship Monobehaviour because it doesn't have attributes. Or something(?)
			newFleet.threat += 5;//newFleet.pFleet[i].GetAttributes().threatLevel;
			newFleet.pFleet[i].SetMyFleet(newFleet);
		}
		
		allFleets.Add(newFleet);
	}
	
	public void NewShip(Ship ship)
	{
		allShips.Add(ship);

		if (ship is AiShip)
			SetFleet_Ai((AiShip)ship);
		else
			SetFleet_Player((PlayerShip)ship);
	}

	// TODO: Fix fleets. 
	// Determine fleets once per system creation.
	void CreateFleets_Init() {
		
		Debug.Log("Creating Fleets of Ships  ASHIPS COUNT "  + allShips.Count );
		
		foreach (Ship ship in allShips)
		{

			if (ship is AiShip)
				SetFleet_Ai((AiShip)ship);
			else
				SetFleet_Player((PlayerShip)ship);			
		}
		
	}
	
	public void UpdateFleet_LeaderLoss(AiShip ship)
	{
			
	}
	
	public void UpdateFleet_ShipLoss(AiShip ship)
	{
		Fleet fl = ship.GetMyFleet();
		float accel = ship.GetAttributes().mass * ship.GetAttributes().maxAcceleration;
		
	}
	// Loads old ships and fleets based on xml or other loading file structure.
	void PopulateSystem_Load () {
		// Use this if it's an old system, loaded from file. 
	}
	
	// During game.savesystem, game will call this to save fleets in system.
	void SaveSystem () {
		
	}
	
	// Unload system and delete before creating new as to clean up garbage. 
	void UnloadSystem () {
		
	}
	
	// Determine Ship based on Parabolic Probabilities
	shipInfo ShipShipDeterminer (shipInfo inInfo, int inDifficulty) {
		
		int factionIndex = FactionTracker.Instance.GetFactionIndex(inInfo.faction);	
		
		// Locked Variables
		int shipCount = 0;
		string shipname = "";
		
		// Determine how many ships faction has.
		lock(xmlLock)
		{
			shipCount = allShipSpecs[factionIndex].Descendants("Attributes").Count();
		}
		
		double[] probabilities = CreateProbabilitiesShipType(shipCount, inDifficulty, true);
		
		// Use PickFaction to pick the Ship (Code Reuse)
		int shipIndex = PickShip(probabilities);
		Debug.Log("SHIP INDEX: " + shipIndex);
	
		// Get ship from XDocument via ship index.
		lock (xmlLock)
		{
		
//			var nodes =	from typeElement in allShipSpecs[factionIndex].Descendants("Attributes")
//					where (int)typeElement.Attribute("threatLevel") == inDifficulty;
					
			var node = allShipSpecs[factionIndex].Descendants("Attributes").Skip(shipIndex).Take(1);
			foreach (var nodes in node)
			{
				shipname = (string)nodes.Element("modelName");	
			}	
		}	
		
		inInfo.name = shipname;
		inInfo.index =factionIndex;
		
		return inInfo;
		//Debug.Log("Ship Picked: " + shipname + " Faction Picked: " + inShip.getFaction());
		// Load attributes for said ship then return ship.
		// Minus 1 due to player not being faction for AiShips.
		
		
		//inShip.setAttributes(Ships.LoadAiShip(inShip.getFaction(),shipname, factionIndex));
		//inShip.currentSystemSeed = systemID;
		//return inShip;		
	}
	
	/// <summary>
	/// Picks a ship that has threat level that == difficulty.
	/// </summary>
	/// <returns>The ship using difficulty.</returns>
	/// <param name="inInfo">Pass in Ship Info Variable.</param>
	/// <param name="inDifficulty">Difficulty to Use.</param>
	shipInfo DetermineShipUsingDifficulty (shipInfo inInfo, int inDifficulty) {
		
		int factionIndex = FactionTracker.Instance.GetFactionIndex(inInfo.faction);	
		
		List<Attributes> ships = Ships.shipDictionary[inInfo.faction].factionShipList
			.Where(x=>x.threatLevel == inDifficulty).ToList();
		
		//Debug.LogWarning("Faction - " + inInfo.faction + " Difficulty - " + inDifficulty + " List Count - " + ships.Count);
		ThreadSafeRandom rnd = new ThreadSafeRandom();
		
		Attributes pickedShip  = ships[0];
		
		if (ships.Count > 1)
			pickedShip = ships[rnd.NextRange(0, ships.Count - 1)];
			
		inInfo.name = pickedShip.modelName;
		inInfo.index = factionIndex;
		
		return inInfo;
	
	}
	
	// Determines Majority.
	// Picks faction for passed in ship.
	// http://stackoverflow.com/questions/9330394/how-to-pick-an-item-by-its-probability
	String ShipFactionDeterminer () {
			
		string factionName = null;
		double probability = 0;
		double majorityProbability = 0;
		double[] probabilities = new double[factionCount];
		
		// Determines Majority.
		// Picks faction for passed in ship.
		// TODO: Use XML file to not use switch, just use enumerated from XML.
		switch (this.fleetSeed[4])
		{
			// Mixed no Pirates
			case 0:
				Debug.Log("No Pirates");	
				probability = 100 / (factionCount - 1);						
				probabilities = CreateProbabilities(probabilities, probability, 1);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Mixed with Pirates
			case 1:
				Debug.Log("Mix with Pirates");
				probability = 100 / factionCount;
				probabilities = CreateProbabilities(probabilities, probability, probability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Majority Pirates
			case 2:
				Debug.Log("Majority Pirates");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				probabilities = CreateProbabilities("Pirates", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Majority Rainbow
			case 3:
				Debug.Log("Majority Rainbow");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				probabilities = CreateProbabilities("Rainbow Surfers", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Majority Sidrat
			case 4:
				Debug.Log("Majority Sidrat");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				probabilities = CreateProbabilities("Sidrat", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Majority Goons
			case 5:
				Debug.Log("Majority Goons");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				probabilities = CreateProbabilities("The Goons", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Majority Strashok
			case 6:
				Debug.Log("Majority Strashok");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				probabilities = CreateProbabilities("Soyuz Strashok", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Majority Semper
			case 7:
				Debug.Log("Majority Semper");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				probabilities = CreateProbabilities("Semper Solus", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Random Minority Faction
			case 8:
				Debug.Log("Minority Faction");
				probability = 100 / (factionCount * 2);
				majorityProbability = probability * (factionCount + 1);
				// TODO: Select a random Minority to be the Majority. Or Create One.
				probabilities = CreateProbabilities("Pirates", probabilities, probability, majorityProbability);
				factionName = factionNames[PickFaction(probabilities)];
				
			break;
			
			// Empty
			case 9:
				probability = 0;
			break;
		}
		
		//Debug.Log("INSHIP FACTION: " + inShip.getFaction());
		return factionName;
	}
		
	// Create probabilities for MAJORITY systems
	double[] CreateProbabilities (string inMajority, double[] probabilities, double probability, double majorProbability) 
	{
		for (int index = 0; index < factionCount; ++index)
		{
			if (string.Equals(inMajority, factionNames[index], System.StringComparison.OrdinalIgnoreCase))
				probabilities[index] = majorProbability;
			else
				probabilities[index] = probability;
		}		
		return probabilities;
	}
	
	// Create probabilities for MIXED systems
	double[] CreateProbabilities (double[] probabilities, double probability, double majorProbability) 
	{	
		for (int index = 0; index < factionCount; ++index)
		{
			probabilities[index] = probability;
		}		
		return probabilities;
	}
	
	// No Pirates
	double[] CreateProbabilities (double[] probabilities, double probability, bool pirates) 
	{
		Debug.Log("No Pirates!");
		for (int index = 0; index < factionCount; ++index)
		{
			if (string.Equals((string)"pirates", factionNames[index], System.StringComparison.OrdinalIgnoreCase))
				probabilities[index] = 0;
			else
				probabilities[index] = probability;
		}		
		return probabilities;
	}
	
	#region Probabilities
	// For Picking Ship.
	// Parabolic Probability Generation Algorithm
	double[] CreateProbabilitiesShipType (int inShipAmount, int inDiffuclty, bool Parabolic) 
	{
		// Gets the ship with the highest probability
		double[] probabilities;
		probabilities = new double[inShipAmount];
		
		Debug.Log("inShipAmount: " + inShipAmount + " - inDifficulty: " + inDiffuclty);		
		double vertexD = (inShipAmount * inDiffuclty / 9999);
		
		Debug.Log("Vertex Double: " + vertexD);
		int vertex = (int)System.Math.Round(vertexD, System.MidpointRounding.AwayFromZero);
		Debug.Log("VERTEX: " + vertex);
		Debug.Log("Vertex: " + vertex);
		probabilities[vertex] = 50;
		
		// Regular Cases
		if (vertex > 0 && vertex < inShipAmount - 1)
		{
			probabilities = LeftSideParabola(probabilities, vertex, inShipAmount);
			probabilities = RightSideParabola(probabilities, vertex, inShipAmount);
		}		
		
		// Special Cases
		// Nothing on Left
		if (vertex == 0) 
		{
			Debug.Log("SPECIAL CASE VERTEX IS 0");
			probabilities = RightSideSpecialCase(probabilities, inShipAmount);
		}
		// Nothing on Right
		if (vertex == inShipAmount)
		{
			Debug.Log("SPECIAL CASE VERTEX IS SHIP AMOUNT");
			probabilities = LeftSideSpecialCase(probabilities, inShipAmount);
		}
		
		for (int i = 0; i < probabilities.Count(); ++i)
		{
			Debug.Log("Prob " + i + ":" + probabilities[i]);
		}
		return probabilities;
	}
	
	double[] LeftSideParabola(double[] probabilities, int vertex, int inShipAmount)
	{
		Debug.Log("LEFT SIDE Regular Case");
		int difference = 1;
		if (vertex > 1)
			difference = vertex;
		int high = vertex - 1;
		int low = 0;
		int midpoint = -1;
		int calculations = 0;
		double baseProbability = 25.0 / (double)difference;
		
		if (difference == 1)
		{
			probabilities[0] = 25;	
			return probabilities;	
		}
		
		if (difference % 2 > 0)
		{			
			midpoint = (int)System.Math.Round((double)(difference/2), System.MidpointRounding.AwayFromZero);
			Debug.Log("Midpoint: " + midpoint);
			probabilities[midpoint - 1] = baseProbability;
		}		
		
		// Should round down. If not force round down.
		calculations = difference / 2;
		
		for (int ndx = 2; ndx < calculations + 2; ++ndx)
		{
			double probabilityMod = (baseProbability / ndx) * 2.0;
			probabilities[high] = baseProbability + probabilityMod;
			probabilities[low] = baseProbability - probabilityMod;
			high -= 1;
			low += 1;
		}
		
		return probabilities;
	}
	
	double[] RightSideParabola(double[] probabilities, int vertex, int inShipAmount)
	{
		Debug.Log("Right SIDE Regular Case");
		// Right Side of Algorithm
		int difference = 1;
		if (vertex < inShipAmount - 2)
			difference = inShipAmount - vertex + 1;
		
		// Vertex is 0
		int high = vertex + 1;
		int low = inShipAmount - 1;
		int midpoint = -1;
		int calculations = 0;
		double baseProbability = 25.0 / (double)difference;
		
		// Odd numbers have a mid number
		// Handle the possibility of only 1 item on the side.
		if (difference == 1)
		{
			probabilities[inShipAmount - 1] = 25;
			return probabilities;
		}
		
		if (difference % 2 > 0)
		{
			midpoint = (int)System.Math.Round((double)(difference/2), System.MidpointRounding.AwayFromZero);
			probabilities[midpoint - 1] = baseProbability;
		}
		
		// Figure out how many times we are going to calculate		
		calculations = difference / 2;
		
		// Using ndx as my probability modifier, end condition has to be increased based upon this.
		for (int ndx = 2; ndx < calculations + 2; ++ndx)
		{
			double probabilityMod = (baseProbability / ndx) * 2.0;
			probabilities[high] = baseProbability + probabilityMod;
			probabilities[low] = baseProbability - probabilityMod;
			high += 1;
			low -= 1;
		}	
		return probabilities;
	}
	
	// Vertex is 0
	double[] RightSideSpecialCase(double[] probabilities, int inShipAmount)
	{		
		int vertex = 0;
		int difference = inShipAmount - 1;
		Debug.Log("inShipAmount Special Case: " + inShipAmount);
		double baseProbability = 100.0 / (double)difference;
		Debug.Log("Base Probability: " + baseProbability); 
		int high = 0;
		// TODO: Check to make sure this is proper.
		int low = inShipAmount - 1;
		int midpoint = -1;
		int calculations = 0;
		
		// Figure out how many times we are going to calculate		
		calculations = difference / 2;
	
		// Using ndx as my probability modifier, end condition has to be increased based upon this.
		for (int ndx = 2; ndx < calculations + 2; ++ndx)
		{
			double probabilityMod = (baseProbability / ndx) * 2.0;
			probabilities[high] = baseProbability + probabilityMod;
			probabilities[low] = baseProbability - probabilityMod;
			high += 1;
			low -= 1;
		}
		
		// Set middile probability if there is an odd number of ships.
		if (difference % 2 == 0)			
		{			
			midpoint = (int)System.Math.Round((double)(difference/2), System.MidpointRounding.AwayFromZero);
			double middleProbability = (probabilities[midpoint+1] + probabilities[midpoint-1]) / 2; 
			probabilities[midpoint] = middleProbability;
		}
		
		// Normalizing to 100.
		int count = probabilities.Count();
		double normal = 0.0f;
		
		// Get total amount that needs to be normalized to 100;
		for (int nrml = 0; nrml < probabilities.Count(); nrml++)
		{
			normal += probabilities[nrml];
		}	
		
		// Cross mulityple to get normal.
		normal = (probabilities[0] * 100) / normal;
		normal = normal / probabilities[0]; 
		
		// Finally normalize all the probabilities.
		for (int nrml2 = 0; nrml2 < probabilities.Count(); nrml2++)
		{
			probabilities[nrml2] *= normal;
		}	
		
		return probabilities;		
	}
	
	// Vertex is Max
	double[] LeftSideSpecialCase(double[] probabilities, int inShipAmount)
	{
		int vertex = inShipAmount - 1;
		int difference = inShipAmount - 1 ;
		double baseProbability = 100.0 / (double)difference;
		int high = vertex;
		// TODO: Check to make sure this is proper.
		int low = 0;
		int midpoint = -1;
		int calculations = 0;

		
		// Figure out how many times we are going to calculate
		calculations = difference / 2;
		// Using ndx as my probability modifier, end condition has to be increased based upon this.
		for (int ndx = 2; ndx < calculations + 2; ++ndx)
		{
			double probabilityMod = (baseProbability / ndx) * 2.0;
			probabilities[high] = baseProbability + probabilityMod;
			probabilities[low] = baseProbability - probabilityMod;
			high -= 1;
			low += 1;
		}

				// Set middile probability if there is an odd number of ships.
		if (difference % 2 == 0)			
		{			
			midpoint = (int)System.Math.Round((double)(difference/2), System.MidpointRounding.AwayFromZero);
			double middleProbability = (probabilities[midpoint+1] + probabilities[midpoint-1]) / 2; 
			probabilities[midpoint] = middleProbability;
		}
		
		// Normalizing to 100.
		int count = probabilities.Count();
		double normal = 0.0f;
		
		// Get total amount that needs to be normalized to 100;
		for (int nrml = 0; nrml < probabilities.Count(); nrml++)
		{
			normal += probabilities[nrml];
		}	
		
		// Cross mulityple to get normal.
		normal = (probabilities[vertex] * 100) / normal;
		normal = normal / probabilities[vertex];  
		
		// Finally normalize all the probabilities.
		for (int nrml2 = 0; nrml2 < probabilities.Count(); nrml2++)
		{
			probabilities[nrml2] *= normal;
		}	
			
		return probabilities;
	}
	// Pick a faction based on probabilities. 
	int PickFaction (double[] probabilities) 
	{
		int count = probabilities.Count();
		double pick = (double)SystemSeed.RandomSeed_Custom(count,99);
		double sum = 0;
		int i=1;
        while(sum < pick) 
		{
        	sum = sum + probabilities[i];
			++i;
			if (i > count - 1)
				break;
        }
		Debug.Log("Int OUT: " + (i-1));
		return (i-1);
	}
	
	int PickShip(double[] probabilities)
	{
		int count = probabilities.Count();
		string bigstring = "";

		//DEBUG
		for (int r = 0; r < count; ++r)
		{
			bigstring += "Prob" + r + ": " + probabilities[r];
		}
		
		Debug.Log(bigstring);
		
		double pick = (double)SystemSeed.RandomSeed_Custom(count,99);
		double sum = 0;
		int i=0;
		Debug.Log ("PICK: " + pick);
        while(sum < pick) 
		{
        	sum = sum + probabilities[i];
			++i;
			if (i > count - 1)
				break;
        }
		Debug.Log("Ship Index Picked: " + (i-1));
		return (i-1);	
	}
	#endregion
	
	public List<Fleet> GetFleetList ()
	{
		return allFleets;
	}
	
	public int GetFleetListCount()
	{
		if (allFleets == null)
			return 0;
		
		return allFleets.Count;
	}


}

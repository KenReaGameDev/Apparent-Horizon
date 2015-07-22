using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

public class AiShip : Ship {	
	
	// Use this for initialization
	void Start () {		
		shipObject = this.transform.gameObject;
		gameObject.tag = "Ship";
		
		if (photonView == null)
			photonView = GetComponent<PhotonView>();

//		photonView.onSerializeTransformOption = OnSerializeTransform.OnlyPosition;
//		photonView.onSerializeRigidBodyOption = OnSerializeRigidBody.OnlyVelocity;
//		photonView.synchronization = ViewSynchronization.ReliableDeltaCompressed;
		
	}
	
	// Update is called once per frame
	public override void Update () {

		if (attributes == null)
		{
			//Debug.LogWarning("Ship has no attributes!");
			return;
		}
		
		if (DestroyThisShip)
			DestroyShip();
			
		MasterClientUpdates();
		ImportantUpdates();

		if (buffs.disable.time <= 0)	
		{
			BehaviorUpdates();
			Repairs();	
		}
			
//		if (recentCommands.Count > 15)
//			recentCommands.RemoveAt(0);
			
	}

	void ElectronicUpdates()
	{
		CheckCollidersDelegate();
	}
	
	void MasterClientUpdates()
	{
		if (attributes.structureHealth <= 0)
			DestroyShip();
	}
	
	void ImportantUpdates()
	{	
		if (blocked)
			PathingCheck();
				
		CalculateHealth();
		
		buffs.Update();
		
		AggressorChecks();
	}

	void BehaviorUpdates()
	{
		
		if (isFleetLeader && determiner == null && photonView.isMine)
		{
			Debug.Log("DetShip");
			determiner = new BehaviorDeterminerAiShip(this);
			myFleet.ChangeLeader(this);
		}

		if (behavior == null)
			behavior = new DoNothing(this);		

		if (isFleetLeader && photonView.isMine)
			determiner.Update();

		behavior.CallUpdate();
	}
	
	
	
	void SaveShip () {
		// Save all information about ship.
	}
	
	public string getFaction()
	{
		return faction;	
	}
	
	public Attributes getAttributes()
	{
		return attributes;	
	}
	
	public Attributes GetAttributes()
	{
		return attributes;	
	}
	
	public void setAttributes(Attributes inAttributes)
	{
		attributes = inAttributes;
	}
	
	public void SetAttributes(Attributes inAttributes)
	{
		attributes = inAttributes;
	}	
	
	public void setFaction(string inFaction)
	{
		faction = inFaction;	
	}
	
	public Ship getFleetLeader()
	{
		return fleetLeader;	
	}
	
	public void setFleetLeader(AiShip inShip)
	{
		fleetLeader = inShip;
	}
	
	public void setAttributesInternal(string inShip)
	{
		this.attributes = Ships.LoadAiShip(inShip);
		//setShipModelLoadInternal();
		//setShipModelLoad(this);
	}
	
	public GameObject getShipModel()
	{
		return this.shipObject;	
	}
	
	public void setShipModelLoadInternal()
	{
		shipObject = GameObject.Instantiate(Resources.Load("Ships/" + faction + "/" + attributes.name + "/" + attributes.modelName)) as GameObject;	
		shipObject.name = attributes.name;
		shipObject.AddComponent("AiShip");
		AiShip temp = shipObject.GetComponent(typeof(AiShip)) as AiShip;
		temp = this;	
	}

	public void CompleteShip(Attributes inAtt, int inID, FleetManager mng)
	{
		Debug.Log(gameObject.name);
		if (photonView == null)
			photonView = this.gameObject.GetComponent<PhotonView>();
		
		manager = mng;
		attributes = inAtt;
		this.ID = inID;
		this.gameObject.name = "Ship_" + attributes.name + ID;
		this.tag = "Ship";
		this.shipObject = this.gameObject;
		if (this.gameObject.GetComponent<Rigidbody>() == null)
			this.gameObject.AddComponent<Rigidbody>();

		type = (ShipClass)Enum.Parse(typeof(ShipClass), attributes.type.ToUpper());
		//modelName = shipModel.name;
		faction = attributes.faction;
		FactionTracker.Instance.GetFaction(faction).ShipCreated();
		attributes.structureHealth = 100;

		this.gameObject.transform.localScale = attributes.initialScale;
		//transform.localScale = new Vector3(50,50,50);
		rigidbody.mass = attributes.mass;
		rigidbody.angularDrag = 1;
		rigidbody.drag = 0.2f;
		rigidbody.useGravity = false;
		behavior = new DoNothing(this);
		gameObject.tag = "AiShip";
		
		PopulateRigPoints();
		//PopulateCameraPoints();
		
		if (photonView.isMine)
			CreateTurretsRandom();
		else
			photonView.RPC("RequestTurretSyncRPC", PhotonTargets.MasterClient, PhotonNetwork.player.ID);
			
		CalculateAttributes();
		
		buffs = new Buff(this);		
	}
	
	
	void CreateTurretsRandom()
	{
		Debug.Log("Creating Turrets for " + this.gameObject.name);

		// Make sure there are rig points on this ship from XML.
		if (attributes.rigPointType == null)
		{
			Debug.LogError("Points are Null");
			return;
		}
		if ( attributes.rigPointType.Length < 1)
		{
			Debug.LogError("Points are Empty");
			return;
		}



		// Make sure Rig Points and Rig Sizes array have proper amount of rigs.
		List<string> properAmount = new List<string>();
		for (int ndx = 0; ndx < attributes.rigPointType.Length; ++ndx)
		{
			if (!String.IsNullOrEmpty(attributes.rigPointType[ndx]))
				properAmount.Add(attributes.rigPointType[ndx]);
			else
				break;			   
		}

		attributes.rigPointType = properAmount.ToArray();
		properAmount.Clear();

		for (int ndx = 0; ndx < attributes.rigPointSize.Length; ++ndx)
		{
			if (!String.IsNullOrEmpty(attributes.rigPointSize[ndx]))
				properAmount.Add(attributes.rigPointSize[ndx]);
			else
				break;			   
		}
		attributes.rigPointSize = properAmount.ToArray();

		int rigPoints = attributes.rigPointType.Length;
		//Debug.Log("Start Position: " + transform.position);
		//PopulateRigPoints();

		if (rigPoints != RigPointsCount)
		{
			// Also add InGame to InXML amounts for debugging for modding.
			Debug.LogError(this.name + " Ship rig points != XML Rig points for ship. Please make sure you have the correct amount of rig point types in your XML file for this ship. Ship will stop being rigged for weapons now. " + rigPoints + " / " + RigPointsCount);
			return;
		}

		//Debug.Log("Starting Loop for Random turrets");
		int rpcount = 0;
		
		CryptoRandom rng = new CryptoRandom();
		
		foreach (Transform rp in RigPoints)
		{
			if (rpcount > 3)
				break;
				
			// Holds all the possible types for this rig point
			string possibleTypes = attributes.rigPointType[rpcount];
			List<string> typeArray = new List<string>(possibleTypes.Split('|'));
			int typesPossible = typeArray.Count;
//			
//			string debugadd = "";
//			
//			for (int ndx = 0; ndx <= typesPossible; ndx++)
//			{
//				debugadd += typeArray[ndx];
//			}
//			
//			// Possible Tpes
//			Debug.LogWarning(debugadd);
//			debugadd = "";
			
			// Holds all the possible Sizes for this rigpoint.
			string possibleSizes = attributes.rigPointSize[rpcount];
			string[] sizeArray = possibleSizes.Split('|');
			int sizesPossible = sizeArray.Length;
//
//			// PossibleSizes
//			for (int ndx = 0; ndx <= sizesPossible; ndx++)
//			{
//				debugadd += sizeArray[ndx];
//			}
//			Debug.LogWarning(debugadd);
			
			//Debug.LogWarning(sizesPossible.ToString() + " " + typesPossible.ToString());
			
			bool selectingWeapon = true;
			string weaponSelection = "";
			WeaponAttributes weaponSelectionAttributes = null;
			
			Weapon.WeaponSize sze = Weapon.WeaponSize.NONE;
			Weapon.WeaponType tpe = Weapon.WeaponType.NONE;
			//Debug.LogWarning(typesPossible);

			int loopCount = 0;
			while (selectingWeapon)
			{
				// Select a random weapon size / type from possibilities.
				rng = new CryptoRandom();
				int	pickSize = rng.Next(sizesPossible);
								
				rng = new CryptoRandom();				
				int pickType = 	pickType = rng.Next(typesPossible);
				
				//Debug.LogWarning(pickSize + " " + pickType);
				sze = (Weapon.WeaponSize) System.Enum.Parse(typeof(Weapon.WeaponSize), sizeArray[pickSize]);
				tpe = (Weapon.WeaponType) System.Enum.Parse(typeof(Weapon.WeaponType), typeArray[pickType]);
				
				//Debug.LogWarning((int)tpe);
				// Get all the corresponding weapon size dictionary.
				//Debug.LogWarning(sze.ToString() + " " + tpe.ToString());
				Weapon.WeaponTypeCache holder = Game.weaponDictionary[sze.ToString() + "|" + tpe.ToString()];

				// Make a list for all the possible weapons in that size.
				List<WeaponAttributes> weaponsPossible = new List<WeaponAttributes>();
				// Insert all weapons possible into that list.
				foreach (var pair in holder.weapons)
				{
					weaponsPossible.Add(pair.Value);
				}

				// Select one of the weapons from that list.
				weaponSelectionAttributes = weaponsPossible[UnityEngine.Random.Range(0,weaponsPossible.Count - 1)];
				weaponSelection = weaponSelectionAttributes.path;
				//
				
				// If weapon is selected exit loop.
				if (weaponSelection.Length > 1)
					break;
				
				// Try 50 times before saying fuck this shit.
				loopCount++;
				if (loopCount > 10)
					return;
			}

			// Instantiate proper turret -- Offset it by space between two rig points. Attach.
			//Debug.Log("Current Position: " + transform.position);
			//Debug.Log("Weapons/" + sze.ToString() + "/" + tpe.ToString() + "/" + weaponSelection);
			GameObject tur = PhotonNetwork.Instantiate("Weapons/" + sze.ToString() + "/" + tpe.ToString() + "/" + weaponSelection, rp.position, rp.rotation, 0) as GameObject;

			Weapon wscript = tur.GetComponent<Weapon>();			
			wscript.SetAttributes(weaponSelectionAttributes);
			wscript.owner = this;
			Weapons.Add(wscript);
			tur.transform.parent = rp;
			//tur.transform.position = wscript.owner.transform.position;
			//tur.transform.localScale = new Vector3(3,3,3);
			// rPoint or rp figure it out!
			
			if (wscript.rigpoint == null)
				wscript.SetRigPoint();
				
			Vector3 offset = rp.position - wscript.rigpoint.position;
			tur.transform.position += offset;
			tur.transform.parent = rp;
			tur.name = weaponSelection;
			
			++rpcount;
		}
		
	}
	
	[RPC] public void RequestTurretSyncRPC(int PlayerID)
	{
		if (!PhotonNetwork.isMasterClient)
			return;
			
		int[] rigPointRelations = new int[RigPoints.Count];
		int[] weaponIDs = new int[Weapons.Count];		
		string[] nameIDs = new string[Weapons.Count];
		
		int ndx = 0;
		foreach (Transform rigP in RigPoints)
		{
			if (rigP.childCount == 0)
			{
				ndx++;
				continue;
			}
			
			weaponIDs[ndx] = rigP.GetComponentInChildren<PhotonView>().viewID;
			rigPointRelations[ndx] = ndx;
			ndx++;
		}
		
		photonView.RPC("SyncTurretsRPC", PhotonPlayer.Find(PlayerID), PlayerID, rigPointRelations, weaponIDs);
	}
	
	[RPC] public void SyncTurretsRPC(int PlayerID, int[] rigPointRelations, int[] weaponIDs)
	{	
		Debug.Log("Syncing Turrets");
		if (PhotonNetwork.player.ID != PlayerID)
			return;
		
		Debug.Log("Player Accepted");	
		
		int count = rigPointRelations.Length;
		
		for (int ndx = 0; ndx < count; ndx++)
		{
			Debug.Log("Syncing Turret " + ndx);
			
			if (weaponIDs[ndx] == 0)
				continue;
			
			Debug.Log("Turret Accepted");
			
			GameObject tur = PhotonView.Find(weaponIDs[ndx]).gameObject;
			Transform rp = RigPoints[rigPointRelations[ndx]];
			Weapon wscript = tur.GetComponent<Weapon>();
			wscript.owner = this;
			Weapons.Add(wscript);
			tur.transform.parent = rp;
			//tur.transform.position = wscript.owner.transform.position;
			//tur.transform.localScale = new Vector3(3,3,3);
			// rPoint or rp figure it out!
			if (wscript.rigpoint == null)
				wscript.SetRigPoint();
				
			Vector3 offset = rp.position - wscript.rigpoint.position;
			tur.transform.position += offset;
			tur.transform.parent = rp;
			//tur.name = wscript.weaponName;
		}		
	}
	
	public void CreateTurretSync(int rigPoint, int weaponSize, int weaponType, string weaponName)
	{
		
	}
	
	public void setShipModelClone(string inFaction, string inType, string inModel)
	{
		
	}
	
	public void setAttributesExternal(Attributes shipatts)
	{
		this.attributes = shipatts;		
	}
	
	bool GetIsFleetLeader()
	{
		return isFleetLeader;
	}
	
	public void SetMyFleet(Fleet inFleet)
	{
		myFleet = inFleet;	
		fleetLeader = myFleet.fleetLeader;
	}
	
	public Fleet GetMyFleet()
	{
		return myFleet;
	}

	public Transform GetTarget()
	{		
		return target;
	}
	
	public void SetTarget(Transform inTarget)
	{
		target = inTarget;
	}
	
	
	protected override void DestroyShip()
	{
		
		// Cannot be destroyed while being set up.
		if (settingUp)
			return;
			
		foreach (Weapon wpn in Weapons)
			PhotonNetwork.Destroy(wpn.gameObject);
			
		FleetManager.aiShipCount--;
		
		myFleet.threat -= attributes.threatLevel;
		myFleet.RemoveAI(this);
		
		PhotonNetwork.Destroy(this.gameObject);
		
		GameObject deathExplosion = GameObject.Instantiate(Resources.Load("Effects/ShipExplosion"), shipObject.transform.position, Quaternion.identity) as GameObject;
		
		bool doExplosion = UnityEngine.Random.Range(0.0f, 1.0f) > 0.5;
		foreach (Transform tf in RigPoints)
		{
			if (doExplosion)
				GameObject.Instantiate(deathExplosion, tf.position, tf.rotation);
			
			doExplosion = !doExplosion;
		}
		

	}
	
	void OnDestroy()
	{
		if (photonView.isMine || PhotonNetwork.room ==  null)
			return;
			
		FleetManager.aiShipCount--;
		
		myFleet.threat -= attributes.threatLevel;
		myFleet.RemoveAI(this);
		
		GameObject deathExplosion = GameObject.Instantiate(Resources.Load("Effects/ShipExplosion"), shipObject.transform.position, Quaternion.identity) as GameObject;
		
		bool doExplosion = UnityEngine.Random.Range(0.0f, 1.0f) > 0.5;
		foreach (Transform tf in RigPoints)
		{
			if (doExplosion)
				GameObject.Instantiate(deathExplosion, tf.position, tf.rotation);
				
			doExplosion = !doExplosion;
		}
	}
			
	[RPC] public void UpVectorRPC(Vector3 inVector)
	{
		behavior.SetUpVector(inVector);
	}
	

	
}

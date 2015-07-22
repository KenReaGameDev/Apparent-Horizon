using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
	
public class Ship : MonoBehaviour{
	
	// Debugging
	public bool DestroyThisShip = false;
	public bool settingUp = true;
	
	public enum ShipClass {
		DEFAULT,			// Default Ship
		SCOUT,				// Single Light Cannon, High Speed
		FRIGATE,			// Mines and Bombs, single cannon
		FIGHTER,			// Dual Light Cannon or Single Heavy Cannon, High Speed
		BOMBER,				// Heavy Missiles or Light Missiles, Low Speed
		DESTROYER,			// Multi Cannon, Single Missile, Medium Speed
		CRUISER, 			// Heavy Missiles, Medium Speed, Light Auto-Cannons
		BATTLECRUISER,		// Heavy Missiles, Slow Speed, Medium Auto-Cannons, Medium Armor
		BATTLESHIP,			// Multi Cannon, Dual Missile, Low Speed, Heavy Armor
		DREADCRUISER,		// Massive Missile Boat
		DREADNAUGHT,		// Massive Multi-Armament, Low Speed, High Armor
		TITAN,				// Capable of Wielding Planet Busters
		GALAXY,				// Capable of Base Building in non-inhabited zones
		OMNI				// Capable of Star Busting
	};

	//public List<string> recentCommands = new List<string>();
	public string modelName;
	public GameObject shipObject;
	public int ID;
	public int currentSystemSeed;
	public ShipClass type;
	public string faction; //private
	public Ship fleetLeader; //protected
	public Fleet myFleet = null; //protected
	public Fleet targetFleet = null;
	public bool isFleetLeader = false; //protected
	public Attributes attributes;
	public Buff buffs;
	public Ability ability = new Ability_None();
	public float health = 0;
	public float maxHealth = 0;
	
	protected ship_control controls;
	protected float resourcesHeld = 0;
	
	public GameObject targetQuad;

	public List<Weapon> Weapons = new List<Weapon>();
	public List<Weapon> NoTarget = new List<Weapon>();

	public Transform target = null; //protected
	
	// For AI
	public BehaviorDeterminerAiShip determiner; //protected
	public AiBehavior behavior;
	public string behaviorName;
	public AiPathing pathing; //protected
	public FleetManager manager;
	
	public List<Transform> CameraPoints = new List<Transform>();
	public int cameraPointsCount = 0;
	public int currentCameraPoint = 0;

	public List<Transform> RigPoints = new List<Transform>();
	public int RigPointsCount = 0;	

	// checking who is in range.
	protected float rangeObjectsDelta = 0;
	public Collider[] inRangeObjects;
	public List<GameObject> inRangeShips = new List<GameObject>();

	public Dictionary<GameObject, float> Aggressors = new Dictionary<GameObject, float>();

	protected int aggCheck = 0;
	protected float aggTime = 0;
	protected Camera currentCamera;
	public Camera UICamera;

	// Movement Information
	public float degreesPerSecond;
	
	
	// Path Finding
	protected bool blocked;
	protected bool blockedStill;
	protected float blockTimer = 0;
	protected Vector3 pathingDirection;
	protected Vector3 collisionPoint;
	protected Transform collisionTransform;
	
	// Timers
	float repairTicks = 0;

	// Networking Information
	public PhotonView photonView;
	

	public Ship()
	{

	}
	
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	public virtual void Update () {

	}
	
	protected void Repairs()
	{	
		ShieldRegen();
		if (buffs.regen.time > 0)
		{
			ArmorRegen();
			HullRegen();
		}
		
		HealthCheck();
	}
	
	protected void ShieldRegen()
	{
		attributes.shieldHealth += (attributes.shieldRepairRate * Time.deltaTime) * buffs.regen.power;
	}
	
	protected void ArmorRegen()
	{
		attributes.armorHealth += (attributes.armorRepairRate * Time.deltaTime) * buffs.regen.power;
	}
	
	protected void HullRegen()
	{
		attributes.structureHealth += (attributes.structureRepairRate * Time.deltaTime) * buffs.regen.power;
	}
	
	protected void HealthCheck()
	{
		if (attributes.shieldHealth > (attributes.maxShieldHealth * buffs.shield.power))
		    attributes.shieldHealth = attributes.maxShieldHealth * buffs.shield.power;
		   
		if (attributes.structureHealth > attributes.maxStructureHealth)
			attributes.structureHealth = attributes.maxStructureHealth;
			
		if (attributes.armorHealth > attributes.maxArmorHealth)
			attributes.armorHealth = attributes.maxArmorHealth;
		
	}
	
	void SaveShip () {
		// Save all information about ship.
	}
	
	
	public string getFaction()
	{
		return faction;	
	}	

	public Attributes GetAttributes()
	{
		return attributes;	
	}

	protected void CalculateAttributes()
	{
		degreesPerSecond = (attributes.rotationsPerMinute * 360) / 60;
		SetMaxHealth();
		CalculateHealth();
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
	
	public void setFleetLeader(Ship inShip)
	{
		fleetLeader = inShip;
	}
	
	public void passFleetLeader(Ship inShip)
	{
		
	}
	
	public void setAttributesInternal(string inShip)
	{
		this.attributes = Ships.LoadAiShip(inShip);
		//setShipModelLoadInternal();
		//setShipModelLoad(this);
	}
	
	public GameObject getShipObject()
	{
		return this.shipObject;	
	}
	
	public void setShipModelLoadInternal()
	{
	
	}
	
//	public Ship setShipModelLoad(Ship inShip)
//	{	
//
//	}
	
	public void setShipModelClone(string inFaction, string inType, string inModel)
	{
		
	}
	
	public void setAttributesExternal(Attributes shipatts)
	{
		this.attributes = shipatts;		
	}
	
	public void SetIsFleetLeader(bool inBool, Ship inShip)
	{
		isFleetLeader = inBool;
		if (isFleetLeader)
			determiner = new BehaviorDeterminerAiShip(inShip);
	}
	
	bool GetIsFleetLeader()
	{
		return isFleetLeader;
	}
	
	public void SetMyFleet(Fleet inFleet)
	{
		myFleet = inFleet;	
	}
	
	public Fleet GetMyFleet()
	{
		return myFleet;
	}
	
	public AiBehavior GetBehavior()
	{
		return behavior;
	}
	
	public void SetBehavior(AiBehavior inBehavior)
	{
		//Debug.Log("BehaviorChangeRPC to " + inBehavior.ToString() + " ship " + this.gameObject.name);
		
		if (inBehavior != null)
			behavior = inBehavior;	
		
		behaviorName = inBehavior.GetType().ToString();
			
		//Debug.Log("BehaviorChanged to " + behavior.ToString() + " ship " + this.gameObject.name);
	}
	
	public Transform GetTarget()
	{		
		return target;
	}
	
	public void SetTargetShip(Ship inship)
	{
		if (inship == null)
			return;
		
		target = inship.gameObject.transform;
		targetFleet = inship.GetMyFleet();	
		photonView.RPC("SetTargetShipRPC", PhotonTargets.Others, inship.photonView.viewID);
	}
	
	[RPC] public void SetTargetShipRPC(int inTargetID)
	{
		GameObject tShip = PhotonView.Find(inTargetID).gameObject.gameObject;
		if (tShip == null)
			return;
			
		target = tShip.transform;
		targetFleet = target.GetComponent<Ship>().GetMyFleet();
	}
	
	public void SetTargetTransform(Transform inTarget)
	{
		target = inTarget;
		PhotonView pv = target.GetComponent<PhotonView>();
		
		if (photonView.isMine && pv != null)
			photonView.RPC("SetTargetShipRPC", PhotonTargets.Others, pv.viewID);	
	}
	
	

	protected void PopulateRigPoints()
	{
		RigPointsCount = 0;
		Transform[] tmp = this.GetComponentsInChildren<Transform>();
		for (int i = 0; i < tmp.Length; ++i)
		{			
			if (tmp[i].tag == "RigPoint")
			{
				RigPoints.Add(tmp[i]);
				RigPointsCount++;	
			}
		}		
	}

	protected void PopulateCameraPoints()
	{
		Transform[] tmp = this.GetComponentsInChildren<Transform>();
		for (int i = 0; i < tmp.Length; ++i)
		{
			if (tmp[i].tag == "CameraPoint")
				CameraPoints.Add(tmp[i]);
		}		
		cameraPointsCount = CameraPoints.Count;
	}
	
	protected void SetMaxHealth()
	{
		maxHealth = attributes.maxShieldHealth + attributes.maxArmorHealth + attributes.maxStructureHealth;
	}
	
	protected void CalculateHealth()
	{
		health = ((attributes.structureHealth + attributes.armorHealth + attributes.shieldHealth) * 100) / maxHealth;
	}
	
	protected void CheckCollidersInRange()
	{
		inRangeShips.Clear();

		if (targetFleet == null)
			return;

		inRangeObjects = Physics.OverlapSphere(transform.position, attributes.range);


		for (int ndx = 0; ndx < inRangeObjects.Length; ++ndx)
		{
			if (inRangeObjects[ndx].gameObject.tag == "Ship")
			{
				inRangeShips.Add(inRangeObjects[ndx].gameObject);
			}
		}

		rangeObjectsDelta = 0;
	}

	protected void CheckCollidersDelegate()
	{
		if ((rangeObjectsDelta += Time.deltaTime) > 2)
			CheckCollidersInRange();
	}

	protected void RemoveWeapon(Weapon inWeapon)
	{
		Weapons.Remove(inWeapon);
		Destroy(inWeapon.gameObject);
	}

	protected void RemoveWeapon(GameObject inWeapon)
	{
		Weapon wpn = inWeapon.GetComponent<Weapon>();
		Weapons.Remove(wpn);
		Destroy(inWeapon);
	}

	public void DamageRecieve(DamageMessage message)
	{
		// Don't allow player to hit player and AI to hit AI
		if (message.hasShip && message.player != null)
		{
			bool isPlayer = false;
			if (this is PlayerShip)
				isPlayer = true;
				
			if (isPlayer == message.player)
				return;
		}
		
		rigidbody.AddForce(message.damageForce * message.damageDirection);
		CalculateDamages(message);
	}

	void CalculateDamages(DamageMessage message)
	{

		float damageLeft = message.damage;
		float shieldChange = 0;
		float armorChange = 0;
		float structureChange = 0;
		
		if (attributes.shieldHealth > 0 && damageLeft > 0)
		{
			shieldChange = DamageModifier(damageLeft, message.bias, Weapon.DamageBias.SHIELD);
			attributes.shieldHealth -= shieldChange;
			damageLeft -= attributes.shieldHealth;
			if (attributes.shieldHealth < 0)
				attributes.shieldHealth = 0;

		}

		if (attributes.armorHealth > 0 && damageLeft > 0)
		{
			armorChange = DamageModifier(damageLeft, message.bias, Weapon.DamageBias.ARMOR);
			attributes.armorHealth -= armorChange;
			damageLeft -= attributes.armorHealth;
			if (attributes.armorHealth < 0)
				attributes.armorHealth = 0;
		}

		if (attributes.structureHealth > 0 && damageLeft > 0)
		{
			////Debug.Log(attributes.structureHealth + " Ship Before: " + name);
			structureChange = DamageModifier(damageLeft, message.bias, Weapon.DamageBias.HULL);
			attributes.structureHealth -= structureChange;
			damageLeft -= attributes.structureHealth;
			
			////Debug.Log(attributes.structureHealth + " Ship After: " + name);
			if (attributes.structureHealth <= 0 && photonView.isMine)
			{
				DestroyShip();
				return;
			}
		}
		
		CalculateHealth();
		
		photonView.RPC("DamageSyncRPC", PhotonTargets.Others, message.viewID, shieldChange, armorChange, structureChange, health);
		if (message.owner != null && message.owner.gameObject != null)
			AddAggressors(message.owner.gameObject);
	}

	[RPC] public void DamageSyncRPC(int objectID, float shieldChange, float armorChange, float structureChange, float currentHealth)
	{
		//Debug.Log("[RPC] Damage Taken.");
		AddAggressors(PhotonView.Find(objectID).gameObject);
		
		if (attributes == null)
			return;
			
		attributes.shieldHealth -= shieldChange;
		attributes.armorHealth -= armorChange;
		attributes.structureHealth -= structureChange;
		CalculateHealth();
		
		float varianceAcceptable = health * 0.1f;
		
		if (currentHealth > health + varianceAcceptable || currentHealth < health - varianceAcceptable)
		{
			photonView.RPC("SyncHealthRPC", PhotonTargets.MasterClient, null);
		}
	}
	
	[RPC] public void SyncHealthRPC()
	{
		photonView.RPC("SyncHealthRPC", PhotonTargets.Others, attributes.armorHealth, attributes.shieldHealth, attributes.structureHealth);
	}
	
	[RPC] public void SyncHealthRPC(float armor, float shield, float structure)
	{
		attributes.shieldHealth = shield;
		attributes.armorHealth = armor;
		attributes.structureHealth = structure;
	}
	
	protected virtual void DestroyShip()
	{
		//Debug.Log("Called in Ship Class Bad!!");
	}

	float DamageModifier(float inDamage, Weapon.DamageBias inBias, Weapon.DamageBias inHit)
	{
		if (inBias == Weapon.DamageBias.NONE)
			return inDamage;

		if (inBias != inHit)
			return inDamage * 0.75f;

		return inDamage * 1.25f;
	}
	


	protected void AddAggressors(GameObject go)
	{
		if (Aggressors.ContainsKey(go))
			return;

		Aggressors.Add(go, 5);
	}

	protected void AggressorChecks()
	{
		if ((aggTime += Time.deltaTime) < 1 || Aggressors.Count < 1)
			return;

		foreach (GameObject go in Aggressors.Keys.ToList())
			Aggressors[go] -= aggTime;
		
		aggTime = 0;
		
		var removeEntries = Aggressors.Where(pair => pair.Value == 0 || pair.Key == null).ToList();
		
		foreach (var delete in removeEntries)
		{
			Aggressors.Remove(delete.Key);
		}
		



//		// Every 5 seconds check to see if aggressor arrays are still holding the correct values.
//		try
//		{
//			if ((aggTime + Time.deltaTime) > 5 && AggressorObjects.Count > 0) 
//			{
//				//Debug.Log("Aggressor Count on Start: " + AggressorObjects.Count);
//				int aggCount = AggressorTime.Count;
//				for (int ndx = 0; ndx < aggCount; ++ndx)
//				{
//					AggressorTime[ndx] -= aggTime;
//					if (AggressorTime[ndx] <= 0)
//					{
//						AggressorObjects.RemoveAt(ndx);
//						Aggressors.RemoveAt(ndx);
//						AggressorTime.RemoveAt(ndx);
//						--ndx;
//						--aggCount;
//						//Debug.Log("Removed Aggressor. Aggressor Count is now: " + AggressorObjects.Count);
//					}
//				}
//				aggTime = 0;
//			}
//		}
//		catch
//		{
//			//Debug.Log("Aggressor Count on Error: " + AggressorObjects.Count);
//			
//		}
//		
//		if (AggressorObjects.Count > 0 && AggressorObjects[aggCheck] == null)
//			AggressorObjects.RemoveAt(aggCheck);

	}

	public Camera GetCurrentCamera()
	{
		return currentCamera;
	}

	private void Repair()
	{
		if ((repairTicks += Time.deltaTime) < 0.25f)
			return;

		repairTicks -= 0.25f;

		float multiplier = buffs.regen.power;

		// Dont Regen Shields when disabled.
		if (buffs.disable.time == 0 && attributes.shieldHealth < attributes.maxShieldHealth * buffs.shield.power)
			attributes.shieldHealth += multiplier *= attributes.shieldRepairRate;

		if (attributes.armorHealth < attributes.maxArmorHealth * buffs.armor.power)
			attributes.armorHealth += multiplier * attributes.armorRepairRate;

		// Dont overheal.
		if (attributes.armorHealth > attributes.maxArmorHealth * buffs.armor.power)
			attributes.armorHealth = attributes.maxArmorHealth;
		if (attributes.shieldHealth > attributes.maxShieldHealth * buffs.shield.power)
			attributes.shieldHealth = attributes.maxShieldHealth;
		// Hull is repaired by drones or by paying for it while docking / being close to the mobile base..

	}

	
	protected void PathingCheck()
	{
		if (!target)
			return;
		
		rigidbody.AddForce(Vector3.Normalize(target.position - transform.position));
		
		if (blocked)
			blockTimer += Time.deltaTime;
		
		if (blockTimer > 2)
			RecheckBlocked();
		
		if (blockedStill)
		{
			MoveObject();
		}
	}
	
	void OnCollisionEnter(Collision col)
	{
		if (col.collider.isTrigger)
			return;
			
		blocked = true;
	}
	
	protected void RecheckBlocked()
	{
		pathingDirection = target.position - transform.position;
		float distanceToTarget = Vector3.Distance(transform.position, target.position);
		Ray pathCheck = new Ray(transform.position, pathingDirection);
		RaycastHit hitCheck;
		
		if (Physics.Raycast(pathCheck, out hitCheck, 150.0f))
		{
			if (hitCheck.collider.transform != target)
			{
				blockedStill = true;
				collisionTransform = hitCheck.collider.transform;
				collisionPoint = hitCheck.point;
			}
		}
		else
			blockedStill = false;
	}
	
	// Move the object to the other side of the blocking object with proper pathingDirectionection.
	protected void MoveObject()
	{
		// Get the distances in vector form to both the center of the object and the collision point with the object.
		Vector3 vectorToBlock = collisionTransform.position - transform.position;
		Vector3 vectorToCollision = collisionPoint - transform.position;
		
		// Calculate the vector distance from the middle of the blocking object and the collision.
		Vector3 difference = vectorToBlock - vectorToCollision;
		// Get new position using the distance and then a bit more to avoid collision.
		Vector3 escape = collisionTransform.position + (difference * 1.2f);
		
		// Move ship.
		transform.position = escape;
		rigidbody.angularVelocity = Vector3.zero;
		// Reset blocking parameters.
		ResetBlocks();
		
	}
	
	/// <summary>
	/// Resets if ship is blocked + Timers.
	/// </summary>
	protected void ResetBlocks()
	{
		blockTimer = 0;
		blocked = false;
		blockedStill = false;
	}
	
	/// <summary>
	/// Adds Amount of resource to cargo
	/// </summary>
	/// <param name="amount">Amount.</param>
	public void AddResoruce(float amount)
	{
		resourcesHeld += amount;
	}
	
	/// <summary>
	/// Returns amount of resources held for deposit
	/// </summary>
	/// <returns>Amount of Resources Held.</returns>
	public float DepositResources()
	{
		float temp = resourcesHeld;
		resourcesHeld = 0;
		return temp;
	}
	
	
	[RPC] public void RequestBehaviorRPC(int PlayerID)
	{
		if (!PhotonNetwork.isMasterClient)
			return;
			
		behavior.RequestBehavior(PlayerID);
	}
	
	// Network Code
	[RPC] public void SetUpShipRPC()
	{
		//Debug.Log(this.name + "[RPC] Set up Ship.");
		PopulateRigPoints();
		PopulateCameraPoints();
		photonView = GetComponent<PhotonView>();
	}
	
	
	// Behavior RPC Syncing.
	[RPC] public void SearchRPC(Vector3 beaconPosition)
	{	
		//Debug.Log("[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
			
		SetBehavior(new Search(this, beaconPosition));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void RepairRPC()
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
			
		SetBehavior(new Repair(this));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void SearchFollowRPC()
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
		
		SetBehavior(new SearchFollow(this));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void SoloEngageRPC(Vector3 psuedoVector, int shipID)
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
		
		SetBehavior(new SoloEngage(this));
		behavior.SetUpVector(psuedoVector);
		behaviorName = behavior.GetType().ToString();
		SetTargetShipRPC(shipID);
	}
	
	[RPC] public void TeamEngageRPC(Vector3 psuedoVector, int shipID)
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
		
		SetBehavior(new TeamEngage(this));
		behavior.SetUpVector(psuedoVector);
		behaviorName = behavior.GetType().ToString();
		SetTargetShipRPC(shipID);
	}
	
	[RPC] public void SoloFleeRPC(Vector3 psuedoVector)
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
		
		SetBehavior(new SoloFlee(this));
		behavior.SetUpVector(psuedoVector);
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void TeamFleeRPC()
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;
		
		SetBehavior(new TeamFlee(this));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void RegroupRPC()
	{
	
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		if (this is PlayerShip)
			return;		
		
		SetBehavior(new Regroup(this));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void ProtectRPC()
	{
		if (this is PlayerShip)
			return;
		
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		SetBehavior(new Protect(this));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void DoNothingRPC()
	{
		//Debug.Log(this.name + "[RPC] BehaviorChangeRPC");
		
		if (this is PlayerShip)
			return;		
		
		SetBehavior(new DoNothing(this));
		behaviorName = behavior.GetType().ToString();
	}
	
	[RPC] public void UpdateBeaconRPC(Vector3 beaconVector)
	{
		behavior.SyncBeaconPosition(beaconVector);
	}	
	
	public ship_control GetShipControls()
	{
		return controls;
	}
}

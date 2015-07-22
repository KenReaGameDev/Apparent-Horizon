using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Weapon : MonoBehaviour{

	
	#region Enums
	public struct WeaponTypeCache {
		public Dictionary <string, WeaponAttributes> weapons;
		public WeaponSize size;
	};
	
	public enum FireOrientation {
		FORWARD = 0,
		BACKWARD,
		UP,
		DOWN,
		LEFT,
		RIGHT
	};

	public enum WeaponSize {
		STATIC = 0,			// No Rotation (Bomb Pods, Forward Firing Weapons, EMP)
		SMALL,				// Light varients of Missiles and Lasers
		LARGE,				// Heavy Missiles / Heavy Lasers
		UBER,				// Super Weapons
		NONE
	};

	public enum TargetingAggression {
		WIMP = 2,		// Only targets Enemies
		REGULAR = 5,	// Targets Potentials and enemies
		STRONG = 10,	// Targets Neutral Potential and Enemies
		ALL = 99999		// Kills fucking everything for no reason at all.
	};

	public enum WeaponType {
		LASER,		// Single Shot
		CLASER,		// Continous Laser
		FLAK,		// For small targets
		MISSILE,	// Tracks Target based on type
		ROCKET,		// Straight Line Missile
		BOMB,
		NONE
	};

	public enum DamageBias {
		SHIELD = 0,	// Shields
		ARMOR,	// Armor
		HULL,	// Structure
		ROCK,	// Planets
		NONE
	};
	
	public enum ControlMode {
		OFF = 0,
		AI,
		PLAYER
	};
	#endregion


	// Owner Information
	public Ship owner;
	public GameObject goOwner;
	public GameObject tracerObject;
	public Transform  Target;
	
	protected WeaponAttributes attributes;
	protected 	float 	distance;
	
	// Weapon Information
	public string weaponName;
	public WeaponType type;
	public WeaponSize size;
	public DamageBias bias = DamageBias.NONE;
	public TargetingAggression aggression = TargetingAggression.REGULAR;
	public ControlMode control = ControlMode.AI;
	public FireOrientation fireOrient = FireOrientation.FORWARD;
	public float fireRate;
	public float fireDelta;
	public float range = 1;
	public float rayTimer = 0;
	public bool automaticTarget = true;
	public bool weaponOn = true;
	public bool swtester = false;
	public float damage = 0;
	public float maxRot = 360;
	public float minRot = 270;
	
	public GameObject firingParticle;
	public GameObject hitParticle;
	
	// For multi Firepoint Setups.
	public List<GameObject> firingParticles = new List<GameObject>();
	public List<GameObject> hitParticles = new List<GameObject>();
	
	protected bool canFire = false;
	
	// Rigidbody.constraints
	Quaternion 	rotation;
	Vector3		position;
	Vector3		direction;
	Vector3 	minRotationRange;
	Vector3 	maxRotationRange;
	Vector4 	color;
	float		maxRotationf = 0;	// Float for Tracking Turrets
	float		minRotationf = 0;	// Float for Tracking Turrets
	int 		failedShots = 0;
	int			frameCount = 0;
	int 		checkRate = 30;

	public Transform rigpoint;
	protected Transform turretBase;
	protected Transform turretCannons;
	protected Vector3 	targetDirection; 
	public TargetingSystem shipTargetingSystem;	

	BoxCollider	targetingSystem;
	GameObject	targetingBeacon;
	Transform 	firePoint;

	// Target Information
	
	protected 	float   	trackingTime = 0;
	protected 	float		reaquireTime = 0;
	protected 	Vector3 	targetPredictedPostion = Vector3.zero;
	protected 	Vector3		acceleration = Vector3.zero;
	protected 	Vector3 	prevVelocity = Vector3.zero;
	protected 	List<Transform> 	potentialTargets 	= new List<Transform> ();

	// Rigging information
	protected List<Transform>	firePoints 			= new List<Transform>();
	protected List<float>		firePointDelta 		= new List<float>();

	// Debug Information
	public List<string> shotData = new List<string>();
	// Potential Behaviors
	MissileBehavior mBehavior;

	// UI Information.
	public UISprite UI_weaponSprite;
	public UISprite UI_reloadSprite;
	public UISprite UI_selectedSprite;
	public UILabel  UI_weaponLabel;
	public UIButton UI_AutoFire;
	public UIButton UI_SelectWeapon;
	
	bool labelSwap = false;

	public GameObject UI_Weapon;
	TargetingUI UI_Targeting;

	// Weapon Constructing Information
	public float cost;
	public bool researched;


	// Networking Information
	public PhotonView photonView;
	protected float toggleTimer = 0;
	
	protected virtual void Targeting()
	{
		targetDirection = turretBase.InverseTransformPoint(Target.position);
		targetDirection.y = 0;		
		targetDirection = turretBase.TransformPoint(targetDirection);
		turretBase.LookAt(targetDirection, turretBase.up);
		turretCannons.LookAt(Target, turretCannons.up);
	}

	//Camera.main.renderer
	public Weapon()
	{

	}
	
	protected void DetermineUpdate()
	{
		if (!weaponOn)
			return;

		if (control == ControlMode.PLAYER && Input.GetKeyDown(KeyCode.Space) && shipTargetingSystem.currentWeapon == this)
			SetTargetKey();
		
		if (size != WeaponSize.STATIC && !Target || shotData.Count > 15 )
		{
			if (!AttemptTargetAcquisition())
				return;
		}

		switch (size)
		{
			case WeaponSize.STATIC:
				StaticAxisUpdate();
			break;

			case WeaponSize.SMALL: case WeaponSize.LARGE: case WeaponSize.UBER:
				TwoAxisUpdate();
			break;
		}
	}
	
	protected void NonLocalUpdate()
	{			
		switch (size)
		{
		case WeaponSize.STATIC:
			break;
			
		case WeaponSize.SMALL: case WeaponSize.LARGE: case WeaponSize.UBER:
			if (!Target)
				return;
			Targeting();
			break;
		}
	}

	// Acquires targets for questions
	bool AttemptTargetAcquisition()
	{

		// If weapon has no owner, do not attempt aquisition.
		if (!owner || !automaticTarget || (reaquireTime += Time.deltaTime) < 3)
		{
			return false;
		}

		shotData.Clear();
//		if (control == ControlMode.PLAYER)
//			Debug.LogWarning("ready to check");

		reaquireTime = 0;

		if (control == ControlMode.AI)
			return AcquireTargetForAI();
		else if (control == ControlMode.PLAYER)
			return AcquireTargetForPlayer();

		return false;
	}

	bool AcquireTargetForAI()
	{
		
		if (size != WeaponSize.STATIC)
		{
			Ray ray;
			RaycastHit hit;
			
			// Check all objects in range for target capability.
			foreach (GameObject go in owner.inRangeShips)
			{
				if (go.rigidbody == null)
					continue;
				
				// Make sure we are targeting an eligable fleet / target.
				Ship shipTarget = go.GetComponent<Ship>();
				if (shipTarget.GetMyFleet() != owner.targetFleet)
					continue;
				
				direction = turretCannons.position - go.transform.position;
				ray = new Ray(firePoints[0].position , direction);
				if (Physics.Raycast(ray, out hit, owner.attributes.range))
				{					
					if (hit.collider.gameObject == go)
					{
						Target = go.transform;
						TargetChangeFSM();
						return true;
					}	
				}
			}
		}
		

		// Attempt to fire upon owners target.
		if (owner.target != null && owner.target.rigidbody != null)
		{
			Target = owner.target;
			return true;
		}

		return false;
	}

	bool AcquireTargetForPlayer()
	{
		if (size != WeaponSize.STATIC && shipTargetingSystem != null)
		{

			//Debug.LogWarning("Aquiring Target");

			Ray ray;
			RaycastHit hit;
			
			// Check all objects in range for target capability.
			foreach (TargetUIInfo targetcheck in shipTargetingSystem.targets)
			{
				// Make sure we are targeting an eligable fleet / target.

				if ((int)targetcheck.GetRelation() > 2)
				{
					//Debug.LogWarning("Not an Enemy - Relation was :: " + (int)targetcheck.GetRelation());
					continue;
				}

				if (targetcheck.targetTF == null)
				{
					//Debug.LogWarning("Enemy had no Transform");
					continue;

				}

				direction = targetcheck.targetTF.position - firePoints[0].position ;
				ray = new Ray(firePoints[0].position, direction);
				if (Physics.Raycast(ray, out hit, range))
				{					
					if (hit.transform.root.gameObject == targetcheck.target)
					{
						//Debug.LogWarning("Enemy Locked");
						Target = targetcheck.targetTF;
						targetcheck.TargetedAutomatically();
						TargetChangeFSM();
						return true;
					}	
					else
					{

						//Debug.LogWarning("Enemy Could not be hit. Hit :: " + hit.collider.name + " instead.");
						continue;
					}					 
				}
				else
				{
					//Debug.LogWarning("Enemy was not in range");
					continue;
				}
			}
		}
		return false;
	}
	protected virtual void StaticAxisUpdate()
	{
		
	}
	
	// For moving turrets.
	protected virtual void TwoAxisUpdate()
	{
		
	}

	// Update is called once per frame
	void Update () {
	
	}

	// See if current rotation can hit target.
	bool TraceShot()
	{
		
		return false;
	}

	protected void GetAllFiringPointsGO(GameObject inObject)
	{
		Transform[] tmp = inObject.GetComponentsInChildren<Transform>();
		for (int ndx = 0; ndx < tmp.Length; ++ndx)
		{
			if (tmp[ndx].gameObject.tag == "FirePoint")
				firePoints.Add(tmp[ndx]);
		}
	}
	
	protected void GetAllFiringPoints()
	{
		Transform tf = this.transform;

		if (size == WeaponSize.STATIC)
			tf = turretBase;
		else
			tf = turretCannons;

		Transform[] tmp = tf.GetComponentsInChildren<Transform>();
		for (int ndx = 0; ndx < tmp.Length; ++ndx)
		{
			if (tmp[ndx].tag == "FirePoint")
				firePoints.Add(tmp[ndx]);
		}
	}
	
	public void SetRigPoint()
	{
		Transform[] tmp = gameObject.GetComponentsInChildren<Transform>();
		
		for (int ndx = 0; ndx < tmp.Length; ++ndx)
		{
			if (tmp[ndx].name == "RigPoint")
				rigpoint = tmp[ndx];
		}

		if (rigpoint == null)
		{
			Debug.LogError("Rigpoint still null");
		}
	}
	
	public void SetRigPoint(Transform rp)
	{
		rigpoint = rp;
	}
	
	/// <summary>
	/// If Weapon Requires Static Rigpoint, Enable it and Use it.
	// </summary>
	public void RequiresStaticRigPoint()
	{
		Transform nrp = transform.FindChild("RigPointStatic");
		
		if (nrp == null)
			return;
		else
			rigpoint = nrp;
		// Enable the mesh renderer for the connection piece.
		MeshRenderer meshPart = rigpoint.GetComponentInChildren<MeshRenderer>();
		
		if (meshPart != null)
			meshPart.enabled = true;		
	}
	
	protected void SetTransforms()
	{
	
		if (rigpoint == null)
		{
			rigpoint = transform.FindChild("RigPoint");
		}
		
		Transform[] tmp = gameObject.GetComponentsInChildren<Transform>();
		
		if (size == WeaponSize.STATIC)
		{
			for (int ndx = 0; ndx < tmp.Length; ++ndx)
			{
				if (tmp[ndx].tag == "TurretBody")
					turretBase = tmp[ndx];
				
				turretCannons = turretBase;
			}
			
			return;
		}
		
		for (int ndx = 0; ndx < tmp.Length; ++ndx)
		{
			if (tmp[ndx].tag == "TurretBody")
				turretBase = tmp[ndx];
			
			if (tmp[ndx].tag == "TurretGuns")
				turretCannons = tmp[ndx];
		}
	}
	
	protected void Constraint()
	{		
		if (turretCannons.localEulerAngles.x > maxRot || turretCannons.localEulerAngles.x < minRot)
			turretCannons.localEulerAngles = new Vector3(maxRot, 0, 0);		
	}
	
	// Returns the direction the missile should launch at based on what type of weapon it is.
	protected Vector3 returnFireOrientation()
	{
		switch (fireOrient)
		{
		case FireOrientation.FORWARD:
			return transform.forward;
		case FireOrientation.UP:
			return transform.up;
		}

		// Just incase it bugs out.
		return transform.forward;
	}

	protected void PredictPosition(float inShotSpeed)
	{
		acceleration = Target.rigidbody.velocity - prevVelocity;
		prevVelocity =  Target.rigidbody.velocity;
		float distance = Vector3.Distance(transform.position, Target.position);
		float timeToTarget = distance / inShotSpeed;
		// Most of the time shots are too far ahead.
		// Changed acceleration to half but I may want to change time a bit instead. 
		targetPredictedPostion = (timeToTarget * (Target.rigidbody.velocity + acceleration)) + Target.position;
		trackingTime = 0;
	}	

	public Transform GetBase()
	{
		return turretBase;
	}
	
	protected void CheckPhotonView()
	{		
		if (photonView == null)
			photonView = GetComponent<PhotonView>();
	}
	
	protected void UpdateLaserPosition(LineRenderer laser)
	{
		laser.SetPosition(0, firePoints[0].position);
		laser.SetPosition(1, firePoints[0].position);
	}

	protected void AcquireTargetingSystem()
	{
		if (owner is PlayerShip)
		{
			shipTargetingSystem = owner.gameObject.GetComponent<TargetingSystem>();
			shipTargetingSystem.weapons.Add(this);
			control = ControlMode.PLAYER;

			// Add weapon to GRID.
			UI_Targeting = shipTargetingSystem.GetUI();
			UI_Weapon = GameObject.Instantiate(UI_Targeting.UIOBJECT_WEAPON) as GameObject;
			UI_Weapon.name = "WeaponsUI";

			Debug.Log(UI_Weapon.transform.GetChild(0).name);
			Debug.Log(UI_Weapon.transform.GetChild(1).name);
			Debug.Log(UI_Weapon.transform.GetChild(2).name);

			UI_weaponLabel = UI_Weapon.transform.FindChild("UILabel").GetComponent<UILabel>();
			UI_reloadSprite = UI_Weapon.transform.FindChild("UIReadyToFire").GetComponent<UISprite>();
			UI_weaponSprite = UI_Weapon.transform.FindChild("UIWeaponSprite").GetComponent<UISprite>();
			UI_selectedSprite = UI_Weapon.transform.FindChild("UISelected").GetComponent<UISprite>();
			UI_SelectWeapon = UI_Weapon.transform.FindChild("UIWeaponSprite").GetComponent<UIButton>();

			// Allows changing weapon and setting targets by left / right clicking.
			EventDelegate SelectDelegate = new EventDelegate(this, "SelectWeapon");
			UI_SelectWeapon.onClick.Add(SelectDelegate);
			SelectDelegate = new EventDelegate(this, "SetTarget");
			UI_SelectWeapon.onClick.Add(SelectDelegate);

			UI_selectedSprite.enabled = false;

			//UI_AutoFire.onClick += SwitchModes;
//			UIEventListener.Get(UI_AutoFire.gameObject).onClick += SwitchModes;
//
			UI_Weapon.transform.parent = UI_Targeting.weaponGrid.transform;
			UI_Weapon.transform.localPosition = Vector3.zero;
			UI_Weapon.transform.localScale = new Vector3(1,1,1);
			UI_Targeting.weaponGrid.mReposition = true;
			UI_Targeting.weaponGrid.enabled = true;
		}
		else 
			control = ControlMode.AI;
	}

	public void SwitchModes()
	{

		automaticTarget = !automaticTarget;
		Debug.LogWarning("Switch Modes Clicked");
	}

	public void SelectWeapon()
	{
		if (UICamera.currentTouchID != -1)
			return;

		Debug.LogWarning("Weapon Selected :: " + this.weaponName);
		if (shipTargetingSystem.currentWeapon != null && shipTargetingSystem.currentWeapon != this)
		{
			shipTargetingSystem.currentWeapon.DeselectWeapon();
			labelSwap = false;
		}

		// Checking if we want to switch weapon name and target name
		if (shipTargetingSystem.currentWeapon == this)
		{
			// Swap variable
			labelSwap = !labelSwap;

			// If no target then we don't  have target names.
			if (labelSwap && Target != null)
				UI_weaponLabel.text = Target.name;
			else
				UI_weaponLabel.text = weaponName;
		}
		else
		{
			UI_weaponLabel.text = weaponName;
		}

		shipTargetingSystem.currentWeapon = this;
		UI_selectedSprite.enabled = true;
	}

	public void SelectWeaponKey()
	{

		Debug.LogWarning("Weapon Selected :: " + this.weaponName);

		if (shipTargetingSystem.currentWeapon != null && shipTargetingSystem.currentWeapon != this)
		{
			shipTargetingSystem.currentWeapon.DeselectWeapon();
			labelSwap = false;
		}
		
		// Checking if we want to switch weapon name and target name
		if (shipTargetingSystem.currentWeapon == this)
		{
			// Swap variable
			labelSwap = !labelSwap;
			
			// If no target then we don't  have target names.
			if (labelSwap && Target != null)
				UI_weaponLabel.text = Target.name;
			else
				UI_weaponLabel.text = weaponName;
		}
		else
		{
			UI_weaponLabel.text = weaponName;
		}
		
		shipTargetingSystem.currentWeapon = this;
		UI_selectedSprite.enabled = true;
	}


	
	public void SetNoTarget()
	{
		Target = null;
		photonView.RPC("NoTargetRPC", PhotonTargets.Others, null);
	}
	
	public void SetTarget()
	{
		if (UICamera.currentTouchID != -2)
			return;

		Debug.LogWarning("Target Selected");
		if(shipTargetingSystem.currentTarget != null && shipTargetingSystem.currentWeapon == this)
		{
//			if (Target != shipTargetingSystem.currentTarget.targetTF)
//				Target = shipTargetingSystem.currentTarget.targetTF;
//			else
//				Target = null;

			Target = shipTargetingSystem.currentTarget.targetTF;
			//
			Debug.LogWarning("Target Selected :: " + Target.name);
			TargetChangeFSM();
		}
	}

	public void SetTargetKey()
	{		
		Debug.LogWarning("Target Selected");
		if(shipTargetingSystem.currentTarget != null && shipTargetingSystem.currentWeapon == this)
		{			
			Target = shipTargetingSystem.currentTarget.targetTF;
			Debug.LogWarning("Target Selected :: " + Target.name);
			TargetChangeFSM();
		}		
	}
	
	public void SetTargetAll()
	{
		if(shipTargetingSystem.currentTarget != null)
		{
			Target = shipTargetingSystem.currentTarget.targetTF;
			Debug.LogWarning("Target Selected :: " + Target.name);
			TargetChangeFSM();
		}
	}
	
	void TargetChangeFSM()
	{
		Debug.Log("Sending Target Information");
		if (Target == null)
			return;
		
		PhotonView targetPhotonView = Target.gameObject.GetComponent<PhotonView>();
		if (targetPhotonView != null)
		{
			photonView.RPC("ChangeTargetRPC", PhotonTargets.Others, targetPhotonView.viewID);
			Debug.Log("Sending Target view PhotonView");
		}
		else
			photonView.RPC("ChangeTargetNoViewRPC", PhotonTargets.Others, Target.gameObject.name);
	}
	
	[RPC] void NoTargetRPC()
	{
		Target = null;
	}
	
	public void DeselectWeapon()
	{
		UI_selectedSprite.enabled = false;
	}

	protected void PopulateUI()
	{

	}

	public void SwapOut()
	{
		owner.Weapons.Remove(this);
		shipTargetingSystem.weapons.Remove(this);
		GameObject.Destroy(UI_Weapon);
	}

	protected virtual void ReadyToFireColor()
	{

	}
	
	/// <summary>
	/// Sets up the weapon for other players on the network.
	/// </summary>
	/// <param name="parent">Parent.</param>
	/// <param name="rigPnt">Rig point.</param>
	/// <param name="ownr">(Ship) Owner Script.</param>
	/// <param name="wpnName">Weapon Name.</param>
	[RPC] public void BasicSetupRPC(int objectID, int currentRP, string wpnName)
	{
		Debug.Log("[RPC] Set up Weapon.");
		this.owner = PhotonView.Find(objectID).GetComponent<Ship>();

		
		Debug.Log("RPC OWNER :: " + owner.name);
		
		if (rigpoint == null)
			SetRigPoint();
			
		transform.localPosition = Vector3.zero;
		this.transform.parent = owner.RigPoints[currentRP];
		//this.gameObject.transform.localScale = new Vector3(3,3,3);
		Vector3 offset = owner.RigPoints[currentRP].position - rigpoint.position;
		this.gameObject.transform.position += offset;
		
		this.weaponName = wpnName;		
	}
	
	[RPC] public void BasicSetupStationRPC(int objectID, int currentRP, string wpnName)
	{
		this.owner = PhotonView.Find(objectID).GetComponent<SpaceStation>().GetHoldingPlayer();
		Debug.Log("[RPC] Set up Weapon.");
		
		Debug.Log("RPC OWNER :: " + owner.name);
		
		this.transform.parent = owner.RigPoints[currentRP];
		//this.gameObject.transform.localScale = new Vector3(3,3,3);
		Vector3 offset = owner.RigPoints[currentRP].position - rigpoint.position;
		this.gameObject.transform.position += offset;
		
		this.weaponName = wpnName;		
	}
	
	
	public void UpdateScale()
	{
		photonView.RPC("UpdateScaleRPC", PhotonTargets.Others, transform.localScale);
	}
	
	[RPC] public void UpdateScaleRPC(Vector3 inScale)
	{
		transform.localScale = inScale;
	}
	
	[RPC] public void ChangeTargetRPC(int objectID)
	{
		Debug.Log("Changed Target.");
		Target = PhotonView.Find(objectID).transform;
	}
	
	[RPC] public void ChangeTargetNoViewRPC(string inName)
	{
		Target = GameObject.Find(inName).transform;
	}
	
	[RPC] public void RequestTarget()
	{
		if (photonView.isMine)
			TargetChangeFSM();		
	}
	
	[RPC] public virtual void ToggleFireRPC()
	{
		Debug.Log("[RPC] Toggled Fire.");
	}
	
	[RPC] public virtual void ToggleFireRPC(int inID)
	{
		Debug.Log("[RPC] Toggled Fire.");
	}
	
	[RPC] public virtual void ToggleFireRPC(bool firing)
	{
		Debug.Log("[RPC] Toggled Fire.");
	}
	
	[RPC] public virtual void ToggleFireRPC(int Projectiles, float Distance)
	{
		Debug.Log("[RPC] Toggled Fire.");
	}
	
	protected void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
//		if (stream.isWriting)			
//		{
//			// Make sure we can serialize a target.
//			bool serializeTarget = false;
//			
//			// It if can, target true and target are sent, if not only targettrue is sent.
//			if (Target != null)
//			{
//				serializeTarget = true;
//				stream.SendNext(serializeTarget);
//				stream.SendNext(Target);
//			}
//			else
//			{
//				stream.SendNext(serializeTarget);
//			}
//			stream.SendNext(control);
//		}		
//		else 
//		{		
//			// check if there is a target transform.
//			bool serializeTarget = false;
//			serializeTarget = (bool)stream.ReceiveNext();
//			
//			// if there is, accept it.
//			if (serializeTarget)
//				Target = (Transform)stream.ReceiveNext();
//				
//			control = (ControlMode)stream.ReceiveNext();
//		}
	}
	
	public WeaponAttributes GetAttributes()
	{
		return attributes;
	}	
	
	public void SetAttributes(WeaponAttributes inAttributes)
	{
		attributes = inAttributes;
	}
}

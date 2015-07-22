using UnityEngine;
using System.Collections;

public abstract class MissileBehavior : MonoBehaviour {

	// Weapon Infromation
	public	float 	damage;
	public 	float 	blastRadius;
	public	float	timing;
	public	float 	armingTime = 2.0f;
	public	float	speed;
	public 	float 	thrust;
	public	float 	degreesPerSecond;
	public	float 	rotationsPerMinute;
	public	float 	missileHealth;
	
	public	bool	armed;
	public 	bool	local;
	
	public PhotonView photonView;
	
	protected string missileName;
	protected float timeAlive = 0;
	
	public	GameObject 		ownerObject;
	public	Transform 		target;
	public	MissileType		mType;
	public	CurrentStage 	mStage;
	public	Ship			owner;	
	public 	Weapon.DamageBias	bias = Weapon.DamageBias.NONE;
	public	Weapon.WeaponType	type = Weapon.WeaponType.MISSILE;

	// Target Information
	protected Vector3		prevPosition = Vector3.zero;
	protected Vector3 		acceleration = Vector3.zero;
	protected Vector3 		velocity 	 = Vector3.zero;
	protected Vector3		prevVel		 = Vector3.zero;
	
	protected float 		distance;

	public enum MissileType {
		TracerMissile = 0,			// Trace your warhead.
		KineticMissile,			// Great for small targets, light fast and high capacity
		ShapedExplosiveMissile,	// Very good at piercing armor.
		EMPMissile,				// Disables enemy ships / systems.
		NuclearMissile,			// Both EMP and Nuclear in capabilities.
		AntimatterMissile,			// Extremely destructive warhead
		GravitonMissile, 			// Will suck targets towards them
		BlackholeMissile,			// Ultimate Warhead, massive in size, only the largest ships can use. Will suck up suns. Will sphagetifi things at event horizon.
		NULL
	};

	public static float[] reloadTimes = new float[] {0, 1, 3, 5, 9, 15, 25, 30, 360};
	
	public enum CurrentStage {
		Chasing = 0,
		Detonating
	};


	// Use this for initialization
	public virtual void Start () {

	}

	public void SetInfo(Transform inTarget, MissileType inType, Ship inOwner, bool inlocal, float inThrust, float inTimeAlive, float inBlastRadius)
	{
		owner = inOwner;
		target = inTarget;
		mType = inType;
		local = inlocal;
		thrust = inThrust;
		missileName = owner.name + mType.ToString();
		timeAlive = inTimeAlive;
		blastRadius = inBlastRadius;
		missileHealth = blastRadius/6;
	}	
	
	public void SetModifiers(Weapon.DamageBias inBias, float inDamageMultiplier, float inBlastRadiusMultiper, float inTurnSpeedMultiplier, float inVelocityMulitplier)
	{
		bias = inBias;
		damage *= inDamageMultiplier;
		blastRadius *= inBlastRadiusMultiper;
		rotationsPerMinute *= inTurnSpeedMultiplier;
		speed *= inVelocityMulitplier;
	}

	// Maybe add a no collide for 2 seconds to emulate 
	// Update is called once per frame
	public virtual void Update () {
		if (Game.gameState == Game.Gamestate.Paused)
			return;

		Debug.Log("Missile Target: " + target.gameObject.ToString());
		if (!armed)
			Arming();
		
		if (mStage == CurrentStage.Chasing)
		{
			Follow();
			
		}
		
		if (mStage == CurrentStage.Detonating)
		{
			// Do detonaty things here.	
		}
	}
	
	public virtual void OnCollisionEnter(Collision co)
	{
			
		if (!armed || co.collider.isTrigger)
			return;
		
		Detonate();
	}
	
	public virtual void TimeAliveCounter()
	{
		if (target == null)
			Abort();
			
		if ((timeAlive -= Time.deltaTime) <= 0)
			Abort();
	}
	public virtual void Follow() 
	{
		//transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(target.position - transform.position), turnspeed); 
		// Add * time.delta to slow down.
		//transform.position += transform.forward * speed * Time.deltaTime;	
	}	


	
	protected abstract void SendDamage();
	
	protected abstract void Detonate();
	
	public void sendDentonation()
	{
		Detonate();
	}

	public void SetTarget(Transform inTarget)
	{
		target = inTarget;	
	}
	
	public virtual void MissileUpdate()
	{
		
	}
	
	public void Arming()
	{
		timing += Time.deltaTime;		
		if (timing > armingTime)
			armed = true;
	}
	
	protected void CalculateTurningSpeed()
	{
		degreesPerSecond = (rotationsPerMinute * 360) / 60;
	}
	
	protected void Abort()
	{
		if (photonView.isMine)
			PhotonNetwork.Destroy(gameObject);
	}
	
	public void DamageRecieve(DamageMessage message)
	{
		rigidbody.AddForce(message.damageForce * message.damageDirection);
		missileHealth -= message.damage;
		
		if (missileHealth <= 0)
			Abort();
	}
	// Use command pattern for explosion settings
}

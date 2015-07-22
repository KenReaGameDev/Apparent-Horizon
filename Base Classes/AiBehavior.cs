using UnityEngine;
using System.Collections;
using System.Threading;

// TODO:
// http://www.dofactory.com/Patterns/PatternState.aspx#_self1
// Behavioral pattern US IT

// Fleet Leader will determine behavior of ships.
public abstract class AiBehavior {

	protected enum BehaviorCommand {
		WAIT = 0,
		STOP,
		WORK
	};
	
	protected float waitTimer = 0;
	protected float requestWaitTimer = 0;

	protected Ship ship;
	protected Ship leader;
	protected float regenTicks = 0;
	protected float regenWait = 1;
	protected float orbitRange = 10000;
	protected float distance = 0;
	protected float currentSpeed;
	protected float degreePerSecond;
	protected float deltaUpdateDistance = 0;
	protected float deltaOrbitUpdate = 1;

	protected BehaviorCommand bCommand = BehaviorCommand.WORK;
	protected Vector3 psuedoUpVector = Vector3.up;
	protected Vector3 orbitDirection;
	protected FleetManager fleetManager;
	protected int[] shuffleArray;
	
	public float timeInBehavior = 0;

	public abstract void DetermineTarget();
	public abstract void DetermineSpeed ();
	public abstract void RequestBehavior(int PlayerID);
	
	protected abstract void WaitUpdate();
	protected abstract void WorkUpdate();
	protected abstract void Update();
	public abstract bool KeepBehaviorCheck(AiShip leader, AiShip subject);

	public string debugString;

	public void SetShip(AiShip inShip)
	{
		this.ship = inShip;	
	}

	public void SetLeader(Ship inShip)
	{
		this.leader = inShip;	
	}
	
	public bool ShipTarget()
	{
		if (ship.GetTarget() == null)
			return false;
		
		return true;
	}	
	
	public bool LeadTarget()
	{
		if (leader.GetTarget() == null)
			return false;
		
		return true;
	}
	public void CallUpdate()
	{
		Update();
	}
	protected virtual void Regen()
	{

	}

	protected virtual void Switch()
	{
		timeInBehavior = 0;
	}

	public void CallSwitch()
	{
		Switch();
	}

	protected virtual void UpdateTime()
	{
		timeInBehavior += Time.deltaTime;
		requestWaitTimer += Time.deltaTime;
	}

	protected void DefaultBehaviorSwitch()
	{
		switch (bCommand)
		{
		case BehaviorCommand.WORK:
			WorkUpdate();
			break;
		case BehaviorCommand.WAIT:
			WaitUpdate();
			break;
		}
	}

	protected virtual void AlertToChange()
	{

	}

	protected void RandomPsuedoUpVector()
	{
		psuedoUpVector = new Vector3(Random.Range(0.0f,1.0f), Random.Range(0.0f,1.0f), Random.Range(0.0f, 1.0f));
		
		if (ship == null)
			Debug.LogError("AI has no Ship " + this.GetType().ToString());
		
		if (ship.photonView == null)
			Debug.LogError("Ship has no Photon View");
		
		if (psuedoUpVector == null)
			Debug.LogError("Ship has no PSUV");
			
		ship.photonView.RPC("UpVectorRPC", PhotonTargets.Others, psuedoUpVector);
	}

	protected Vector3 TargetDirectionNormalized(Transform inTarget)
	{
		return Vector3.Normalize(inTarget.position - ship.transform.position);
	}

	protected Vector3 TargetDirection(Transform inTarget)
	{
		return inTarget.position - ship.transform.position;
	}

	// Check to see if target is within range of location.
	protected bool WithinRange(float Range, Vector3 location, Vector3 targetLocation)
	{	
		float dis = Vector3.Distance(location, targetLocation);
		
		//Debug.Log("Distance!! " + dis);
		if (dis > Range)
			return false;
		
		return true;
	}
	
	public void SetUpVector(Vector3 inVector)
	{
		psuedoUpVector = inVector;
	}
	
	public virtual void SyncBeaconPosition(Vector3 inVector)
	{
		
	}
	
	protected double GetCurrentHealthPercentage()
	{
		double health = ((ship.GetAttributes().structureHealth + ship.GetAttributes().armorHealth + ship.GetAttributes().shieldHealth) * 100) / ship.maxHealth;
		return health;
	}
	/// <summary>
	/// Returns the string of the current class.
	/// </summary>
	/// <returns>The current class.</returns>
	public string GetCurrentClass()
	{
		return GetType().ToString();
	}

	public void RequestFleetShuffle()
	{
		if (fleetManager == null)
		{
			GameObject requiredObject = GameObject.FindGameObjectWithTag("required");
			fleetManager = requiredObject.GetComponent<FleetManager>();
		}
		
		
		shuffleArray = new int[fleetManager.GetFleetListCount()];
		
		for (int ndx = 0; ndx < shuffleArray.Length; ndx++)
			shuffleArray[ndx] = ndx;
		
		System.Random r = new System.Random();
		for (int i = shuffleArray.Length; i > 0; i--)
		{
			int j = r.Next(i);
			int k = shuffleArray[j];
			shuffleArray[j] = shuffleArray[i - 1];
			shuffleArray[i - 1]  = k;
		}
	}
	
	void ShuffleArray()
	{
		shuffleArray = new int[fleetManager.GetFleetListCount()];
		for (int ndx = 0; ndx < shuffleArray.Length; ndx++)
			shuffleArray[ndx] = ndx;
		
		System.Random r = new System.Random();
		for (int i = shuffleArray.Length; i > 0; i--)
		{
			int j = r.Next(i);
			int k = shuffleArray[j];
			shuffleArray[j] = shuffleArray[i - 1];
			shuffleArray[i - 1]  = k;
		}
	}
	
	
}

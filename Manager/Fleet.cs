using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Fleet {
	
	public FleetManager manager;
	
	public Ship fleetLeader = null;
	public List<AiShip> fleet = null;
	public List<PlayerShip> pFleet = null;
	public Vector3 position = Vector3.zero;
	public string faction = null;	
	public int threat;
	public float minimumSpeed;
	public float minimumAccel;
	public bool stealth = false;
	public bool players = false;
	public bool isEmpty = false;
	
	public void UpdateFleet()
	{
		if (fleet.Count == 0 && pFleet.Count == 0)
		{
			manager.GetFleetList().Remove(this);
			isEmpty = true;
		}
		
		if (fleetLeader == null)
		{
			pFleet.RemoveAll(item => item == null);
			ChangeLeader(pFleet[0]);
		}	
	}
	
	public Fleet(FleetManager inManager, Ship inShip)
	{
		manager = inManager;
		fleetLeader = inShip;
	}
	
	public void ChangeLeader(Ship inNewLeader)
	{
		if (CheckRemoveFleet())
			return;
		
		fleetLeader = inNewLeader;	
		foreach(AiShip ship in fleet)
		{
			ship.setFleetLeader(inNewLeader);	
			ship.behavior.SetLeader(inNewLeader);
		}
	}
	
	public void RemoveAI(AiShip inShip)
	{
		fleet.Remove(inShip);
		
		if (fleetLeader == inShip && fleet.Count > 0)
		{
			ChangeLeader(fleet[0]);
		}		
		
		CheckRemoveFleet();
	}
	
	bool CheckRemoveFleet()
	{
		Debug.LogWarning("Checking to remove fleet");
		if (fleet.Count == 0 && pFleet.Count == 0)
		{
			Debug.LogWarning("remove fleet");
			manager.GetFleetList().Remove(this);
			isEmpty = true;
			return true;
		} 
		
		return false;
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Buff{

	Ship owner;

	// (Buff Amount, Timer)
	public BuffInfo thrust = new BuffInfo(1,0, false);
	public BuffInfo mass = new BuffInfo(1,0, false);
	public BuffInfo shield = new BuffInfo(1,0, false);
	public BuffInfo armor = new BuffInfo(1,0, false);
	public BuffInfo structure = new BuffInfo(1,0, false);
	public BuffInfo regen = new BuffInfo(1,0, false);
	public BuffInfo explosionRadius = new BuffInfo(1,0, false);
	public BuffInfo damage = new BuffInfo(1,0, false);
	public BuffInfo rateOfFire = new BuffInfo(1,0, false);
	public BuffInfo abilitiesModifier = new BuffInfo(1,1, false);		// Modify the buffs time and power
	public BuffInfo disable = new BuffInfo(0,0, false);

	Dictionary<string, BuffInfo> buffs = new Dictionary<string, BuffInfo>();

	HashSet<BuffInfo> currentBuffs = new HashSet<BuffInfo>();

	float deltaUpdates = 0;
	// Use this for initialization
	public Buff (Ship inOwner) {

		owner = inOwner;

		buffs.Add("thrust", thrust);
		buffs.Add("mass", mass);
		buffs.Add("shield", shield);
		buffs.Add("armor", armor);
		buffs.Add("structure", structure);
		buffs.Add("regen", regen);
		buffs.Add("explosionRadius", explosionRadius);
		buffs.Add("damage", damage);
		buffs.Add("rateOfFire", rateOfFire);
		buffs.Add("disable", disable);

		// Explosion Radius * 3 for 120 seconds;
		//SetBuff("explosionRadius", 3, 120);
	}
	
	// Update is called once per frame
	public void Update () {

		if ((deltaUpdates += Time.deltaTime) < 1)
			return;

		foreach (BuffInfo buff in currentBuffs)
		{
			if (buff.infinite)
				continue;
				
			UpdateBuff(buff);
			if (buff.time <= 0)
				currentBuffs.Remove(buff);
		}	

		deltaUpdates = 0;
	}

	// Therefore this will be called. Then the variable outside can also be updated?
	void UpdateBuff(BuffInfo inBuff)
	{
		if ((inBuff.time -= deltaUpdates) <= 0)
		{
			if (!inBuff.prevInfinite)
			{
				inBuff.power = 1;
				inBuff.time = 0;
			}
			else
				inBuff.SwapInfinite();
		}
	}

	public void SetBuff(string inKey, float inPower, float inTime, bool inInfinite)
	{
	
		if (currentBuffs.Contains(buffs[inKey]))
		{
			BuffInfo currentBuff = buffs[inKey];
			
			// Holds Infinite Power And uses stronger power until time runs out.
			if (currentBuff.infinite && !inInfinite)
			{
				if (currentBuff.power < inPower)
				{
					currentBuff.SwapInfinite(inPower, inTime);
				}
				
				return;			
			}
				
			if (currentBuff.infinite && inInfinite)
			{
				if (currentBuff.power < inPower)
					currentBuff.power = inPower;
					
				return;
			}	
			
			buffs[inKey].power = inPower;
			buffs[inKey].time = inTime;
			buffs[inKey].infinite = inInfinite;
		}	
		else
		{
			buffs[inKey].power = inPower;
			buffs[inKey].time = inTime;
			buffs[inKey].infinite = inInfinite;
			
			currentBuffs.Add(buffs[inKey]);			
		}
	}

	public void SetOwner(Ship inShip)
	{
		owner = inShip;
	}
}

using UnityEngine;
using System.Collections;

public class Ability {


	protected float basePower;
	protected float baseTime;
	protected string[] buffNames;
	protected string abilityName;
	public bool infinite;
	public Sprite icon;

	public void ActivateAbility(Buff inBuff)
	{
		BuffInfo mod = inBuff.abilitiesModifier;
		for (int i = 0; i < buffNames.Length; ++i)
		{
			inBuff.SetBuff(buffNames[i], basePower, baseTime, infinite);
		}
	}


}

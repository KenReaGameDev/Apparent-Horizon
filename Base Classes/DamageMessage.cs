using UnityEngine;
using System.Collections;

public class DamageMessage {

	public float damage;
	public Vector3 damageDirection;
	public float damageForce;
	public Weapon.DamageBias bias;
	public Weapon.WeaponType type;
	public GameObject owner; 
	public int viewID;
	public bool hasShip;
	public bool player;

	public DamageMessage (float dmg, Vector3 dir, float dmgFrce, Weapon.DamageBias bs, Weapon.WeaponType tp, GameObject own)
	{
		damage = dmg;
		damageDirection = dir;
		damageForce = dmgFrce;
		bias = bs;
		type = tp;
		owner = own;
		try
		{
			viewID = own.GetComponent<PhotonView>().viewID;
		}
		catch
		{
			Debug.Log(owner.name  + " does not have a photon View ID");
		}
	}
	
	public DamageMessage (float dmg, Vector3 dir, float dmgFrce, Weapon.DamageBias bs, Weapon.WeaponType tp, Ship own)
	{
		damage = dmg;
		damageDirection = dir;
		damageForce = dmgFrce;
		bias = bs;
		type = tp;
		
		if (own != null)
			owner = own.gameObject;
		else
			return;
		
		hasShip = true;
		try
		{
			viewID = own.GetComponent<PhotonView>().viewID;
		}
		catch
		{
			Debug.Log(owner.name  + " does not have a photon View ID");
		}
		
		if (own is AiShip)
			player = false;
		else
			player = true;
	}

}

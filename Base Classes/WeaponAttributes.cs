using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.IO;


public class WeaponAttributes {
	
	// Weapon General Information
	public string name;
	public string type;
	public string size;
	public string path;
	public string bias;
	
	public int typeNum;
	public int sizeNum;
	public int biasNum;	
	
	public int cost;
	
	// Weapon Firing Information
	public float range;
	public float reloadSpeed;
	public float ammoCapacity;
	public float damage;
	public float damageRadius;
	public float damageFalloff;
	public float fireRate;
	
	// Projectile Information
	public float projectileThrust;
	public float projectileMass;
	public float projectileTurnRate;
	public float detonationRange;
	public float detonationForce;
	
	public bool locked;
	
	

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Factions {

	public enum FactionDensity{
		Low = 0,
		Medium,
		High
	};

	public string FactionName;
	public Dictionary<string, int> relations = new Dictionary<string, int>();

	public string[] otherFactionsName;
	public int[] otherFactionsRelation;
	public int maxShipsInFleet;		
	public int typeOfShips;
	public int index;
	public int numberOfShipsInSystem;
	public FactionDensity density;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	}

	void UpdateDensity()
	{

		if (numberOfShipsInSystem > 20)
		{
			density = FactionDensity.High;
			return;
		}
		else if (numberOfShipsInSystem > 10)
		{
			density = FactionDensity.Medium;
			return;
		}
		else
		{
			density = FactionDensity.Low;
			return;
		}

	}

	public void ShipDestroyed()
	{
		numberOfShipsInSystem--;
		UpdateDensity();
	}

	public void ShipCreated()
	{
		numberOfShipsInSystem++;
		UpdateDensity();
	}

	public FactionDensity GetDensity()
	{
		return density;
	}
}

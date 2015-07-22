using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class levelGenerator : MonoBehaviour {
	
	#region Variables
	Vector3 positionStar;
	Vector3 positionPlanet;
	
	float numberOfPiratesSpawnedTotal = 0;
	float numberOfPirates = 0;
	float numberOfPiratesAllowed = 0;
	
	float numberOfCargoSpawnedTotal = 0;
	float numberofCargo = 0;
	float numberofCargoAllowed = 10;
	
	float pirateTimer = 0;
	float cargoTimer = 0;
	
	static float score = 0;
	
	IEnumerator SpawnDelay() {        
        yield return new WaitForSeconds(2);       
	}
	#endregion
	
	
	// Use this for initialization
	void Start () {
		positionStar = Vector3.zero;
		positionPlanet = GameObject.Find("Earth").transform.position;	
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Game.gameState == Game.Gamestate.Paused)
			return;
		
		pirateTimer += Time.deltaTime;
		cargoTimer += Time.deltaTime;
		
		ScoreCheck();
		//CheckPirates();
		CheckCargo();
	}
	
	void SpawnPirates()
	{
		if (pirateTimer <= 1)
			return;
		
		try
		{
			var pirateSpawn = GameObject.Instantiate(GameObject.FindGameObjectWithTag("pirate"), new Vector3(RSPX(), RSPY(), RSPZ()), Quaternion.identity);
			++numberOfPiratesSpawnedTotal;
			pirateSpawn.name = "pirate_" + numberOfPiratesSpawnedTotal.ToString();
			Debug.Log("spawned a pirate via cloning");
		}
		catch
		{
			var pirateSpawn = GameObject.Instantiate(Resources.Load("Ships/Pirates/Default/Pirate"), new Vector3(RSPX(), RSPY(), RSPZ()), Quaternion.identity);
			++numberOfPiratesSpawnedTotal;
			pirateSpawn.name = "pirate_" + numberOfPiratesSpawnedTotal.ToString();
			Debug.Log("spawned a pirate via loading");
		}
		
		pirateTimer = 0;
	}
	
	#region Random Space Between Sun and Planet
	float RSPX()
	{
		return Random.Range(positionStar.x, positionPlanet.x);				
	}
	float RSPY()
	{
		return Random.Range(positionStar.y, positionPlanet.y);			
	}
	float RSPZ()
	{
		return Random.Range(positionStar.z, positionPlanet.z);		
	}
	#endregion
	

	void SpawnCargo()
	{
		if (cargoTimer <= 1)
			return;
		
		try
		{
			var cargoSpawn = GameObject.Instantiate(GameObject.FindGameObjectWithTag("cargo"), new Vector3(RSUN(), RSUN(), RSUN()), Quaternion.identity);
			++numberOfCargoSpawnedTotal;
			cargoSpawn.name = "cargo_" + numberOfCargoSpawnedTotal.ToString();
		}
		catch
		{
			var cargoSpawn = GameObject.Instantiate(Resources.Load("cargo"), new Vector3(RSUN(), RSUN(), RSUN()), Quaternion.identity);
			++numberOfCargoSpawnedTotal;
			cargoSpawn.name = "cargo_" + numberOfCargoSpawnedTotal.ToString();
		}	
		
		cargoTimer = 0;
	}
	
	#region Random Space Around the Sun
	
	// Returns a random value around the edges of the sun.
	float RSUN()
	{
		bool flag = randomBoolean();
		
		if (flag)
			return Random.Range(9500 , 10000);
		else
			return Random.Range(-9500, -10000);
		
	}

	#endregion
	
	bool randomBoolean()
	{
		return (Random.value > 0.5f);
	}
	
	void CheckPirates()
	{
		numberOfPirates = 0;
		GameObject[] plist = GameObject.FindGameObjectsWithTag("pirate");
		
		foreach (GameObject s in plist)
		{
			++numberOfPirates;
		}
		
		if (numberOfPirates < numberOfPiratesAllowed)
			SpawnPirates();
	}
	
	void CheckCargo()
	{
		numberofCargo = 0;
		GameObject[] clist = GameObject.FindGameObjectsWithTag("cargo");
		
		foreach (GameObject c in clist)
		{
			++numberofCargo;
		}
		
		if (numberofCargo < numberofCargoAllowed)
			SpawnCargo();
		
		SpawnDelay();
	}
		
	// Allows more pirates to be spawned depending on score.
	void ScoreCheck()
	{
		if (score < 400)
			numberOfPiratesAllowed = 2;
		else if (score > 400)
			numberOfPiratesAllowed = 3;
		else if (score > 800)
			numberOfPiratesAllowed = 4;		
		else if (score > 1500)
			numberOfPiratesAllowed = 5;		
		else if (score > 2800)
			numberOfPiratesAllowed = 6;		
		else if (score > 4000)
			numberOfPiratesAllowed = 7;		
		
	}
	
	public static void UpdateScore(float currentScore)
	{
		score += currentScore;
	}
	
	
}

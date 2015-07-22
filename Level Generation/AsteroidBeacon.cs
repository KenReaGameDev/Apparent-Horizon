using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AsteroidBeacon : MonoBehaviour {
	
	public	SphereCollider 	enableCollider;
	public 	bool 			playerSpawned = false;
	public 	bool 			aiSpawned = false;
	public	bool			spawned = false;
	public 	int 			asteroidsAtBeacon;
	public 	int 			ID;
	public	float			grouping;
			float 			distanceFromPlayer;
			float 			deltaTime;
			bool			enablingAsteroids;
			int				enabledCount = 0;
			int 			enabledMax = 0;
			Transform		self;
			AsteroidFields	asteroidFieldClass;
	
	
	public 	AsteroidFields.Grouping	groupType;
	
	public struct AsteroidInfo {	
		public Renderer renderer;
		public GameObject gameObject;
		public MeshCollider meshCol;
		public BoxCollider boxCol;	
	};
	
	public Dictionary<string, AsteroidInfo> asteroidList = new Dictionary<string, AsteroidInfo>();
	
	float listTimer = 0;
	public 	List<GameObject> nearShips = new List<GameObject>();
	
	public PhotonView photonView;
	void Start()
	{
		//name = "|AsteroidBeacon|";
		enableCollider = GetComponent<SphereCollider>();
		enableCollider.radius = 5000;

		deltaTime = 0;		
		spawned = false;
	}
	
	void Update()
	{
		if (Game.gameState == Game.Gamestate.Paused)
			return;
		
		
		if (Game.NeedCamera())
			return;
		
		if ((deltaTime += Time.deltaTime) > 1)
		{
			float distance = Vector3.Distance(Game.currentCam.transform.position, this.transform.position);
			if (distance < 65000 && !spawned)
				EnableAsteroids();
			else if ( distance > 65000 && spawned)
				DisableAsteroids();
				
			deltaTime = 0;
		}

		if(enablingAsteroids)
			StartCoroutine(EnableAsteroidsOverTime());
	}


	public void SetInfo(AsteroidFields.Grouping inGrouping, float groupDistance, int inAsteroidAmount, AsteroidFields afIn)
	{
		groupType = inGrouping;
		grouping = groupDistance;
		asteroidsAtBeacon = inAsteroidAmount;
		asteroidFieldClass = afIn;
	}
	
//	public void SetSelf(Transform inT)
//	{
//		self = inT;	
//	}

	public void CreateAsteroids() 
	{
	
		AsteroidInfo dictionaryInfo = new AsteroidInfo();
		// Spawns asteroid at a certain distance from the beacon as per grouping specs.
		Vector3 rndLocation = new Vector3(Random.Range(-grouping, grouping), Random.Range(-grouping, grouping), Random.Range(-grouping, grouping)) + transform.position;
		GameObject asteroid = GameObject.Instantiate(Resources.Load("Asteroids/asteroid"), rndLocation, Quaternion.identity) as GameObject;
		asteroid.transform.localScale = new Vector3(Random.Range(500, 3000), Random.Range(500, 3000), Random.Range(500, 3000));
		asteroid.rigidbody.isKinematic = false;		
		asteroid.name = "|Asteroid|" + ID.ToString() + "_0";
		asteroid.GetComponent<AsteroidBehavior>().beacon = this;
		
		dictionaryInfo.gameObject = asteroid;
		dictionaryInfo.meshCol = asteroid.GetComponent<MeshCollider>();
		dictionaryInfo.boxCol = asteroid.GetComponent<BoxCollider>();
		dictionaryInfo.renderer = asteroid.GetComponent<Renderer>();		
		asteroidList.Add(asteroid.name, dictionaryInfo);
		
		for (int asteroidsSpawned = 1; asteroidsSpawned < asteroidsAtBeacon; ++asteroidsSpawned)
		{
			rndLocation = new Vector3(Random.Range(-grouping, grouping), Random.Range(-grouping, grouping), Random.Range(-grouping, grouping)) + transform.position;
			asteroid = GameObject.Instantiate(asteroid, rndLocation, Quaternion.identity) as GameObject;
			asteroid.transform.localScale = new Vector3(Random.Range(500, 3000), Random.Range(500, 3000), Random.Range(500, 3000));
			asteroid.rigidbody.isKinematic = false;
			asteroid.name = "|Asteroid|" + ID.ToString() + "_" + asteroidsSpawned;
			asteroid.GetComponent<AsteroidBehavior>().beacon = this;
			
			dictionaryInfo = new AsteroidInfo();
			dictionaryInfo.gameObject = asteroid;
			dictionaryInfo.meshCol = asteroid.GetComponent<MeshCollider>();
			dictionaryInfo.boxCol = asteroid.GetComponent<BoxCollider>();
			dictionaryInfo.renderer = asteroid.GetComponent<Renderer>();		
			asteroidList.Add(asteroid.name, dictionaryInfo);
			// Put beacon in AsteroidFields list for Beacons.
		}

		DisableAsteroids();
	}
	
	public void SyncAsteroids(Vector3[] scales, Vector3[] positions, Vector3[] rotations)
	{

	}
	
	public IEnumerator SyncAsteroidsCo(Vector3[] scales, Vector3[] positions, Vector3[] rotations)
	{
		AsteroidInfo dictionaryInfo = new AsteroidInfo();
		asteroidsAtBeacon = positions.Length;
		
		for (int asteroidsSpawned = 0; asteroidsSpawned < asteroidsAtBeacon; ++asteroidsSpawned)
		{
			GameObject asteroid = GameObject.Instantiate(Resources.Load("Asteroids/asteroid"), positions[asteroidsSpawned], Quaternion.Euler(rotations[asteroidsSpawned])) as GameObject;
			asteroid.transform.localScale = scales[asteroidsSpawned];
			asteroid.rigidbody.isKinematic = false;
			asteroid.name = "|Asteroid|" + ID.ToString() + "_" + asteroidsSpawned;
			
			dictionaryInfo = new AsteroidInfo();
			dictionaryInfo.gameObject = asteroid;
			dictionaryInfo.meshCol = asteroid.GetComponent<MeshCollider>();
			dictionaryInfo.boxCol = asteroid.GetComponent<BoxCollider>();
			dictionaryInfo.renderer = asteroid.GetComponent<Renderer>();		
			asteroidList.Add(asteroid.name, dictionaryInfo);
			yield return null;
			// Put beacon in AsteroidFields list for Beacons.
		}
		
		DisableAsteroids();
	}
	
	void DisableAsteroid(AsteroidInfo inInfo)
	{

		inInfo.renderer.enabled = false;
		inInfo.boxCol.enabled = false;
		inInfo.meshCol.enabled = false;		
	}
	
	void EnableAsteroid(AsteroidInfo inInfo)
	{
		inInfo.renderer.enabled = true;
		inInfo.boxCol.enabled = true;
		inInfo.meshCol.enabled = true;	
	}
	
	public void DisableAsteroids()
	{
//		foreach(GameObject ast in asteroidList)
//		{
//			ast.SetActive(false);
//		}

		foreach (AsteroidInfo info in asteroidList.Values)
		{
			DisableAsteroid(info);
		}
		
		spawned = false;
		enablingAsteroids = false;
		enabledMax = 0;
	}
	
	public void EnableAsteroids()
	{
		enabledMax = asteroidList.Count;
//		foreach(GameObject ast in asteroidList)
//		{
//			ast.SetActive(true);
//		}
		enabledCount = 0;
		enablingAsteroids = true;
		spawned = true;
	}	

//	void VariedEnable()
//	{
//		if (enabledCount >= enabledMax)
//		{
//			enablingAsteroids = false;
//			enabledCount = 0;
//		}
//
//		asteroidList[enabledCount].SetActive(true);
//		enabledCount++;
//	}

	void OnTriggerEnter(Collider col)
	{
		if (col.gameObject.tag == "Ship")
		{
			nearShips.Add(col.gameObject);
		}
	}

	void OnTriggerExit(Collider col)
	{
		if (col.gameObject.tag == "Ship")
		{
			nearShips.Remove(col.gameObject);
		}
	}

	void MaintainBeacon()
	{
		if (asteroidList.Count < 1)
		{
			Destroy(this);
		}
	}

	IEnumerator EnableAsteroidsOverTime() {

		enablingAsteroids = false;
		
		foreach (AsteroidInfo info in asteroidList.Values)
		{
			EnableAsteroid(info);
			yield return null;
		}
		
//		for (int i = 0; i < enabledMax; i++) {
//			asteroidList[i].SetActive(true);
//			yield return null;
//		}
	}

//	public void DespawnAsteroids()
//	{
//		//Vector3 prevPos = self.transform.position;
//		//self.transform.position = new Vector3(9999999, 99999999, 999999999);
//		//self.transform.position = prevPos;
//		Debug.Log("Triggered Despawn");
//		despawned = true;
//		asteroidsAtBeacon = 0;
//		DestroyAsteroids();
//		asteroidFieldClass.StoreBeacon(this);
//		//AddDespawnCollider();
//	}
	
	/*
	void OnCollisionEnter(Collision other)
	{
		Debug.Log("In Enter Collider");
		if (other.gameObject.CompareTag("asteroid"))
			Destroy(other.gameObject);
	}
	
	void OnTriggerEnter(Collision other)
	{
		Debug.Log("In Enter Collider");
		++asteroidsAtBeacon;
		if (other.gameObject.CompareTag("asteroid"))
			Destroy(other.gameObject);
	}	
	
	void OnTriggerStay(Collision other)
	{
		Debug.Log("In Stay Collider");
		++asteroidsAtBeacon;
		if (other.gameObject.CompareTag("asteroid"))
			Destroy(other.gameObject);
	}	
	*/
}
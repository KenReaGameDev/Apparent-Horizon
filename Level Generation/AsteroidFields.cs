using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class AsteroidFields : MonoBehaviour {

	public enum GameState {
		Playing = 0,
		Saving,
		Finished
	};
	
	public enum Grouping { 
		Sparse = 0, 
		Loose, 
		Moderate,
		Tight, 
		Single 
	};
	
	static public float[] GroupingDistance  = new float[5] { 
		6000, 
		5000, 
		4000, 
		2000,
		50
	};
	
	public int[,] GroupingNumbers = new int[5,5] {
		{4,6,8,10,12},
		{12,16,20,24,28},
		{10,14,18,20,26},
		{8,10,12,14,16},
		{1,1,1,1,1}
	};

	public Game gameScript;
	public GameState gState;
	int asteroidAmount = 0;
	float deltaCheckTime = 0;

	List<AsteroidBeacon> asteroidBeacons = new List<AsteroidBeacon>();
	Dictionary<AsteroidBeacon, AsteroidGroupInfo> groupDictionary = new Dictionary<AsteroidBeacon, AsteroidGroupInfo>();
	
	public PhotonView photonView;
	
	// Use this for initialization
	void Start () 
	{
		gameScript = GameObject.FindWithTag("required").GetComponent<Game>();		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
		
		if (Game.gameState == Game.Gamestate.Paused || Game.gameState == Game.Gamestate.Loading)
			return;

		if (gState == GameState.Playing && deltaCheckTime > 5)
		{
			deltaCheckTime += Time.deltaTime;
			CheckBeacons();
			
		}
	}
	

	
	public Grouping RandomEnum<Grouping>()
	{ 
  		Grouping[] values = (Grouping[]) System.Enum.GetValues(typeof(Grouping));
  		return values[new System.Random().Next(0,values.Length)];
	}
	
	public Grouping SpecificEnum<Grouping>(int inEnum)
	{ 
  		Grouping[] values = (Grouping[]) System.Enum.GetValues(typeof(Grouping));
  		return values[inEnum];
	}
	
	public void CreateLevel(int inAmount) 
	{	 
		// TODO:  change back after debug.
		//inAmount = 1;
		// Need to better randomly generate positions
		//System.Security.Cryptography.RNGCryptoServiceProvider rnd = new System.Security.Cryptography.RNGCryptoServiceProvider();
		for (int i = 0; i < inAmount; ++i)
		{
			Grouping groupType = RandomEnum<Grouping>();
			int amount = GroupingNumbers[(int)groupType, Random.Range(0,4)];
			Vector3 location = new Vector3(Random.Range(-250000, 250000), Random.Range(-27000, 27000), Random.Range(-250000, 250000));

			GameObject beacon = GameObject.Instantiate(Resources.Load("Asteroids/asteroidBeacon"), location, Quaternion.identity) as GameObject;
			AsteroidBeacon bcn = beacon.GetComponent<AsteroidBeacon>();
			bcn.ID = i;
			bcn.name = "|AsteroidBeacon|" + i;
			bcn.SetInfo(groupType, GroupingDistance[(int)groupType], amount, this);
			bcn.CreateAsteroids();
			bcn.GetComponent<SphereCollider>().isTrigger = true;
			asteroidBeacons.Add(bcn);
		}
		
		NetworkManager.LoadingIncrement();
			
	}
	
	[RPC] public void SyncBeaconsNewPlayerRPC(int playerID)
	{
		if (PhotonNetwork.isMasterClient)
			StartCoroutine(SyncBeacons(playerID));
	}
	
	IEnumerator SyncBeacons(int PlayerID)
	{
		Debug.LogWarning("Sending Beacons");
		foreach (AsteroidBeacon bcn in asteroidBeacons)
		{
			int amount = bcn.asteroidList.Count;
			
			Vector3[] aPos = new Vector3[amount];
			Vector3[] aScl = new Vector3[amount];
			Vector3[] aRot = new Vector3[amount];
			
			int ndx = 0;
			
			foreach (AsteroidBeacon.AsteroidInfo ainfo in bcn.asteroidList.Values)
			{
				
				aPos[ndx] = ainfo.gameObject.transform.position;
				aRot[ndx] = ainfo.gameObject.transform.rotation.eulerAngles;
				aScl[ndx] = ainfo.gameObject.transform.localScale;
				ndx++;
			}
			
			photonView.RPC("BeaconCreateRPC", PhotonTargets.Others, (int)bcn.groupType, amount, bcn.ID, bcn.transform.position, aScl, aPos, aRot);
			
			yield return null;
		}		
	}
	
	[RPC] public void BeaconCreateRPC(int grpType, int inAmnt, int ndx, Vector3 initPos, Vector3[] scales, Vector3[] positions, Vector3[] rotations)
	{
		GameObject beacon = GameObject.Instantiate(Resources.Load("Asteroids/asteroidBeacon"), initPos, Quaternion.identity) as GameObject;
		
		Grouping groupType = (Grouping)grpType;
		int amount = inAmnt;
		
		AsteroidBeacon bcn = beacon.GetComponent<AsteroidBeacon>();
		bcn.ID = ndx;
		bcn.name = "|AsteroidBeacon|" + ndx;
		bcn.SetInfo(groupType, GroupingDistance[(int)groupType], amount, this);
		StartCoroutine(bcn.SyncAsteroidsCo(scales, positions, rotations));
		//bcn.SyncAsteroids(scales, positions, rotations);
		bcn.GetComponent<SphereCollider>().isTrigger = true;
		asteroidBeacons.Add(bcn);	
	}
	// Used for saving game.
//	void StoreAsteroids () 
//	{		
//		foreach (AsteroidBeacon beac in asteroidBeacons)
//		{
//				AsteroidGroupInfo storingInfo = new AsteroidGroupInfo();
//				storingInfo.amount = beac.asteroidsAtBeacon;
//				storingInfo.groupType = beac.groupType;
//				storingInfo.location = beac.transform.position;
//				groupsInMemory.Add(storingInfo);
//				beac.DespawnAsteroids();
//		}
//		
//		gState = GameState.Finished;
//		Debug.Log(groupsInMemory.Count);
//	}

	// Check to see if the beacons have any asteroids left.
	void CheckBeacons()
	{
		asteroidBeacons.RemoveAll(AsteroidBeacon => AsteroidBeacon == null);
	}

//	public void StoreBeacon(AsteroidBeacon bcn)
//	{
//		AsteroidGroupInfo storingInfo = new AsteroidGroupInfo();
//		storingInfo.amount = bcn.asteroidsAtBeacon;
//		storingInfo.groupType = bcn.groupType;
//		storingInfo.location = bcn.transform.position;
//		groupsInMemory.Add(storingInfo);
//		//asteroidBeacons.Remove(bcn);
//		//Destroy(bcn.gameObject);
//	}
	
	public void SetSaving()
	{
		gState = GameState.Saving;	
	}
	
	public void SetPlaying()
	{
		gState = GameState.Playing;	
	}
}

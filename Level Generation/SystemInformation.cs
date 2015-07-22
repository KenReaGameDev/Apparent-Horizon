using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

// Bug -- Somewhere along the line, it's not destroying the planets before creating new ones. 

public class SystemInformation : MonoBehaviour {
	
	// GUI
	bool changeSystemMenu = false;
	public Rect windowRect = new Rect(0,0,50,50);
	
	private	FleetManager fleetManager;
	public 	AsteroidFields asteroidSpawner;
	GameObject starCurrent;
	float 	starRadius = 0;
	int 	starMaterial = 0;
	bool  	isInhabited;
	string 	nickname;
	public Material skybox;

	struct PlanetInfo {	
		public 	GameObject	planet;
		public 	int 		material;
		public 	Vector3 	position;
		public 	Vector3 	scale;
		public 	string 		name;
		public 	bool 		hasMoon;
		public 	float 		moonScale;
		public	Vector3 	moonPosition;
		public	int			moonMaterial;
		
	}
	
	// Used to save entire systems for instant recall.
	struct SystemInfoChunk {
		public	List <PlanetInfo> 	systemPlanets;
		public	List <AsteroidGroupInfo> asteroidGroups; // Hold the groups instead of the entire class.
		public	AsteroidFields		asteroidSpawner;
		public	float 	starRadius;
		public 	int		starMaterial;
		public	bool	isInhabited;
		public	int[] 	seedArray;
		public	int 	ID;
		public	string	nickname;
		public FleetManager fleetManager;
	}	
	
	public int ID;	
	public int[] seedArray = new int[10];
	List <PlanetInfo> systemPlanets = new List<PlanetInfo>();
	List <SystemInfoChunk> systemsInMemory = new List<SystemInfoChunk>();
	public PhotonView photonView;
	// Use this for initialization
	void Start () {

	}
	
	public void ServerNewSystem()
	{
		NewSystem();
	}
	
	// Update is called once per frame
	void Update () {		
		if (Game.gameState == Game.Gamestate.Paused || Game.gameState == Game.Gamestate.Loading)
			return;
			
		if (Input.GetKeyDown(KeyCode.N))
			changeSystemMenu = !changeSystemMenu;	
		

		if(!fleetManager)
			CreateFleetManager();	

	}
	
	void CreateID()
	{
		for (int i = 0; i < seedArray.Length; i++)
		{
    		ID += seedArray[i] * System.Convert.ToInt32(System.Math.Pow(10, seedArray.Length-i-1));
		}
		ID = System.Math.Abs(ID);	
	}
	
	// Seed Array has randomly generated numbers for all types of system possibilites.
	void NewSystem () {
		Debug.Log("New System");
		seedArray = SystemSeed.RandomSeed();
		CreateID();
		Debug.Log("System SEED: " + ID);
		asteroidSpawner = this.gameObject.GetComponent("AsteroidFields") as AsteroidFields;
		
		CreateAsteroids();
		CreateSun();
		CreatePlanets();
		CreateFleetManager();
		CreateSkyBox();
		NetworkManager.LoadingIncrement();
	}
	
	void SaveSystem () {
		asteroidSpawner.SetSaving();
		StoreSystem();
	}
	
	void UnloadSystem () {
		//RemovePlanets(); Probably want to do this in store system.
		RemoveStar();	
	}
	
	void LoadSystem (int i) {
		//SaveSystem();
		
		if (i > 5)
		{
			// Load from XML file.
		}
		
		systemPlanets = systemsInMemory[i].systemPlanets;
		isInhabited = systemsInMemory[i].isInhabited;
		ID = systemsInMemory[i].ID;
		starRadius = systemsInMemory[i].starRadius;
		starMaterial = systemsInMemory[i].starMaterial;
		seedArray = systemsInMemory[i].seedArray;
		asteroidSpawner = this.gameObject.GetComponent("AsteroidFields") as AsteroidFields;
		//asteroidSpawner.groupsInMemory = 	systemsInMemory[i].asteroidGroups;
		asteroidSpawner.gState = AsteroidFields.GameState.Playing;
		LoadPlanets();
		LoadSun();
		systemsInMemory.RemoveAt(i);
	}
	
	// Stores the system and adds it to the List of Systems in memory.
	// Last 5 systems only.
	void StoreSystem () {
		
		for (int i = 0; i < systemPlanets.Count-1; ++i)
		{
			PlanetInfo tempP = systemPlanets[i];
			tempP.position = tempP.planet.transform.position;
			systemPlanets[i] = tempP;
		}
		
		SystemInfoChunk storeSystem	= new SystemInfoChunk();
		//storeSystem.asteroidGroups = asteroidSpawner.groupsInMemory;
		storeSystem.starRadius = starRadius;
		storeSystem.ID = ID;
		storeSystem.systemPlanets = systemPlanets;
		storeSystem.seedArray = seedArray;
		storeSystem.isInhabited = isInhabited;
		storeSystem.starMaterial = starMaterial;
		systemsInMemory.Add(storeSystem);
		
		RemovePlanets();
		
		// Removes oldest system if more than 5 present.
		// System settings in final version can potentially determine this.
		if (systemsInMemory.Count > 5)
		{
			// Store old system in XML file.
			// Check for system first, update system if already saved.
			systemsInMemory.RemoveAt(0);
		}
	}
	
	void RemovePlanets() {
		Debug.Log("Desotrying Planets");
		foreach (PlanetInfo p in systemPlanets)
		{
			Debug.Log(p.name + " is Destroyed");
			Destroy(p.planet);	
		}		
		systemPlanets = new List<PlanetInfo>();
	}
	
	void RemoveStar () {
		Destroy(starCurrent);
	}
	
	void CreateSun () {
		if (seedArray[0] > 3)
		{
			seedArray[0] = Random.Range(0,3);
		}
		
		GameObject star = PhotonNetwork.Instantiate(("Stars/StarX"), Vector3.zero, Quaternion.identity, 0) as GameObject;
		int scale = seedArray[0] + 1;
		Star starScript = star.GetComponent<Star>();
		starScript.SetStar(18000 * scale, seedArray[0]);
		star.name = "star";
		star.GetComponent<SphereCollider>().isTrigger = true;
		star.rigidbody.drag = 100000;
		starCurrent = star;

		
		photonView.RPC("TransferSystemInformation", PhotonTargets.OthersBuffered, ID, seedArray);
		photonView.RPC("CreateStarRPC", PhotonTargets.OthersBuffered, star.GetComponent<PhotonView>().viewID);
	}
	
	[RPC] public void TransferSystemInformation(int inID, int[] inSeedArray)
	{
		seedArray = inSeedArray;
		ID = inID;
	}
	
	[RPC] public void CreateStarRPC(int starID)
	{
		GameObject star = PhotonView.Find(starID).gameObject;
		int scale = seedArray[0] + 1;
		Star starScript = star.GetComponent<Star>();
		starScript.SetStar(18000 * scale, seedArray[0]);
		star.name = "star";
		star.GetComponent<SphereCollider>().isTrigger = true;
		star.rigidbody.drag = 100000;
		starCurrent = star;
	}
	
	[RPC] public void CreatePlanetRPC(int planetID, int moonID, int ptype, float inScale, int ndx)
	{
		GameObject planet = PhotonView.Find(planetID).gameObject;
		PlanetInfo planetInfoTemp = new PlanetInfo();
		int planetType = ptype;	
		
		planet.name = "Planet " + ndx.ToString();
		Material mat = (Material)Resources.Load("Planets/Materials/" + seedArray[planetType].ToString(), typeof(Material));
		planet.renderer.material = mat;
		planet.GetComponent<PlanetUpdate>().enabled = true;
		planet.transform.localScale *= inScale; // will modify this based on planet type.
		if (moonID > -1)
		{
			GameObject moon = PhotonView.Find(moonID).gameObject;
			// Set up moon.
			moon.name = planet.name + " Moon";
			Material moonmat = (Material)Resources.Load("Planets/Materials/" + seedArray[2].ToString(), typeof(Material)); // MAKE THIS RANDOM BASED ON MOON MATERIALS
			moon.renderer.material = moonmat;
			moon.GetComponent<PlanetUpdate>().enabled = true;
			float moonScale = 200; // MAKE THIS RANDOM
			moon.transform.localScale *= moonScale;
			moon.transform.parent = planet.transform;
			moon.transform.position += new Vector3(planet.transform.localScale.x * 2, planet.transform.localScale.x * 2,0.0f);	
			moon.AddComponent("rotatePlanet");
			planetInfoTemp.hasMoon = true;
			planetInfoTemp.moonScale = moonScale;
			planetInfoTemp.moonPosition = moon.transform.position;
			planetInfoTemp.moonMaterial = 3; // MAKE THIS RANDOM
		}
		
		// Add planet for saving.			
		planetInfoTemp.material = planetType;
		planetInfoTemp.name = planet.name;
		planetInfoTemp.position = planet.transform.position;
		planetInfoTemp.scale = planet.transform.localScale;
		planetInfoTemp.planet = planet;
		systemPlanets.Add(planetInfoTemp);
	}
	
	void CreatePlanets () {
		int planets = seedArray[1];
		
		for (int ndx = 0; ndx < planets; ++ndx)
		{
			PlanetInfo planetInfoTemp = new PlanetInfo();
			int planetType = Random.Range(0, 9);
			int hasMoon = SystemSeed.RandomSeed_Zero_to_OneHundred();
			
			GameObject planet = PhotonNetwork.Instantiate(("Planets/Planet"), PlanetPositionGenerator(), Quaternion.identity, 0) as GameObject;
			planet.name = "Planet " + ndx.ToString();
			Material mat = (Material)Resources.Load("Planets/Materials/" + seedArray[planetType].ToString(), typeof(Material));
			planet.renderer.material = mat;
			planet.GetComponent<PlanetUpdate>().enabled = true;
			float scaleUp = UnityEngine.Random.Range(1000,10000);
			planet.transform.localScale *= scaleUp; // will modify this based on planet type.
			
			int moonID = -1;
			// Checks if planet has a moon using Modulus.
			if (hasMoon % 2 != 0)
			{
				GameObject moon = PhotonNetwork.Instantiate(("Planets/Planet"), planet.transform.position, Quaternion.identity, 0) as GameObject;
				moon.name = planet.name + " Moon";
				Material moonmat = (Material)Resources.Load("Planets/Materials/" + seedArray[2].ToString(), typeof(Material)); // MAKE THIS RANDOM BASED ON MOON MATERIALS
				moon.renderer.material = moonmat;
				moon.GetComponent<PlanetUpdate>().enabled = true;
				float moonScale = 200; // MAKE THIS RANDOM
				moon.transform.localScale *= moonScale;
				moon.transform.parent = planet.transform;
				moon.transform.position += new Vector3(planet.transform.localScale.x * 2, planet.transform.localScale.x * 2,0.0f);	
				moon.AddComponent("rotatePlanet");
				planetInfoTemp.hasMoon = true;
				planetInfoTemp.moonScale = moonScale;
				planetInfoTemp.moonPosition = moon.transform.position;
				planetInfoTemp.moonMaterial = 3; // MAKE THIS RANDOM
				moonID = moon.GetComponent<PhotonView>().viewID;
			}
			
			// Add planet for saving.			
			planetInfoTemp.material = planetType;
			planetInfoTemp.name = planet.name;
			planetInfoTemp.position = planet.transform.position;
			planetInfoTemp.scale = planet.transform.localScale;
			planetInfoTemp.planet = planet;
			systemPlanets.Add(planetInfoTemp);
			
			photonView.RPC("CreatePlanetRPC", PhotonTargets.OthersBuffered, planet.GetComponent<PhotonView>().viewID, moonID, planetType, scaleUp, ndx);
		}
	}

	void CreateSkyBox()
	{
		List<Material> skyboxes = Resources.LoadAll<Material>("Skyboxes").ToList();
		skybox = skyboxes[Random.Range(0, skyboxes.Count)];
		foreach( PlayerShip ps in GetComponent<Game>().GetPlayerList())
		{
			try 
			{
				//Camera psCamera = ps.CameraPoints[ps.currentCameraPoint].GetComponent<Camera>();
				//psCamera.renderer.material = skybox;
				ps.GetCurrentCamera().GetComponent<Skybox>().material = skybox;//renderer.material = skybox;

			}
			catch
			{
				Debug.Log("Camera did not exist");
			}
		}
	}

	void LoadPlanets ()	{
		
		for (int i = 0; i < systemPlanets.Count-1; ++i)
		{
			GameObject planet = PhotonNetwork.Instantiate(("Planets/Planet"),systemPlanets[i].position, Quaternion.identity, 0) as GameObject;
			planet.name = systemPlanets[i].name;
			planet.renderer.material = (Material)Resources.Load("Planets/Materials/" + systemPlanets[i].material, typeof(Material));
			planet.transform.localScale = systemPlanets[i].scale;	
			PlanetInfo pTemp = systemPlanets[i];
			pTemp.planet = planet;
			systemPlanets[i] = pTemp;			
		}
	}
	
	void LoadSun () {
		GameObject star = PhotonNetwork.Instantiate(("Stars/star"), Vector3.zero, Quaternion.identity, 0) as GameObject;
		Material mat = (Material)Resources.Load("Stars/Materials/" + starMaterial, typeof(Material));
		// Scale is based on diameter so we have to mulitply by 2.
		star.transform.localScale *= starRadius * 2; 
		star.renderer.material = mat;
		star.name = "star";
		starCurrent = star;
	}
	
	void CreateAsteroids () {
		asteroidSpawner.SetPlaying();
		asteroidSpawner.CreateLevel(seedArray[3] * 50);
	}
	
	void CreateFleetManager () {
		Debug.Log("Creating new Fleet Manager");
		fleetManager = GameObject.FindGameObjectWithTag("required").GetComponent<FleetManager>();
		fleetManager.FleetManager_New(seedArray, ID);		
	}
	
	Vector3 PlanetPositionGenerator () {
		return new Vector3(PositionOutsideSun(), PositionOutsideSun(), Random.Range(-1000, 1000));
	}
			
	// Returns a random value not witin the sun.
	float PositionOutsideSun()
	{
		bool flag = randomBoolean();
		
		if (flag)
			return Random.Range(starRadius * 2 , 500000);
		else
			return Random.Range(-starRadius * 2, -500000);		
	}
	
	bool randomBoolean()
	{
		return (Random.value > 0.5f);
	}
	
	public float GetStarRadius()
	{
		return starRadius;
	}
	
	void OnGUI() {		
		windowRect = new Rect(windowRect.x, windowRect.y, 250, 50 + (systemsInMemory.Count * 30));
		//Debug.Log("onguisysteminfo");
		//new Rect(20,20,500, 50 + (systemsInMemory.Count * 30))
		if (changeSystemMenu)
			windowRect = GUI.Window(0, windowRect, WindowInfo, "My Window");
	}
	
	void WindowInfo(int windowID) {		
		int offsetY = 17;
		int index = 0;
		foreach (SystemInfoChunk sys in systemsInMemory)
		{
			if (GUI.Button (new Rect(1, offsetY, 250, 25), "System" + sys.ID))
			{	
				Debug.Log("Loading System" + sys.ID);
				SaveSystem();
				UnloadSystem();
				LoadSystem(index);
			}
				
			offsetY += 26;
			++index;
		}
		
		if (GUI.Button (new Rect(0, windowRect.height - 25, 250, 25), "New System"))
		{
			SaveSystem();
			UnloadSystem();
			NewSystem();			
		}
				
		GUI.DragWindow(new Rect(0,0,10000,10000));	
	}
	

}


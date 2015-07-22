using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpaceStation : MonoBehaviour {
	
	FleetManager flManager;
	Factions owner;
	public Camera cam;
	public Camera uiCam;
	public UIPanel uiPanel;
	public SpaceStationUI userInterface;
	
	bool swapped = false;
	
	Transform camPoint;
	Transform constructionArea;

	// For storing weapons that are bought but not used currently.
	public static List<GameObject> storedWeapons = new List<GameObject>();
	// Menu's	
	// Mode Selector

	private int modeIndex = -1;
	
	// Faction Selector

	private int currentIndex = -1;
	
	// Ship Selector
	private bool shipsLoaded = false;
	private int currentShipIndex = -1;
	private bool askSwitch = false;
	
	// Upgrade -- Slot Selector
	Transform[] rigPoints;
	private int currentRigPoint = -1;
	
	// Upgrade -- Weapon Size Selector
	private int currentSizeSelection = -1;
	private bool showSizeSelection = false;
	
	// Upgrade -- Weapon Type Selector
	private int currentTypeSelection = -1;
	private bool showTypeSelection = false;
	
	// Upgrade -- Weapon Selector
	private int currentWeaponSelection = -1;
	private bool showWeaponSelection = false;
	
	// Ui Popup Lists
	
	/// <summary>
	/// The main popup is for the main menu of the space station.
	/// </summary>
	UIPopupList mainPopup;
	
	/// <summary>
	/// Box One Usually consists of -- Weapon Rig Points / Factions.
	/// </summary>
	UIPopupList BoxOne;
	
	/// <summary>
	/// Box Two Usually consists of -- Weapon Sizes / Ships.
	/// </summary>
	UIPopupList BoxTwo;
	
	/// <summary>
	/// Box Three Usually consists of -- Weapon Types.
	/// </summary>
	UIPopupList BoxThree;
	
	/// <summary>
	/// Box Four Usually consists of -- Weapons.
	/// </summary>
	/// 
	UIPopupList BoxFour;
	/// <summary>
	/// Accept changes made.
	/// </summary>
	UIButton AcceptOne;
	bool acceptClicked = false;

	public enum StationState {
		UPGRADE = 0,
		SWITCH,
		WEAPONS,
		NONE,
		ENTERED
	};
	
	public StationState state = StationState.NONE;
	PlayerShip player;
	PlayerShip holdingPlayer;
	MouseOrbitImproved orbitView;
	public static int currentResources = 5000;
	
	bool enterCheck = false;
	// Use this for initialization
	void Start () {
	
		flManager = GameObject.Find("_Required").GetComponent<FleetManager>();
		
		Transform[] tmp = this.GetComponentsInChildren<Transform>();
		for (int i = 0; i < tmp.Length; i++)
		{
			if (tmp[i].tag == "CameraPoint")
			{
				camPoint = tmp[i];
				cam = camPoint.GetComponent<Camera>();
				orbitView = camPoint.GetComponent<MouseOrbitImproved>();
			}
			
			if (tmp[i].tag == "ConstructionPoint")
			{
				constructionArea = tmp[i];
				if (orbitView != null)
					orbitView.target = constructionArea;
			}
		}

		GameObject.FindGameObjectWithTag("required").GetComponent<Game>().AddSpaceStation(this);

		if (cam == null)
		{
			Debug.Log("NO CAMERA ON STATION:: " + name);
			return;
		}
		else
			cam.enabled = false;
		
		if (constructionArea == null)
		{
			Debug.Log("NO CONSTRUCTION AREA ON STATION:: " + name);
			return;
		}
	}

	
	// Update is called once per frame
	void Update () {
		switch (state)
		{
		case StationState.NONE:
			UpdateStateNone();
			break;
		case StationState.ENTERED:
		case StationState.SWITCH:
		case StationState.UPGRADE:
		case StationState.WEAPONS:
			UpdateStateEntered();
			break;
		}


	}
	
	void UpdateStateNone()
	{
		if (enterCheck && Input.GetKeyDown(KeyCode.E) )
		{
			SwitchToStation();
		}
	}
	
	void UpdateStateEntered()
	{
		//GUIUpdate();
		holdingPlayer.transform.position = constructionArea.position;
		holdingPlayer.transform.rotation = constructionArea.rotation;
		if (Input.GetKeyDown(KeyCode.E))
		{
			SwitchFromStation();
		}
		
		if (Input.GetMouseButton(1))
		{
			cam.transform.position = orbitView.transform.position;
			cam.transform.rotation = orbitView.transform.rotation;
			orbitView.moveEnabled = true;
		}
		else
		{
			orbitView.moveEnabled = false;
		}
	}
	
	void OnTriggerEnter(Collider col)
	{
		// Dont enter if material is Collision Mat
//		if (col.sharedMaterial != null && col.sharedMaterial.name == "CollisionMat")
//			return;
		
		if (col.transform.root.gameObject.tag == "player" && col.transform.root.GetComponent<PhotonView>().isMine)
		{
			enterCheck = true;
			player = col.gameObject.transform.root.GetComponent<PlayerShip>();
		}
	}
	
	void OnTriggerExit(Collider col)
	{
//		if (col.sharedMaterial != null && col.sharedMaterial.name == "CollisionMat")
//			return;
		
		if (col.transform.root.gameObject.tag == "player" && col.transform.root.GetComponent<PhotonView>().isMine)
		{
			enterCheck = false;
			player = null;
		}
	}
	
	void SwitchToStation()
	{
		if (flManager == null)
			flManager = GameObject.Find("_Required").GetComponent<FleetManager>();
			
		cam.enabled = true;
		// If there user interface is null - get it.
		if (userInterface == null)
			userInterface = Game.uiManager.GetComponentInChildren<SpaceStationUI>();

		//TODO: Enable Mouse Orbit here. 

		// Dont allow player ship to move.
		player.rigidbody.isKinematic = true;
		
		List<MeshCollider> meshes = new List<MeshCollider>(player.transform.root.GetComponentsInChildren<MeshCollider>());
		
		foreach (MeshCollider meshCol in meshes)
			meshCol.enabled = false;
			
		// Disables all player ship scripts
		player.DisableScripts();

		// Disables current player camera and sets position to construction area
		player.GetCurrentCamera().enabled = false;
		player.transform.position = constructionArea.position;
		//player.transform.root.parent = constructionArea.transform;
		player.transform.rotation = transform.rotation;

		// Activates the UI Camera and UI
		userInterface.EnableUI(this);
		userInterface.UICamera.gameObject.SetActive(true);
		HideAndClearUI();

		// Tells station is is entered.
		state = StationState.ENTERED;

		// Copys player data to station
		holdingPlayer = player;
		flManager.playerShips.Remove(player);
		foreach (Weapon wpn in holdingPlayer.Weapons)
		{
			wpn.Target = null;
		}
		// Populates Rig Points
		//PopulateUpgradePoints();

		// Enables Mouse Camera Movement.
		orbitView.enabled = true;
	}
	
	void SwitchFromStation()
	{
		List<MeshCollider> meshes = new List<MeshCollider>(holdingPlayer.transform.root.GetComponentsInChildren<MeshCollider>());
		
		foreach (MeshCollider meshCol in meshes)
			meshCol.enabled = true;
			
		// Allows players to reneter staiton.
		enterCheck = false;
		// No more mouse view
		orbitView.enabled = false;
		// Disable station cam.
		cam.enabled = false;
		
		// Disables station UI
		userInterface.DisableUI();
		// Enables player camera
		holdingPlayer.GetCurrentCamera().enabled = true;
		holdingPlayer.GetCurrentCamera().GetComponent<SU_CameraFollow>().enabled = true;
		// tells station we have left.
		state = StationState.NONE;
		// Enables player scripts.

		
		swapped = false;
		
		// Sets player to be able to move.
		flManager.playerShips.Add(holdingPlayer);
		holdingPlayer.rigidbody.isKinematic = false;
		holdingPlayer.transform.parent = null;
		
		if (swapped)
			holdingPlayer.EnableNewShipSCripts();
		else
			holdingPlayer.EnableScripts();
			
		enterCheck = false;
		player = null;
		flManager.CheckBadPlayers();
		// TODO: change scale back to 1 after.
	}

	public PlayerShip GetHoldingPlayer()
	{
		if (holdingPlayer != null)
			return holdingPlayer;
			
		return null;
	}
	

	public void OnClickMode()
	{
		string clicked = UIButton.current.name;
		
		switch(clicked)
		{
		case "Exit":
			SwitchFromStation();
			break;
		case "Ships":
			state = StationState.SWITCH;
			break;
		case "Upgrades":
			state = StationState.UPGRADE;
			break;
		case "Weapons":
			state = StationState.WEAPONS;
			break;				
		}
		
		SwitchModes();
	}
	
	void OnGUI()
	{		

	}
	
	void SwitchModes()
	{
		HideAndClearUI();
//		switch(state)
//		{
//		case StationState.SWITCH:
//			PopulateFactions();
//			break;
//		}
	}
	
	public void SwitchShips(Attributes inAttributes)
	{
		// Load Ship
		Attributes newship = inAttributes;
		//Debug.LogWarning("Ships/" + shipFaction + "/" + newship.type + "/" + newship.modelName);
		GameObject ship = PhotonNetwork.Instantiate("Ships/" + newship.faction + "/" + newship.type + "/" + newship.modelName ,holdingPlayer.transform.position, transform.rotation, 0) as GameObject;
		//ship.transform.parent = constructionArea;
		// Update Gameobject Information
		ship.name = holdingPlayer.name;
		ship.tag = "player";
		holdingPlayer.DepositResources();
		// Add Components.
		ship.AddComponent<PlayerShip>();
		ship.AddComponent<Rigidbody>();
		ship.rigidbody.isKinematic= true;

		// Create a PlayerShip holder to swap.
		PlayerShip swapShipPlayer = ship.GetComponent<PlayerShip>();
		swapShipPlayer.enabled = false;
		swapShipPlayer.SetMyFleet(holdingPlayer.GetMyFleet());
		

		// New Ship Class Update
		swapShipPlayer.SwapShip(newship, holdingPlayer);
		// Replace old Player Script with new.
		// This is now done when entering and leaving station.
//		int indexShip = flManager.playerShips.IndexOf(holdingPlayer);
//		if (indexShip != null)
//			flManager.playerShips[indexShip] = swapShipPlayer;
			
		// Destroy the class that the station holds.
		holdingPlayer.myFleet.pFleet.Add(swapShipPlayer);
		holdingPlayer.DestroySwap();
		
		// Replace old information.
		holdingPlayer = swapShipPlayer;
		player = swapShipPlayer;
		player.myFleet.UpdateFleet();
		
		// Syncs newer playership with game.
		string[] syncAttributes = new string[3];
		syncAttributes[0] = player.attributes.faction;
		syncAttributes[1] = player.attributes.type;
		syncAttributes[2] = player.attributes.modelName;		
		flManager.GetComponent<Game>().ScenePhotonView.RPC("SyncPlayerShipRPC", PhotonTargets.OthersBuffered, player.photonView.viewID, PhotonNetwork.player.name, syncAttributes);
		
		swapped = true;
		// Update station.
		SwitchToStation();
//		HideAndClearUI();
//		state = StationState.ENTERED;		
	}



//	void LoadShipBoxs()
//	{
//		Ships.AttributesPreLoad factionShips = Ships.shipDictionary[BoxOne.value];
//		
//		int count = 0;
//		BoxTwo.items.Clear();
//		foreach (var pair in factionShips.factionShipDictionary)
//		{
//			BoxTwo.items.Add(pair.Key);
//			count++;
//		}
//	}

	public void DeathSwap(Ship inShip)
	{
		holdingPlayer = (PlayerShip)inShip;
		player =(PlayerShip) inShip;
		SwitchToStation();
		HideAndClearUI();
		state = StationState.SWITCH;
		//PopulateFactions();
		//LoadDefaultShip();
		//SwitchShips();

	}

//	void LoadDefaultShip()
//	{
//		// Switch to player default.
//		BoxOne.gameObject.SetActive(true);
//		BoxOne.value = BoxOne.items[0];
//		BoxOne.currentIndex = 0;
//		LoadShipBoxs();
//		BoxTwo.gameObject.SetActive(true);
//		BoxTwo.value = BoxTwo.items[0];
//		BoxTwo.currentIndex = 0;
//	}

	#region UI Population / Modification
//	bool PopulateUpgradePoints()
//	{
//		if (holdingPlayer.RigPoints.Count < 1)
//			return false;
//		
//		BoxOne.items.Clear();
//		
//		int count = 0;
//		foreach (Transform tf in holdingPlayer.RigPoints)
//		{
//			string gunName = "Slot Empty";
//			
//			if (holdingPlayer.RigPoints[count].childCount > 0)
//				gunName = holdingPlayer.RigPoints[count].GetComponentInChildren<Weapon>().name;
//			
//			BoxOne.items.Add(gunName);
//			count++;
//		}
//		
//		BoxOne.value = BoxOne.items[0];
//		BoxOne.currentIndex = 0;
//		return true;
//	}
//	
//	bool PopulateSizes()
//	{
//		
//		if (BoxOne.currentIndex < 0 || BoxOne.currentIndex >= holdingPlayer.attributes.rigPointSize.Length)
//			return false;
//		
//		// Holds all the possible Sizes for this rigpoint.
//		string possibleSizes = holdingPlayer.attributes.rigPointSize[BoxOne.currentIndex];
//		string[] sizeArray = possibleSizes.Split('|');
//		int sizesPossible = sizeArray.Length;
//		
//		if (sizesPossible < 1)
//			return false;
//		
//		BoxTwo.items.Clear();
//		for (int i = 0; i < sizesPossible; i++)
//		{
//			BoxTwo.items.Add(sizeArray[i]);
//		}
//		
//		BoxTwo.value = BoxTwo.items[0];
//		BoxTwo.currentIndex = 0;
//		return true;
//	}
//	
//	bool PopulateTypes()
//	{
//		if (BoxOne.currentIndex < 0)
//			return false;
//		
//		// Holds all the possible types for this rig point
//		string possibleTypes = holdingPlayer.attributes.rigPointType[BoxOne.currentIndex];
//		string[] typeArray = possibleTypes.Split('|');
//		int typesPossible = typeArray.Length;
//		
//		if (typesPossible < 1)
//			return false;
//		
//		BoxThree.items.Clear();
//		for (int i = 0; i < typesPossible; i++)
//		{
//			BoxThree.items.Add(typeArray[i]);
//		}
//		
//		BoxThree.value = BoxThree.items[0];
//		BoxThree.currentIndex = 0;
//		
//		return true;
//	}
//	
//	bool PopulateWeapons()
//	{
//		Weapon.WeaponSize sze = (Weapon.WeaponSize) System.Enum.Parse(typeof(Weapon.WeaponSize), BoxTwo.value);
//		Weapon.WeaponType tpe = (Weapon.WeaponType) System.Enum.Parse(typeof(Weapon.WeaponType), BoxThree.value);
//		
//		Weapon.WeaponXML holder = Game.weaponDictionary[sze];
//		
//		if (holder.weapons.Count < 1)
//			return false;
//		
//		
//		string[] weaponsPossible = new string[holder.weapons.Count];
//		int count = 0;
//		
//		foreach (var pair in holder.weapons)
//		{
//			if (pair.Key == tpe)
//			{
//				weaponsPossible[count] = pair.Value;
//				count++;
//			}
//		}
//		
//		
//		
//		BoxFour = userInterface.popups["Box4"];
//		BoxFour.items.Clear();
//		// Loop weapons
//		for (int i = 0; i < count; i++)
//		{
//			
//			BoxFour.items.Add(weaponsPossible[i]);
//			
//		}
//		
//		BoxFour.value = BoxFour.items[0];
//		BoxFour.currentIndex = 0;
//		return true;
//	}
	
//	void ChangeWeapons()
//	{
//		acceptClicked = false;
//		Transform rp = holdingPlayer.RigPoints[currentRigPoint];
//		
//		// Check if there is already a weapon in this slot.
//		foreach (Weapon wpn in holdingPlayer.Weapons)
//		{
//			if (wpn.rigpoint == rp)
//			{
//				Transform tfd = rp.GetChild(0);
//				tfd.SendMessage("SwapOut");
//				PhotonNetwork.Destroy(tfd.gameObject);
//			}
//		}
//		
//		
//		string loadAddress = "Weapons" + "/" + BoxTwo.value + "/" + BoxThree.value + "/" +  BoxFour.value;
//		string[] splitName = BoxFour.value.Split('/');
//
//		if (loadAddress == null)
//			return;
//		
//		GameObject tur = PhotonNetwork.Instantiate(loadAddress, rp.position, rp.rotation, 0) as GameObject;		
//		tur.transform.parent = rp;
//		
//		// Set up weapon.
//		Weapon wscript = tur.GetComponent<Weapon>();		
//		wscript.rigpoint = rp;
//		wscript.owner = holdingPlayer;
//		wscript.weaponName = splitName[1];
//		
//		wscript.photonView.RPC("BasicSetupRPC", PhotonTargets.AllBuffered, holdingPlayer.photonView.viewID, currentRigPoint, splitName[1]);
//		
//		holdingPlayer.Weapons.Add(wscript);
//		tur.transform.parent = rp;
//		//tur.transform.position = wscript.owner.transform.position;
//		tur.transform.localScale = new Vector3(3,3,3);
//		// rPoint or rp figure it out!
//		Vector3 offset = rp.position - wscript.rigpoint.position;
//		offset.x *= tur.transform.localScale.x;
//		offset.y *= tur.transform.localScale.y;
//		offset.z *= tur.transform.localScale.z;
//		tur.transform.position += offset;
//		tur.transform.parent = rp;
//		tur.name = splitName[1];
//		PopulateUpgradePoints();
//	}
	
//	void PopulateFactions()
//	{
//		BoxOne.items.Clear();
//		foreach (var preloads in Ships.shipDictionary)
//		{
//			BoxOne.items.Add(preloads.Key);
//		}
//	}

//	public void PopulateUI(UIButton inButton)
//	{
//		mainPopup = userInterface.popups["Box0"];
//		mainPopup.items.Clear();
//		mainPopup.items.Add("Weapons");
//		mainPopup.items.Add("Ships");
//		mainPopup.items.Add("Research");	
//		
//		BoxFour = userInterface.popups["Box4"];
//		BoxThree = userInterface.popups["Box3"];
//		BoxTwo = userInterface.popups["Box2"];
//		BoxOne= userInterface.popups["Box1"];
//		AcceptOne = inButton;
//
//		userInterface.UICamera.gameObject.SetActive(true);
//	}


	void HideAndClearUI()
	{


//		BoxFour.gameObject.SetActive(true);
//		BoxTwo.gameObject.SetActive(true);
//		BoxOne.gameObject.SetActive(true);
//		BoxThree.gameObject.SetActive(true);
//		AcceptOne.gameObject.SetActive(true);
//
//		BoxFour.currentIndex = -1;
//		BoxTwo.currentIndex = -1;
//		BoxOne.currentIndex = -1;
//		BoxThree.currentIndex = -1;
//		
//		BoxFour.value = "";
//		BoxTwo.value = "";
//		BoxOne.value = "";
//		BoxThree.value = "";
//		
//		BoxFour.items.Clear();
//		BoxTwo.items.Clear();
//		BoxOne.items.Clear();
//		BoxThree.items.Clear();
//
//
//		BoxFour.gameObject.SetActive(false);
//		BoxTwo.gameObject.SetActive(false);
//		BoxOne.gameObject.SetActive(false);
//		BoxThree.gameObject.SetActive(false);
//		AcceptOne.gameObject.SetActive(false);
		
	}

	public void OnClickAccept()
	{
		acceptClicked = true;
	}
	
	public void OrbitViewToggle(bool tog)
	{
		orbitView.enabled = tog;
	}
	#endregion
}

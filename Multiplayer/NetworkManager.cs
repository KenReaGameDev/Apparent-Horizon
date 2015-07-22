using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkManager : Photon.MonoBehaviour {

	private PhotonView myPhotonView;
	private string 	version;
	private string 	roomName;
	private string 	password;
	private bool 	inGame = false;
	private bool 	inLobby = false;
	private bool 	online = false;
	private Game	game;
	private RoomInfo[] roomAry;
	
	public static string playerName;
	public Camera menuCam;
	
	public static bool hostLoading = true;
	
	// Loading Information
	public static bool[] loadingFinished = new bool[4];
	public static int loadingIndex = 0;
	
	// UI
	public GameObject customServerUI;
	public GameObject mainMenuUI;
	
	public GameObject customServerGrid;
	public GameObject customServerPrefab;
	
	public GameObject onlineToggleButton;
	public GameObject offlineToggleButton;
	
	public PhotonStatsGui networkInformation;
	public UILabel pingLabel;
	public UILabel playersConnectedLabel;
	public UILabel playersInGameLabel;
	
	public List<CustomServerInfo> servers = new List<CustomServerInfo>();
	public CustomServerInfo currentCustomSelection;
	CustomServerInfo previousServer;

	public bool isOpen;
	public bool isLocked;
	public bool isVisible;
	
	
	// Use this for initialization
	void Start () {
	
		menuCam.enabled = false;
		menuCam.enabled = true;
		
		game = GetComponent<Game>();
		hostLoading = true;
		loadingIndex = 0;
		loadingFinished = new bool[4];
		
		version = "a1.30";
		roomName = "alpha1.31_room";
		
		StartOnline();
		StartMenus();
				
		for (int i = 0; i < loadingFinished.Length; i++)
		{
			loadingFinished[i] = false;
		}	
	}
	
	void StartMenus()
	{
		mainMenuUI.SetActive(true);
		customServerUI.SetActive(false);
		onlineToggleButton.SetActive(true);
		offlineToggleButton.SetActive(false);
		networkInformation.enabled = true;
	}
	
	void StartOnline()
	{
		online = PhotonNetwork.ConnectUsingSettings(version);
		
		if (online)
		{
			PhotonNetwork.sendRate = 16;
			PhotonNetwork.sendRateOnSerialize = 16;
			PhotonNetwork.MaxResendsBeforeDisconnect = 15;
		}		

	}
	
	void CheckOnline()
	{
		if (!PhotonNetwork.connectedAndReady)
		{
			Debug.Log("Not Online");
			// Switch to offline
			online = false;
			offlineToggleButton.SetActive(true);
			onlineToggleButton.SetActive(false);
			PhotonNetwork.Disconnect();
			PhotonNetwork.offlineMode = true;
			networkInformation.enabled = false;
			pingLabel.text = "Offline";
			playersInGameLabel.text = "Offline";
			playersConnectedLabel.text = "Offline";
			PhotonNetwork.MaxResendsBeforeDisconnect = 0;
			return;
		}
		else
		{
			onlineToggleButton.SetActive(true);
			offlineToggleButton.SetActive(false);
			networkInformation.enabled = true;
		}
	}
	
	void PopulateRoomList()
	{
		foreach (CustomServerInfo cs in servers)
			Destroy(cs.gameObject);
			
		servers.Clear();
		mainMenuUI.SetActive(false);
		customServerUI.SetActive(true);
		
		roomAry = PhotonNetwork.GetRoomList();
		Debug.Log(PhotonNetwork.GetRoomList().Length);
		Debug.Log(roomAry.Length);
		
		for (int i = 0; i < roomAry.Length; i++)
		{
			GameObject go = GameObject.Instantiate(customServerPrefab) as GameObject;
			go.SetActive(true);
			CustomServerInfo info = go.GetComponent<CustomServerInfo>();
			info.SetRoomInfo(roomAry[i], this);
			
			//go.name = info.name;
			go.transform.parent = customServerGrid.transform;
			go.transform.localScale = new Vector3(1,1,1);
			go.transform.localPosition = new Vector3(go.transform.localPosition.x, go.transform.localPosition.y, 0);
		}
		//GameObject.Destroy(customServerGrid.transform.GetChild(0).gameObject);
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;
	}
	
	public void SortByHostName()
	{
		foreach (CustomServerInfo csi in servers)
		{
			csi.gameObject.name = csi.hostName.text;
		}
		
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;
	}
	
	public void SortByServerName()
	{
		foreach (CustomServerInfo csi in servers)
		{
			csi.gameObject.name = csi.serverName.text;
		}	
		
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;	
	}
	
	public void SortByServerPlaying()
	{
		foreach (CustomServerInfo csi in servers)
		{
			csi.gameObject.name = csi.playing.text + csi.serverName.text;
		}	
		
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;	
	}
	
	public void SortByLocked()
	{
		foreach (CustomServerInfo csi in servers)
		{
			csi.gameObject.name = csi.locked.text + csi.serverName.text;
		}	
		
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;
		
	}
	
	public void SortByPlayers()
	{
		foreach (CustomServerInfo csi in servers)
		{
			csi.gameObject.name = csi.currentPlayers.text + csi.serverName.text;
		}
		
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;
	}
	
	public void SortByRound()
	{
		foreach (CustomServerInfo csi in servers)
		{
			csi.gameObject.name = csi.round.text + csi.serverName.text;
		}
		
		customServerGrid.GetComponent<UIGrid>().mReposition = true;
		customServerGrid.GetComponent<UIGrid>().sorted = true;
		customServerGrid.GetComponent<UIGrid>().enabled = true;
	}
	
	// Update is called once per frame
	void Update () {
	
		if (Input.GetKeyDown(KeyCode.Home))
		{
			PhotonNetwork.Disconnect();
			Debug.Log("Disconnected Using Key");	
		}
		
		if (!inGame && inLobby && online)
		{
			playersConnectedLabel.text = PhotonNetwork.countOfPlayersOnMaster.ToString();
			playersInGameLabel.text = PhotonNetwork.countOfPlayersInRooms.ToString();
			pingLabel.text = PhotonNetwork.GetPing().ToString();
		}
	}
	
	void OnPhotonRandomJoinFailed()
	{
		// custom room properies.
		ExitGames.Client.Photon.Hashtable roomInfo = new ExitGames.Client.Photon.Hashtable();
		CreateRoom();
	}
	
	void OnJoinedLobby()
	{
		Debug.Log("Joined Lobby");
		inLobby = true;
		game.menuSystem.mainMenu.SetActive(true);
		
	}
	
	void OnJoinedRoom()
	{	
		game.menuSystem.mainMenu.SetActive(false);
		
		if (PhotonNetwork.playerList.Length == 1)
		{			
			menuCam.gameObject.SetActive(false);
			inGame = true;
			Game.playerWhoIsIt = PhotonNetwork.player.ID;
			GetComponent<Game>().enabled = true;
			GetComponent<Game>().SetupRoom();
			StartCoroutine(CheckLoadingFlags());
		}	
		else
		{
			menuCam.enabled = false;
			inGame = true;
			Game.playerWhoIsIt = PhotonNetwork.player.ID;
						
			StartCoroutine(WaitForHostLoad());			
		}	
	}
	
	void JoinSpecificRoom()
	{
		if (PhotonNetwork.player.name == "")
		{
			float defaultRandom = UnityEngine.Random.Range(0, 9999999);
			PhotonNetwork.player.name = "Default" + defaultRandom.ToString();
		}
			
		PhotonNetwork.JoinRoom(roomName, true);		
	}
	
	public void OnClickJoinRoom()
	{
		if (PhotonNetwork.player.name == "")
		{
			float defaultRandom = UnityEngine.Random.Range(0, 9999999);
			PhotonNetwork.player.name = "Default" + defaultRandom.ToString();
		}
			
		PhotonNetwork.JoinRoom(currentCustomSelection.serverName.text);
	}
	
	/// <summary>
	/// Creates a room using Server name, adds player name to custom information.
	/// </summary>
	public void CreateRoom()
	{
		if (PhotonNetwork.player.name == "")
		{
			float defaultRandom = UnityEngine.Random.Range(0, 9999999);
			PhotonNetwork.player.name = "Default" + defaultRandom.ToString();
		}
			
		// Hashtable is ExitGames Hashtable
		Hashtable customProperties = new Hashtable();
		customProperties["p"] = PhotonNetwork.player.name; // Player Name
		customProperties["r"] = "0";	// Round Number
		customProperties["l"] = isLocked; // Password Protected
		customProperties["pw"] = password; // Current Password
		
		string[] lobbyProperties = new string[customProperties.Count];
		int ndx = 0;
		foreach (string key in customProperties.Keys)
		{
			lobbyProperties[ndx] = key;
			ndx++;
		}		
		
		RoomOptions options = new RoomOptions();		
		options.maxPlayers = 4;
		options.isOpen = isOpen;
		options.isVisible = isVisible;
		options.customRoomProperties = customProperties;
		options.customRoomPropertiesForLobby = lobbyProperties; 		
		
		Debug.LogWarning(options.customRoomProperties.Count);
		PhotonNetwork.CreateRoom(roomName, options, null);		
	}
	
	void OnDisconnectedFromPhoton()
	{	
		if (!online)
			return;
			
		Debug.Log("Disconnected.");
		Application.LoadLevel("MainGame");
		Game.CleanGame();		
	}
	
	void OnLeftLobby()
	{
		if (!online)
			return;
			
		Debug.Log("Disconnected.");
		Application.LoadLevel("MainGame");
		Game.CleanGame();
	}
	
	void OnLeftRoom()
	{
		if (!online)
			return;
			
		Debug.Log("Disconnected.");
		Application.LoadLevel("MainGame");
		Game.CleanGame();
	}
	
	IEnumerator WaitForHostLoad()
	{
		while (hostLoading)
		{
			photonView.RPC("RequestLoadingStatus", PhotonTargets.MasterClient, null);
			yield return null;
		}
		
		SetupNonHost();
	}
	
	// Changes next loading flag to true.
	public static void LoadingIncrement()
	{
		loadingFinished[loadingIndex] = true;
		loadingIndex++;
	}
	
	// Checks to see if all is loaded. If not checks again 3 seconds later.
	IEnumerator CheckLoadingFlags()
	{
		int loaded = 0;
		while (hostLoading)
		{
			loaded = 0;
			for (int i = 0; i < loadingFinished.Length; i++)
			{
				if (loadingFinished[i])
					loaded++;
			}
			
			// If loaded is the same count as the length of the flags needed to be considered loaded, then loading is finished.
			if (loaded == loadingFinished.Length)
			{
				hostLoading = false;
				Game.gameState = Game.Gamestate.Playing;
			}
				
			yield return new WaitForSeconds(3);
			
		}
	}
	
	void SetupNonHost()
	{
		menuCam.enabled = false;
		inGame = true;
		Game.playerWhoIsIt = PhotonNetwork.player.ID;
		GetComponent<Game>().enabled = true;
		GetComponent<Game>().JoinRoom();
	}
	
	public static void QuitGame()
	{
		//GetComponent<NetworkManager>().enabled = false;
		PhotonNetwork.Disconnect();
	}
	
	public void SelectCustomServer(CustomServerInfo info)
	{
		if (previousServer)
			previousServer.highlight.enabled = false;
			
		previousServer = info;
		currentCustomSelection.hostName.text = info.hostName.text;
		currentCustomSelection.ping.text = info.ping.text;
		currentCustomSelection.currentPlayers.text = info.currentPlayers.text;
		currentCustomSelection.serverName.text = info.serverName.text;
		currentCustomSelection.maxPlayers.text = info.maxPlayers.text;
		currentCustomSelection.locked.text = info.locked.text;
		//currentCustomSelection.round.text = info.round.text;
	}
	
	[RPC] public void RequestLoadingStatus()
	{
		if (PhotonNetwork.player.isMasterClient && !hostLoading)
			photonView.RPC("ReturnLoadingStatus", PhotonTargets.Others, false);
	}
	
	[RPC] public void ReturnLoadingStatus(bool status)
	{
		hostLoading = status;
	}
	
	
	void OnGUI()
	{
		
		if (inGame && hostLoading)
		{
			GUI.Label(new Rect(Screen.width - 250, 00, 250, 20), "Waiting For Host to finish loading level.");
			GUI.Label(new Rect(20, 00, 150, 20), "loading steps complete; " + loadingIndex);
		}
	}
	
	public void OnClickCustomMatch()
	{
		//JoinSpecificRoom();
		PopulateRoomList();
	}
	
	public void OnClickQuickMatch()
	{
		if (PhotonNetwork.player.name == "")
		{
			float defaultRandom = UnityEngine.Random.Range(0, 9999999);
			PhotonNetwork.player.name = "Default" + defaultRandom.ToString();
		}
		
		PhotonNetwork.JoinRandomRoom(null, 4, MatchmakingMode.RandomMatching , TypedLobby.Default, null);
	}
	
	public void OnClickCustomBack()
	{
		mainMenuUI.SetActive(true);
		customServerUI.SetActive(false);
	}
	
	public void OnClickCreateGame()
	{
		JoinSpecificRoom();
	}
	
	public void OnSubmitUserName()
	{
		PhotonNetwork.player.name = UIInput.current.label.text;
		Debug.Log(PhotonNetwork.player.name);
	}
	
	public void OnSubmitServerName()
	{
		roomName = UIInput.current.label.text;
	}
	
	public void OnSubmitPassword()
	{
		password = UIInput.current.label.text;
	} 
	
	public void OnClickOnlineToggle()
	{	
		
		string buttonName = UIButton.current.name;
		buttonName = buttonName.ToLower();
		
		// Online switches to Offline else Offline Switches to Online
		if (buttonName == "online")
		{
			// Switch to offline
			online = false;
			offlineToggleButton.SetActive(true);
			onlineToggleButton.SetActive(false);
			PhotonNetwork.Disconnect();
			PhotonNetwork.offlineMode = true;
			networkInformation.enabled = false;
			pingLabel.text = "Offline";
			playersInGameLabel.text = "Offline";
			playersConnectedLabel.text = "Offline";
			PhotonNetwork.MaxResendsBeforeDisconnect = 0;
			
		}
		else
		{	
			// Switch to Online
			online = true;
			onlineToggleButton.SetActive(true);
			offlineToggleButton.SetActive(false);
			PhotonNetwork.offlineMode = false;
			StartOnline();
			networkInformation.enabled = true;
			PhotonNetwork.MaxResendsBeforeDisconnect = 15;
		}
	}
	
	public void OnClickToggleLocked()
	{
		isLocked = UIToggle.current.value;
	}
	
	public void OnClickToggleVisible()
	{
		isVisible = UIToggle.current.value;
	}
	
	public void OnClickToggleOpen()
	{
		isOpen = UIToggle.current.value;
	}
}

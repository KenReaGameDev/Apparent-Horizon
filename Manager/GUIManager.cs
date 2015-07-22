using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIManager : MonoBehaviour {

	public TargetingUI UI_Targeting;
	public SpaceStationUI UI_SpaceStation;
	public GameObject UI_Pause;
	
	List<UIBase> allInterfaces = new List<UIBase>();

	// Use this for initialization
	void Start () {

		if (UI_Targeting == null)
			UI_Targeting = GetComponentInChildren<TargetingUI>();

		if (UI_SpaceStation == null)
			UI_SpaceStation = GetComponentInChildren<SpaceStationUI>();

		allInterfaces.Add(UI_Targeting);
		allInterfaces.Add(UI_SpaceStation);
		foreach (UIBase uis in allInterfaces)
		{
			uis.SetManager(this);
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			UI_Pause.SetActive(!UI_Pause.activeSelf);
		}
	}
	
	public void OnClickResume()
	{
		UI_Pause.SetActive(false);
	}
	
	public void DisabeAllUI()
	{
		foreach (UIBase uis in allInterfaces)
		{
			uis.DisableUI();
		}
	}

	public void DisableOtherUI(UIBase currentUI)
	{
		foreach (UIBase uis in allInterfaces)
		{
			if (uis == currentUI)
				continue;

			uis.DisableUI();
		}
	}

	public void EnablePlayingUI()
	{
		UI_Targeting.EnableUI();
	}
}

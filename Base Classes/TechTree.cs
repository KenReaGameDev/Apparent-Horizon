using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TechTree : MonoBehaviour {

	public Dictionary <string, Tech> techTree = new Dictionary<string, Tech>();

	// Use this for initialization
	void Start () {
		// Test First Tech loading.
		CreateTechs();
	}

	void CreateTechs()
	{
		Tech d = new Destroyers(null, this);
	}

	// Update is called once per frame
	void Update () {
	
	}
}

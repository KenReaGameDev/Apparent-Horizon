using UnityEngine;
using System.Collections;

public class Tech {

	public string name;
	public string description;

	protected bool custom;
	protected bool unlocked;

	protected Tech[] next;
	protected Tech previous;
	protected TechTree tree;

	protected float baseCost;
	protected float multiplier;
	protected float level;

	public Sprite icon;


	// Update is called once per frame
	void Update () {
	
	}

	protected void DebugAnnounce()
	{
		Debug.LogWarning(this.name + " Created.");
	}

	protected virtual float CalculateUpgradeCost()
	{
		return 0;
	}

	// Allows Custom techs to be implemented.
	public void AddCustomTech(string previousTechName)
	{
		tree.techTree.TryGetValue(previousTechName, out previous);

		if (previous == null)
			return;
	
		// Resize the array to allow new tech.
		Tech[] temp = new Tech[next.Length - 1];
		next.CopyTo(temp, 0);
		next = new Tech[temp.Length + 1];
		temp.CopyTo(next, 0);
		next[next.Length-1] = this;
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AiPathing : MonoBehaviour {
	
	Transform target;
	float keepDistance;
	
	// Last Vector will be target. 
	List<Vector3> position = new List<Vector3>();
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		// if no target.. roam in large paths.
		
	}
	
	void CheckPath() {
		// Ray cast to position (target). Initially this will be target.
		// If collosion is not target, decide where to move next.
		// Add a new positon if needed.
	}
	
	void RemovePosition() {
		// If List > 1
		// Check to see if is within certain distance of position.
		// Remove position if it is.
	}
}

using UnityEngine;
using System.Collections;

public class PositionPredict : MonoBehaviour {

	PhotonView photonView;

	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	
	private Vector3 previousPosition = Vector3.zero;
	private Vector3 currentPosition = Vector3.zero;
	private Vector3 nextPosition = Vector3.zero;
	
	private Vector3 recievedVelocity = Vector3.zero;
	private Vector3 recievedVelocityChange = Vector3.zero;
	
	private Vector3 currentVelocity = Vector3.zero;
	private Vector3 previousVelocity = Vector3.zero;
	
	private float velocitychange = 0;
	private Vector3 predictedVelocityChange = Vector3.zero;
	private Vector3 predictedVelocity = Vector3.zero;
	
	int frame = 0;
	int sendFrame = 0;
	// Use this for initialization
	void Start () {
	
		if (photonView == null)
			photonView = GetComponent<PhotonView>();
			
		sendFrame = 60 / PhotonNetwork.sendRate;
							
	}
	
	// Update is called once per frame
	void Update () {
		if (photonView.isMine)
		{
			previousVelocity = currentVelocity;
			currentVelocity = rigidbody.velocity;
			
			frame++;
			if (frame == sendFrame)
				UpdateVelocityForOthers();
		}
		else
			SyncPositionUpdated();
	}
	
	// Needs rewrite to accept and deal with RPC
	void SyncPosition()
	{
		if (rigidbody.velocity.magnitude == 0)
		{
			nextPosition = currentPosition + predictedVelocity;	
			Debug.Log(predictedVelocity.magnitude);
			if (nextPosition.magnitude > 0)		
				transform.position = Vector3.Lerp(currentPosition, nextPosition, 1);
			
			return;
		}
		
		previousVelocity = currentVelocity;
		currentVelocity = rigidbody.velocity;
		
		previousPosition = currentPosition;
		currentPosition = transform.position;
		
		// Ratio
		velocitychange = previousVelocity.magnitude / currentVelocity.magnitude;
		predictedVelocityChange = currentVelocity / velocitychange;
		
		predictedVelocity = (currentVelocity + predictedVelocityChange) / 2;
		
		nextPosition = predictedVelocity + currentPosition;		
		
		Debug.Log(predictedVelocity.magnitude);
		transform.position = Vector3.Lerp(currentPosition, nextPosition, 1);	
	}
	
	void UpdateVelocityForOthers()
	{
		photonView.RPC("RecieveVelocityRPC", PhotonTargets.Others, currentVelocity, currentVelocity - previousVelocity);
		frame = 0;
	}
	
	[RPC] public void RecieveVelocityRPC(Vector3 inVelocity, Vector3 change)
	{
		recievedVelocity = inVelocity;
		recievedVelocityChange = change;
		frame = 0;
	}
	
	void SyncPositionUpdated()
	{
		if (frame == 0)
		{
			currentVelocity = recievedVelocity;
			previousVelocity = currentVelocity + recievedVelocityChange;
		}
		
		if (currentVelocity == Vector3.zero)
			return;
			
		previousPosition = currentPosition;
		currentPosition = transform.position;
		
		velocitychange = previousVelocity.magnitude / currentVelocity.magnitude;
		predictedVelocityChange = currentVelocity / velocitychange;
		
		predictedVelocity = (currentVelocity + predictedVelocityChange) / 2;
		nextPosition = predictedVelocity + currentPosition;
		
		transform.position = Vector3.Lerp(currentPosition, nextPosition, Time.deltaTime);
	}
	
	void LateUpdate()
	{

	}
}

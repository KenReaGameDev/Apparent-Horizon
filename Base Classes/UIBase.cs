using UnityEngine;
using System.Collections;

public class UIBase : MonoBehaviour {

	public bool isEnabled = false;
	public Camera UICamera;
	protected GUIManager manager;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public virtual void DisableUI()
	{
		UICamera.gameObject.SetActive(false);
		this.enabled = false;
	}

	public virtual void EnableUI()
	{
		UICamera.gameObject.SetActive(true);
		this.enabled = true;
	}

	public void SetManager(GUIManager inManager)
	{
		manager = inManager;
	}
}

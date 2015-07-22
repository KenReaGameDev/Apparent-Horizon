using UnityEngine;
using System.Collections;

public class BuffInfo {

	public float power;
	public float time;
	public bool  infinite;
	
	public float prevPower;
	public bool  prevInfinite;
	
	public BuffInfo (float p, float t, bool i)
	{
		power = p;
		time = t;
		infinite = i;		
	}
	
	public void SwapInfinite (float p, float t)
	{
		prevPower = power;
		prevInfinite = true;
		
		power = p;
		time = t;
		infinite = false;
	}
	
	public void SwapInfinite()
	{
		power = prevPower;
		infinite = prevInfinite;
		
		prevPower = 1;
		prevInfinite = false;
	}
	
	public float magnitude()
	{
		if (infinite)
			return 999.9f;

		return (power + time);
	}
}

using UnityEngine;
using System.Collections;

public class SystemSeed {

private static readonly System.Random random = new System.Random();
private static readonly object syncLock = new object();
		
	public static int[] RandomSeed()
	{
		int[] seedArr = new int[10];
		for (int i = 0; i < 9; ++i)
		{
		    lock(syncLock) { // synchronize
		        seedArr[i] = random.Next(0, 9);
			}
		}
		return seedArr;
	}
	
	public static int RandomSeed_Zero_to_OneHundred()
	{
		int seedInt = 0;
		lock(syncLock)
			seedInt = random.Next(0, 100);
		return seedInt;		
	}
	
	public static int RandomSeed_Custom(int inMin, int inMax)
	{
		int seedInt = 0;
		lock(syncLock)
			seedInt = random.Next(inMin, inMax);
		return seedInt;		
	}
}

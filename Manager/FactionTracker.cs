using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.Linq.Expressions;
using System.Linq;
using System.IO;

public class FactionTracker{

	private static FactionTracker instance;
	
	private FactionTracker() 
	{
		Start();		
	}

	public static FactionTracker Instance 
	{
		get
		{
			if (instance == null)
			{
				Debug.Log("Creating new Faction Tracker");
				instance = new FactionTracker();
			}
			return instance;
		}
	}	
	
	public static void CleanInstance()
	{
		instance = null;
	}
	
	Dictionary<string, Factions> relationsListDictionary = new Dictionary<string, Factions>();
	
	void Start () {

		relationsListDictionary.Clear();
		Debug.Log("Initializing Factions");
		InitFactions();
		
		Ships.shipList = new List<Ships.AttributesPreLoad>();
		
		foreach (var pair in relationsListDictionary)
		{
			SerializeAllFactionShips(pair.Value.FactionName);	
		}
		
		Debug.Log("FACTIONS IN AISHIPLIST COUNT: " + Ships.shipList.Count());
		foreach(Ships.AttributesPreLoad apl in Ships.shipList)
		{
			foreach (Attributes aia in apl.factionShipList)
			{
				//Debug.Log(aia.faction + "/" + aia.modelName);	
			}
		}

	}
	
	public void ForceLoadDictionaries()
	{
		relationsListDictionary.Clear();
		Debug.Log("Initializing Factions");
		InitFactions();
		
		Ships.shipDictionary.Clear();
		
		Ships.shipList = new List<Ships.AttributesPreLoad>();

		foreach (var pair in relationsListDictionary)
		{
			SerializeAllFactionShips(pair.Value.FactionName);	
		}
		
		Debug.Log("FACTIONS IN AISHIPLIST COUNT: " + Ships.shipList.Count());
		foreach(Ships.AttributesPreLoad apl in Ships.shipList)
		{
			foreach (Attributes aia in apl.factionShipList)
			{
				//Debug.Log(aia.faction + "/" + aia.modelName);	
			}
		}
	}
	
	void InitFactions()
	{
		int count = 0;
		string filepath = Application.dataPath + "/Resources/Data Files/factiondata.xml";
		XDocument factionXML = XDocument.Load(filepath);
		
		// Creates an array of factions.
		var factionNames = from factionName in factionXML.Root.Elements("FactionAttributes")
			select new {
				factionName_XML = (string)factionName.Element("name"),
				factionID_XML = (int)factionName.Element("id"),
				//TODO: Need to turn this into array.
				factionRelations_XML = factionName.Element("relations").Descendants("id").ToArray()//(string[])(from rel in factionName.Descendants("relations") select (string)rel.Value).ToArray<string>()//(int[])factionName.Element("relations")
		};
		//
		//
		foreach ( var factionName in factionNames)
			++count;

		foreach ( var factionName in factionNames)
		{
			int cnt = factionName.factionRelations_XML.Length;
			//Debug.Log(factionName.factionName_XML + " Relations Count :: " +  cnt);
			Factions f = new Factions();		
			f.index = relationsListDictionary.Count;
			f.otherFactionsName = new string[count];
			f.otherFactionsRelation = new int[count];
			int others = 0;
			
			f.FactionName = factionName.factionName_XML;

			//Debug.Log(factionName.factionRelations_XML);
			
			// Adds Rivals, not self to other list.
			foreach (var factionName2 in factionNames)
			{
				if (factionName.factionID_XML == factionName2.factionID_XML)
					continue;

				f.relations.Add(factionName2.factionName_XML, (int)factionName.factionRelations_XML[others]);
				//Debug.Log(f.relations[factionName2.factionName_XML]);
				//f.otherFactionsName[(int)factionName2.factionID_XML] = factionName2.factionName_XML;
				//f.otherFactionsRelation[(int)factionName2.factionID_XML] = factionName.factionRelations_XML[(int)factionName2.factionID_XML];
				//Debug.Log(f.FactionName + " adds: " + factionName2.factionName_XML);
				++others;
			}
			
			XDocument shipXML = XDocument.Load(Application.dataPath + "/Resources/Ships/" + f.FactionName + "/shipdata.xml");
			f.typeOfShips = shipXML.Descendants("Attributes").Count();		
			//Debug.Log (f.FactionName);
			relationsListDictionary.Add(f.FactionName, f);
		}
	}
	
	void Update () {
	
	}
	
	// Preloading all Serialized Ships.
	// May have to redo if minor factions are created mid game. 
	// UPDATE!!
	// TODO: Make sure adding players into the faction doesn't blow it up.
	void SerializeAllFactionShips(string inFaction)
	{		
		//Debug.Log("FACTION SERIALIZING: " + inFaction);
		Ships.AttributesPreLoad thisFaction = new Ships.AttributesPreLoad();		
		string filepath = Application.dataPath + "/Resources/Ships/" + inFaction + "/shipdata.xml";
		
		
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Attributes>));
		// Open a filestream to stream in the text.
		Stream streamReader = new FileStream(filepath, FileMode.Open);
		
		// Let the deserializer do its work. If the XML was serialized properly, no errors should occur.
		// 
		thisFaction.factionShipList = (List<Attributes>)xmlSerializer.Deserialize(streamReader);			
		streamReader.Close();
		Ships.shipList.Add(thisFaction);
		thisFaction.factionShipDictionary = new Dictionary<string, Attributes>();
		
		foreach( Attributes att in thisFaction.factionShipList)
		{
			string shipname = att.faction + att.modelName;			
			try
			{
				thisFaction.factionShipDictionary.Add(att.modelName, att);
				//Ships.shipDictionary.Add(shipname, att);
			}
			catch
			{
				Debug.LogWarning(att.faction + " / " + att.modelName);
			}
		}
		
		Ships.shipDictionary.Add(inFaction, thisFaction);
		//Debug.Log("COUNT NEXT: " + Ships.shipList.Count());
		
	}
	
	// Gets faction by name.
	public Factions GetFaction(string inFaction)
	{
		Factions f;
		relationsListDictionary.TryGetValue(inFaction, out f);;

		if (f == null)
			return new Factions();
		else
			return f;
	}
		
	public int GetFactionIndex(string inName)
	{
		try
		{
			return relationsListDictionary[inName].index;
		}
		catch
		{
			Debug.Log("No Such Faction");
		}

		return -1;
		
	}
	
	public int GetFactionCount()
	{
		return relationsListDictionary.Count;
	}

	public string[] GetFactionNames()
	{
		string[] factionNamesList = relationsListDictionary.Keys.ToArray();
		return factionNamesList;
	}
	
	public List<XDocument> GetAllFactionShipDatas ()
	{
		List<XDocument> factionShipXMLS = new List<XDocument>();

		foreach (var pair in relationsListDictionary)
		{
			string filepath = Application.dataPath + "/Resources/Ships/" + pair.Key + "/shipdata.xml";
			XDocument factionXML = XDocument.Load(filepath);
			//Debug.Log("SHIP COUNT FOR THIS FACTION: " + factionXML.Descendants("ShipAttributes").Count());
			factionShipXMLS.Add(factionXML);
		}
		return factionShipXMLS;
	}
	
	// Gets the relationship between two factions for one faction.
	public int GetRelationFor(string forFaction, string againstFaction)
	{
		int relation = -1;
		try
		{
			relation = relationsListDictionary[forFaction].relations[againstFaction];
		}
		catch
		{
			Debug.Log("Faction checking error");
		}

		return relation;

	}
}

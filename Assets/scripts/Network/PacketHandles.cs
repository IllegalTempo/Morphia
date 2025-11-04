using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class PacketHandles_Method
{
	public static void Server_Handle_test(NetworkPlayer p, packet packet)
	{
		string text = packet.ReadstringUNICODE();
		long clienttime = packet.Readlong();
		if (text == PacketSend.TestRandomUnicode)
		{
			Debug.Log($"{clienttime} Confirmed {p.SteamName}, successfully connected, delay:{(DateTime.Now.Ticks - clienttime) / 10000}ms");
			//trigger listeners
			NetworkListener.Server_OnPlayerJoinSuccessful?.Invoke(p);

		}
		else
		{
			Debug.Log($"Check Code Mismatched Client Message: {text}");
		}
	}

	public static void Server_Handle_AnimationState(NetworkPlayer p, packet packet)
	{
		float movex = packet.Readfloat();
		float movey = packet.Readfloat();
		p.player.SetAnimation(movex, movey);
		PacketSend.Server_DistributePlayerAnimationState(p.NetworkID, movex, movey);
	}
	public static void Server_Handle_ReadyUpdate(NetworkPlayer p, packet packet)
	{
		bool ready = packet.Readbool();
		p.MovementUpdateReady = ready;
		Debug.Log($"Player {p.SteamName} is ready for receiving pos informations!");
	}
	public static void Server_Handle_PosUpdate(NetworkPlayer p, packet packet)
	{
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();
		Quaternion yrot = packet.Readquaternion();
		p.player.SetMovement(pos, rot, yrot);
		PacketSend.Server_DistributeMovement(p.NetworkID, pos, rot, yrot);
	}



	public static async void Client_Handle_test(Connection c, packet packet)
	{
		int NetworkID = packet.Readint();
		string text = packet.ReadstringUNICODE();
		long Servertime = packet.Readlong();
		NetworkSystem.instance.client.NetworkID = NetworkID;

		if (text == PacketSend.TestRandomUnicode)
		{
			Debug.Log($"{Servertime} Confirmed connected from server. delay:{(DateTime.Now.Ticks - Servertime) / 10000}ms");

		}
		else
		{
			Debug.Log($"Check Code Mismatched Server Message: {text}");

		}
		await Task.Delay(5);
		PacketSend.Client_Send_test();
	}
	public static async void Client_Handle_InitRoomInfo(Connection c, packet packet)
	{
		int numplayer = packet.Readint();
		GameClient client = NetworkSystem.instance.client;
		for (int i = 0; i < numplayer; i++)
		{
			int NetworkID = packet.Readint();
			ulong steamid = packet.Readulong();
			Debug.Log($"Spawning Player {NetworkID} {steamid}");
			client.GetPlayerByNetworkID.Add(NetworkID, NetworkSystem.instance.SpawnPlayer(client.IsLocal(NetworkID), NetworkID, steamid));


		}
		await Task.Delay(1000);
		PacketSend.Client_Send_ReadyUpdate();
	}
	public static void Client_Handle_NewPlayerJoin(Connection c, packet packet)
	{
		ulong playerid = packet.Readulong();
		int supposeNetworkID = packet.Readint();




		NetworkSystem.instance.client.GetPlayerByNetworkID.Add(supposeNetworkID, NetworkSystem.instance.SpawnPlayer(false, supposeNetworkID, playerid));
	}
	public static void Client_Handle_PlayerQuit(Connection c, packet packet)
	{
		GameClient cl = NetworkSystem.instance.client;
		int NetworkID = packet.Readint();
		cl.GetPlayerByNetworkID[NetworkID].Disconnect();
		cl.GetPlayerByNetworkID.Remove(NetworkID);
	}

	public static void Client_Handle_ReceivedPlayerMovement(Connection c, packet packet)
	{
		int NetworkID = packet.Readint();

		Vector3 pos = packet.Readvector3();
		Quaternion headrot = packet.Readquaternion();
		Quaternion bodyrot = packet.Readquaternion();
		NetworkSystem.instance.client.GetPlayerByNetworkID[NetworkID].SetMovement(pos, headrot, bodyrot);
	}


	public static void Client_Handle_ReceivedPlayerAnimation(Connection c, packet packet)
	{
		int NetworkID = packet.Readint();
		float x = packet.Readfloat();
		float y = packet.Readfloat();
		NetworkSystem.instance.client.GetPlayerByNetworkID[NetworkID].SetAnimation(x, y);
	}

	public static void Client_Handle_DistributeNOInfo(Connection c, packet packet)
	{
		string uuid = packet.ReadstringUNICODE();
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();
		if (NetworkSystem.instance.FindNetworkObject.ContainsKey(uuid))
		{
			NetworkSystem.instance.FindNetworkObject[uuid].SetMovement(pos, rot);

		}
		else
		{
			Debug.LogError($"No Network Object with UUID {uuid} found!");
		}
	}

	public static void Server_Handle_SendNOInfo(NetworkPlayer p, packet packet)
	{
		string uuid = packet.ReadstringUNICODE();
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();
		NetworkSystem.instance.FindNetworkObject[uuid].transform.position = pos;
		NetworkSystem.instance.FindNetworkObject[uuid].transform.rotation = rot;


		PacketSend.Server_Send_DistributeNOInfo(uuid, pos, rot);


	}

	public static void Server_Handle_PickUpItem(NetworkPlayer p, packet packet)
	{
		string itemid = packet.ReadstringUNICODE();
		int whopicked = packet.Readint();
		NetworkSystem.instance.FindNetworkObject[itemid].GetComponent<item>().PickUpItem(whopicked);


		PacketSend.Server_Send_DistributePickUpItem(itemid, whopicked);

	}

	public static void Client_Handle_DistributePickUpItem(Connection c, packet packet)
	{
		string itemid = packet.ReadstringUNICODE();
		int whopicked = packet.Readint();

		NetworkSystem.instance.FindNetworkObject[itemid].GetComponent<item>().PickUpItem(whopicked);
	}

	public static void Client_Handle_StartGame(Connection c, packet packet)
	{
		string scenename = packet.ReadstringUNICODE();
		gamecore.instance.StartGame(scenename);

	}

	public static void Client_Handle_DistributeInitialPos(Connection c, packet packet)
	{
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();

		gamecore.instance.LocalPlayer.transform.position = pos;
		gamecore.instance.LocalPlayer.transform.rotation = rot;
		Debug.Log("Received Initial Pos and Rot");
	}

	public static void Client_Handle_Distribute_stickItem(Connection c, packet packet)
	{
		string itemid = packet.ReadstringUNICODE();
		string targetitemid = packet.ReadstringUNICODE();
		item itemToStick = NetworkSystem.instance.FindNetworkObject[itemid].GetComponent<item>();
		item targetItem = NetworkSystem.instance.FindNetworkObject[targetitemid].GetComponent<item>();
		if (itemToStick != null && targetItem != null)
		{
			itemToStick.StickTo(targetItem);
			Debug.Log($"Item {itemid} stuck to {targetitemid}");
		}
		else
		{
			Debug.LogError($"Failed to stick items: {itemid} or {targetitemid} not found.");
		}
	}

	public static void Server_Handle_stickItem(NetworkPlayer p, packet packet)
	{
		string itemid = packet.ReadstringUNICODE();
		string targetitemid = packet.ReadstringUNICODE();
		item itemToStick = NetworkSystem.instance.FindNetworkObject[itemid].GetComponent<item>();
		item targetItem = NetworkSystem.instance.FindNetworkObject[targetitemid].GetComponent<item>();
		if (itemToStick != null && targetItem != null)
		{
			itemToStick.StickTo(targetItem);
			Debug.Log($"Item {itemid} stuck to {targetitemid} by player {p.SteamName}");
			PacketSend.Server_Send_Distribute_stickItem(itemid, targetitemid);
		}
		else
		{
			Debug.LogError($"Failed to stick items: {itemid} or {targetitemid} not found.");
		}
	}

	public static void Server_Handle_drop(NetworkPlayer p, packet packet)
	{
		string itemid = packet.ReadstringUNICODE();
		item itemToDrop = NetworkSystem.instance.FindNetworkObject[itemid].GetComponent<item>();
		if (itemToDrop != null)
		{
			itemToDrop.Drop(itemToDrop.transform.position,false);
			Debug.Log($"Item {itemid} dropped by player {p.SteamName}");
			PacketSend.Server_Send_DistributeDrop(itemid);
		}
		else
		{
			Debug.LogError($"Failed to drop item: {itemid} not found.");
		}
	}

	public static void Client_Handle_DistributeDrop(Connection c, packet packet)
	{
		string itemid = packet.ReadstringUNICODE();
		item itemToDrop = NetworkSystem.instance.FindNetworkObject[itemid].GetComponent<item>();
		if (itemToDrop != null)
		{
			itemToDrop.Drop(itemToDrop.transform.position,false);

			Debug.Log($"Item {itemid} dropped.");
		}
		else
		{
			Debug.LogError($"Failed to drop item: {itemid} not found.");
		}
	}

	public static void Client_Handle_DistributeNewMission(Connection c, packet packet)
	{
		string missionID = packet.ReadstringUNICODE();
		string missiontitle = packet.ReadstringUNICODE();
		string missiondesc = packet.ReadstringUNICODE();
		bool newmission = packet.Readbool();
		if (newmission)
		{
			gamecore.instance.AddMission(missionID, missiontitle, missiondesc);
		}
		else
		{
			gamecore.instance.FinishMission(missionID);
		}
	}

	public static void Client_Handle_enterconversation(Connection c, packet packet)
	{
		string conversationid = packet.ReadstringUNICODE();
		gamecore.instance.StartConversation(conversationid,false);
	}

	public static void Server_Handle_EnterConversation(NetworkPlayer p, packet packet)
	{
		string conversationid = packet.ReadstringUNICODE();
		gamecore.instance.StartConversation(conversationid,false);
	}

	public static void Client_Handle_nextdialogue(Connection c, packet packet)
	{
		gamecore.instance.PlayNextDialogue();
	}

	public static void Server_Handle_nextdialogue(NetworkPlayer p, packet packet)
	{
		gamecore.instance.PlayNextDialogue();
	}
}



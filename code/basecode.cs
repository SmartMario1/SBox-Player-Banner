using Sandbox;
using Sandbox.Internal;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Net;
using Ban.UI;

namespace Ban
{

	[Spawnable]
	public class BanEntity : ModelEntity, IUse
	{

		public static BanUi MyPanel { get; set; } = null;

		private static Dictionary<long, string> BanList = new Dictionary<long, string>();

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/banner/scifi_computer.vmdl" );
			PhysicsEnabled = false;
			Scale *= 1.7f;
			UsePhysicsCollision = false;
		}

		// Couldn't figure out how this worked, gave up.
		//public override void ClientSpawn()
		//{ 
		//	base.ClientSpawn();
		//	var worldpanel = new WorldPanel(Game.SceneWorld);
		//	var panel = worldpanel.Add.Panel();
		//	panel.SetContent("<h1>Hello, World!</h1>");
		//	worldpanel.Position = Position + new Vector3( 0, 0, 30 );
		//	worldpanel.WorldScale *= 2;
		//	
		//
		//	Log.Info( worldpanel);
		//}

		public bool IsUsable( Entity client )
		{
			return client.IsValid() && client.Tags.Has( "player" ) && client.Health > 0;
		}

		public bool OnUse( Entity client )
		{
			Game.AssertServer();
			client.Client.SendCommandToClient( "ban_entity_toggle_ui " + NetworkIdent );
			return false;
		}

		[ConCmd.Client( "ban_entity_toggle_ui" )]
		public static void ToggleUI( int netID )
		{
			if ( MyPanel == null )
			{
				MyPanel = Game.RootPanel.FindRootPanel().AddChild<BanUi>();
				MyPanel.SetDict( BanList, netID );
			}
			else
			{
				MyPanel?.Delete();
				MyPanel = null;
			}
		}

		[ConCmd.Server("ban_entity_ban_user")]
		public static void AddToBanList(long steamid, string steamname)
		{
			// This probably isn't very secure but no ones gonna bother to cheat the system anyway.
			if ( Game.SteamId == ConsoleSystem.Caller.SteamId )
			{
				if ( !BanList.ContainsKey( steamid ) )
				{
					BanList.Add( steamid, steamname );
				}
				RunThroughBanList();
				Log.Info( "Banned " + steamname );
			}
			else
			{
				Log.Error( ConsoleSystem.Caller.Name + " Tried to ban someone without being the host!" );
			}
		}
		[ConCmd.Server( "ban_entity_unban_user" )]
		public static void RemoveFromBanList( long steamid)
		{
			// This probably isn't very secure but no ones gonna bother to cheat the system anyway.
			if ( Game.SteamId == ConsoleSystem.Caller.SteamId )
			{
				Log.Info( "Unbanned " + BanList[steamid] );
				BanList.Remove( steamid );

			}
			else
			{
				Log.Error( ConsoleSystem.Caller.Name + " Tried to unban someone without being the host!" );
			}
		}

		[ConCmd.Admin( "ban_entity_get_banlist" )]
		private static void GetBanlist()
		{
			Log.Info( "Banlist:" );
			foreach ( var item in BanList.Keys )
			{
				Log.Info( item + ": " + BanList[item] );
			}
		}

		private static void RunThroughBanList()
		{
			foreach(var player in Game.Clients)
			{
				if (BanList.ContainsKey(player.SteamId))
				{
					player.Kick();
				}
			}
		}

		[GameEvent.Server.ClientJoined]
		private void ClientJoined(ClientJoinedEvent player_event)
		{
			var steamid = player_event.Client.SteamId;
			if (BanList.ContainsKey(steamid) )
			{
				Log.Info( "Banned user " + player_event.Client.Name + " attempted to join.");
				player_event.Client.Kick();
			}
		}
	}

}

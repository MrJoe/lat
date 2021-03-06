// 
// lat - Preferences.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 .
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
//

using System;
using System.Collections.Generic;
using Gtk;

namespace lat
{
	public class Preferences
	{
		public const string MAIN_WINDOW_MAXIMIZED = "/apps/lat/ui/maximized";

		public const string MAIN_WINDOW_X = "/apps/lat/ui/main_window_x";
		public const string MAIN_WINDOW_Y = "/apps/lat/ui/main_window_y";
		public const string MAIN_WINDOW_WIDTH = "/apps/lat/ui/main_window_width";
		public const string MAIN_WINDOW_HEIGHT = "/apps/lat/ui/main_window_height";
		public const string MAIN_WINDOW_HPANED = "/apps/lat/ui/main_window_hpaned";

		public const string BROWSER_SELECTION = "/apps/lat/ui/browser_selection";

		static GConf.Client client;
		static GConf.NotifyEventHandler changed_handler;

		public static GConf.Client Client 
		{
			get {
				if (client == null) {
					client = new GConf.Client ();

					changed_handler = new GConf.NotifyEventHandler (OnSettingChanged);
					client.AddNotify ("/apps/lat", changed_handler);
				}
				return client;
			}
		}

		public static object GetDefault (string key)
		{
			switch (key) 
			{
				case MAIN_WINDOW_X:
				case MAIN_WINDOW_Y:
				case MAIN_WINDOW_HEIGHT:
				case MAIN_WINDOW_WIDTH:
				case MAIN_WINDOW_HPANED:
					return null;
					
				case BROWSER_SELECTION:
					return 2;
			}

			return null;
		}

		public static object Get (string key)
		{
			try {
				return Client.Get (key);
			} catch (GConf.NoSuchKeyException) {
				object default_val = GetDefault (key);

				if (default_val != null)
					Client.Set (key, default_val);

				return default_val;
			}
		}

		public static void Set (string key, object value)
		{
			Client.Set (key, value);
		}

		public static event GConf.NotifyEventHandler SettingChanged;

		static void OnSettingChanged (object sender, GConf.NotifyEventArgs args)
		{
			if (SettingChanged != null) {
				SettingChanged (sender, args);
			}
		}
	}
	
	public class PreferencesDialog
	{
		Glade.XML ui;
		
		[Glade.Widget] Gtk.Dialog preferencesDialog;
		[Glade.Widget] RadioButton browserSingleClickButton;
		[Glade.Widget] RadioButton browserDoubleClickButton;
		[Glade.Widget] TreeView profilesTreeView;

		ListStore profileStore;
		Gnome.Program program;
		bool gettingHelp = false;
			
		public PreferencesDialog (Gnome.Program program)
		{
			ui = new Glade.XML (null, "lat.glade", "preferencesDialog", null);
			ui.Autoconnect (this);
			
			this.program = program;
			
			profileStore = new ListStore (typeof (string));
			profilesTreeView.Model = profileStore;
			profileStore.SetSortColumnId (0, SortType.Ascending);
			
			TreeViewColumn col;
			col = profilesTreeView.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			UpdateProfileList ();
					
			LoadPreference (Preferences.BROWSER_SELECTION);
					
			preferencesDialog.Icon = Global.latIcon;
			preferencesDialog.Resize (300, 400);			
			preferencesDialog.Run ();
			
			if (gettingHelp) {
				preferencesDialog.Run ();
				gettingHelp = false;
			}
			
			preferencesDialog.Destroy ();
		}

		void UpdateProfileList ()
		{
			profileStore.Clear ();
			
			foreach (string s in Global.Connections.ConnectionNames)
				profileStore.AppendValues (s);
		}

		void LoadPreference (String key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			switch (key) {
				
			case Preferences.BROWSER_SELECTION:
				int b = (int) val;
				if (b == 1)
					browserSingleClickButton.Active = true;
				else if (b == 2)
					browserDoubleClickButton.Active = true;
					
				break;
			}
		}

		string GetSelectedProfileName ()
		{
			TreeIter iter;
			TreeModel model;

			if (profilesTreeView.Selection.GetSelected (out model, out iter))  {
				string name = (string) model.GetValue (iter, 0);
				return name;
			}

			return null;
		}

		public void OnAddProfileClicked (object o, EventArgs args)
		{
			new ProfileDialog ();
			UpdateProfileList ();		
		}
		
		public void OnEditProfileClicked (object o, EventArgs args)
		{
			string profileName = GetSelectedProfileName ();
			if (profileName == null)
				return;
			
			Connection conn = Global.Connections [profileName];
			
			new ProfileDialog (conn);

			UpdateProfileList ();
		}
		
		public void OnDeleteProfileClicked (object o, EventArgs args)
		{
			string profileName = GetSelectedProfileName ();
			string msg = null;
			
			if (profileName == null) {
				// show dialog; must select entry to edit
				return;
			}

			msg = String.Format ("{0} {1}",
				Mono.Unix.Catalog.GetString (
				"Are you sure you want to delete the profile:"),
				profileName);
				
			if (Util.AskYesNo (preferencesDialog, msg)) {
				Global.Connections.Delete (profileName);
				Global.Connections.Save ();
				UpdateProfileList ();				
			}
		}
				
		public void OnDoubleClickToggled (object o, EventArgs args)
		{
			if (browserSingleClickButton.Active)
				Preferences.Set (Preferences.BROWSER_SELECTION, 1);
			else
				Preferences.Set (Preferences.BROWSER_SELECTION, 2);
		}
		
		public void OnHelpClicked (object o, EventArgs args)
		{
			try {

				gettingHelp = true;

				Gnome.Help.DisplayDesktopOnScreen (program, 
					Defines.PACKAGE, 
					"lat.xml", 
					"lat-preferences", 
					Gdk.Screen.Default);

			} catch (Exception e) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					null,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Help error",
					e.Message);

				dialog.Run ();
				dialog.Destroy ();
			}			
		}
	}
	
	public class PluginConfigureDialog
	{
		Glade.XML ui;
		
		[Glade.Widget] Gtk.Dialog pluginConfigureDialog;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.Button newContainerButton;
		[Glade.Widget] Gtk.Button searchBaseButton;
		[Glade.Widget] TreeView columnsTreeView; 

		ListStore columnStore;
		Connection conn;
		ViewPlugin vp;
		List<string> colNames;
		List<string> colAttrs;
			
		public PluginConfigureDialog (Connection connection, string pluginName)
		{
			conn = connection;
			colNames = new List<string> ();
			colAttrs = new List<string> ();
		
			ui = new Glade.XML (null, "lat.glade", "pluginConfigureDialog", null);
			ui.Autoconnect (this);

			columnStore = new ListStore (typeof (string), typeof (string));
			
			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnNameEdit);			
			columnsTreeView.AppendColumn ("Name", cell, "text", 0);
			
			cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);			
			columnsTreeView.AppendColumn ("Attribute", cell, "text", 1);
			
			columnsTreeView.Model = columnStore;
			
			vp = Global.Plugins.GetViewPlugin (pluginName, connection.Settings.Name); 
			if (vp != null) {			
				for (int i = 0; i < vp.ColumnNames.Length; i++) {  
					columnStore.AppendValues (vp.ColumnNames[i], vp.ColumnAttributes[i]);
					colNames.Add (vp.ColumnNames[i]);
					colAttrs.Add (vp.ColumnAttributes[i]);
				}
					
				filterEntry.Text = vp.Filter;
					
				if (vp.DefaultNewContainer != "")
					newContainerButton.Label = vp.DefaultNewContainer;

				if (vp.SearchBase != "")
					searchBaseButton.Label = vp.SearchBase;					
			} else {
				Log.Error ("Unable to find view plugin {0}", pluginName); 
			}
										
			pluginConfigureDialog.Icon = Global.latIcon;
			pluginConfigureDialog.Resize (300, 400);
			pluginConfigureDialog.Run ();
			pluginConfigureDialog.Destroy ();
		}

		void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!columnStore.GetIterFromString (out iter, args.Path))
				return;
				
			string oldAttr = (string) columnStore.GetValue (iter, 1);		
			colAttrs.Remove (oldAttr);
			colAttrs.Add (args.NewText);
			
			columnStore.SetValue (iter, 1, args.NewText);
		}
		
		void OnNameEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!columnStore.GetIterFromString (out iter, args.Path))
				return;
				
			string oldName = (string) columnStore.GetValue (iter, 0);		
			colNames.Remove (oldName);	
			colNames.Add (args.NewText);
			
			columnStore.SetValue (iter, 0, args.NewText);
		}
		
		public void OnOkClicked (object o, EventArgs args)
		{
			ViewPluginConfig vpc = new ViewPluginConfig ();
			vpc.ColumnAttributes = colAttrs.ToArray ();
			vpc.ColumnNames = colNames.ToArray ();
			vpc.PluginName = vp.Name;
			vpc.Filter = filterEntry.Text;

			if (newContainerButton.Label != "")
				vpc.DefaultNewContainer = newContainerButton.Label;

			if (searchBaseButton.Label != "")
				vpc.SearchBase = searchBaseButton.Label;			

			if (vp.PluginConfiguration.Defaults == null)		
				vpc.Defaults = new Dictionary<string,string> ();
			else
				vpc.Defaults = vp.PluginConfiguration.Defaults;
						
			vp.PluginConfiguration = vpc;
			Global.Plugins.SetPluginConfiguration (conn.Settings.Name, vpc);
		}
		
		public void OnAddClicked (object o, EventArgs args)
		{
			columnStore.AppendValues ("Untitiled", "Unknown");
		}
		
		public void OnRemoveClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (columnsTreeView.Selection.GetSelected (out model, out iter)) {
				string name = (string) columnStore.GetValue (iter, 0);
				string attr = (string) columnStore.GetValue (iter, 1);
				colNames.Remove (name);
				colAttrs.Remove (attr);
				columnStore.Remove (ref iter);
			}
		}
		
		public void OnFilterBuildClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public void OnNewContainerClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (conn, null);
			scd.Message = String.Format (Mono.Unix.Catalog.GetString ("Select a container for new objects"));
			scd.Title = Mono.Unix.Catalog.GetString ("Select container");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
				newContainerButton.Label = scd.DN;
		}
		
		public void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (conn, null);
			scd.Message = String.Format (Mono.Unix.Catalog.GetString ("Select a search base"));
			scd.Title = Mono.Unix.Catalog.GetString ("Select container");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
				searchBaseButton.Label = scd.DN;
		}
		
		public void OnSetDefaultValuesClicked (object o, EventArgs args)
		{
			vp.OnSetDefaultValues (conn);
			Log.Debug ("OnSetDefaultValuesClicked vp.PluginConfiguration.Defaults.Count: {0}", vp.PluginConfiguration.Defaults.Count);
		}
	}
}

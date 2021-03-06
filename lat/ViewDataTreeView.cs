// 
// lat - ViewDataTreeView.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 
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

using Gtk;
using Gdk;
using GLib;
using System;
using System.Collections.Generic;
using Novell.Directory.Ldap;

namespace lat 
{
	public class ViewDataTreeView : Gtk.TreeView
	{
		Connection  conn;
		Gtk.Window	parentWindow;
		Menu 		popup;
		ListStore	dataStore;
		
		ViewPlugin viewPlugin;	
		int dnColumn;
		
		public ViewDataTreeView (Connection connection, Gtk.Window parent) : base ()
		{
			conn = connection;
			parentWindow = parent;
			
			this.Selection.Mode = Gtk.SelectionMode.Multiple; 
			
			this.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);
			this.RowActivated += new RowActivatedHandler (OnRowActivated);
			this.Selection.Changed += (object o, EventArgs args) => {};
			this.ShowAll ();
		}
		
		public void ConfigureView (ViewPlugin vp)
		{
			viewPlugin = vp;			
			SetViewColumns ();			
		}

		public void Populate ()
		{
			if (viewPlugin.SearchBase == null)
				viewPlugin.SearchBase = conn.DirectoryRoot;

			try {
				
				LdapEntry[] data = conn.Data.Search (conn.DirectoryRoot, viewPlugin.Filter);
				Log.Debug ("InsertData()\n\tbase: [{0}]\n\tfilter: [{1}]\n\tnumResults: [{2}]",
						viewPlugin.SearchBase, viewPlugin.Filter, data.Length);

				string msg = string.Format (Mono.Unix.Catalog.GetString ("Found {0} entries"), data.Length);
				Global.Window.WriteStatusMessage (msg);

				DoInsert (data, viewPlugin.ColumnAttributes);
				
			} catch (Exception e) {
									
				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Connection error",
					e.Message);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void DoInsert (LdapEntry[] objs, string[] attributes)
		{
			try {

				if (this.dataStore != null)
					this.dataStore.Clear ();

				foreach (LdapEntry le in objs) {
				
					string[] values = conn.Data.GetAttributeValuesFromEntry (le, attributes);
					string[] newvalues = new string [values.Length + 1];
													
					values.CopyTo (newvalues, 0);
					newvalues [values.Length] = le.DN;
					
					this.dataStore.AppendValues (newvalues);
				}

			} catch {

				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read data from server");

				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Network error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void DoPopUp()
		{
			popup = new Menu();

			ImageMenuItem newItem = new ImageMenuItem ("New");
			Gtk.Image newImage = new Gtk.Image (Stock.New, IconSize.Menu);
			newItem.Image = newImage;
			newItem.Activated += new EventHandler (OnNewEntryActivate);
			newItem.Show ();

			popup.Append (newItem);

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("document-save.png");
			ImageMenuItem exportItem = new ImageMenuItem ("Export");
			exportItem.Image = new Gtk.Image (pb);
			exportItem.Activated += new EventHandler (OnExportActivate);
			exportItem.Show ();

			popup.Append (exportItem);

			ImageMenuItem deleteItem = new ImageMenuItem ("Delete");
			Gtk.Image deleteImage = new Gtk.Image (Stock.Delete, IconSize.Menu);
			deleteItem.Image = deleteImage;
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();

			popup.Append (deleteItem);

			ImageMenuItem propItem = new ImageMenuItem ("Properties");
			Gtk.Image propImage = new Gtk.Image (Stock.Properties, IconSize.Menu);
			propItem.Image = propImage;
			propItem.Activated += new EventHandler (OnEditActivate);
			propItem.Show ();
			
			popup.Append (propItem);
			
			bool users = false;
			if (viewPlugin.Name.ToLower() == "users") {
			
				SeparatorMenuItem sm = new SeparatorMenuItem ();
				sm.Show ();
				popup.Append (sm);

				Gdk.Pixbuf pwdImage = Gdk.Pixbuf.LoadFromResource ("locked16x16.png");
				ImageMenuItem pwdItem = new ImageMenuItem ("Change password");
				pwdItem.Image = new Gtk.Image (pwdImage);
				pwdItem.Activated += new EventHandler (OnPwdActivate);
				pwdItem.Show ();
				
				popup.Append (pwdItem);				
				users = true;
			}
			
			if (users || viewPlugin.Name.ToLower() == "contacts") {
				
				if (!users) {
					SeparatorMenuItem sm = new SeparatorMenuItem ();
					sm.Show ();
					popup.Append (sm);				
				}
				
				pb = Gdk.Pixbuf.LoadFromResource ("mail-message-new.png");
				ImageMenuItem mailItem = new ImageMenuItem ("Send email");
				mailItem.Image = new Gtk.Image (pb);
				mailItem.Activated += new EventHandler (OnEmailActivate);
				mailItem.Show ();

				popup.Append (mailItem);

				Gdk.Pixbuf wwwImage = Gdk.Pixbuf.LoadFromResource ("go-home.png");
				ImageMenuItem wwwItem = new ImageMenuItem ("Open home page");
				wwwItem.Image = new Gtk.Image (wwwImage);
				wwwItem.Activated += new EventHandler (OnWWWActivate);
				wwwItem.Show ();

				popup.Append (wwwItem);			
			}
			
			popup.Popup(null, null, null, 3, Gtk.Global.CurrentEventTime);
		}

		public string GetDN (TreePath path)
		{
			TreeIter iter;
			
			if (this.dataStore.GetIter (out iter, path)) {
				string dn = (string) this.dataStore.GetValue (iter, dnColumn);
				return dn;
			} 

			return null;
		}

		public void OnNewEntryActivate (object o, EventArgs args) 
		{
			viewPlugin.OnAddEntry (conn);
			Populate ();
		}
		
		public void ShowNewItemDialog (string viewName)
		{
			viewPlugin = null;			
			
			viewPlugin = Global.Plugins.GetViewPlugin (viewName, conn.Settings.Name);  
			if (viewPlugin == null) {
				return;
			}
			
			ConfigureView (viewPlugin);
			
			viewPlugin.OnAddEntry (conn);
			Populate ();
		}

		public void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			
			if (this.Selection == null)
				return;
				
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp) {
				try {
				
					LdapEntry le = conn.Data.GetEntry (GetDN(path));
					viewPlugin.OnEditEntry (conn, le);			
					Populate ();
					
				} catch (Exception e) {
					
					Log.Debug (e);
					
					string	msg = Mono.Unix.Catalog.GetString ("Unable to edit entry");

					HIGMessageDialog dialog = new HIGMessageDialog (
						parentWindow,
						0,
						Gtk.MessageType.Error,
						Gtk.ButtonsType.Ok,
						"Error",
						msg);

					dialog.Run ();
					dialog.Destroy ();					
				}
			}
		}

		string GetSelectedAttribute (string attrName)
		{
			Gtk.TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			try {
				LdapEntry le = conn.Data.GetEntry (GetDN(tp[0]));
				LdapAttribute la = le.getAttribute (attrName);

				return la.StringValue;
			}
			catch {}

			return "";
		}

		void OnEmailActivate (object o, EventArgs args) 
		{
			string url = GetSelectedAttribute ("mail");

			if (url == null || url == "") {
				string msg = Mono.Unix.Catalog.GetString (
					"Invalid or empty email address");

				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Email error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
				
				return;
			}

			try {
			
				Gnome.Url.Show ("mailto:" + url);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to send mail to: ") + url;

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Email error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void DeleteEntry (TreePath[] path)
		{
			try {

				if (!(path.Length > 1)) {

					LdapEntry le = conn.Data.GetEntry (GetDN(path[0]));
					Util.DeleteEntry (conn, le.DN);
					return;
				}

				List<string> dnList = new List<string> ();

				foreach (TreePath tp in path) {
					LdapEntry le = conn.Data.GetEntry (GetDN(tp));
					dnList.Add (le.DN);
				}

				Util.DeleteEntry (conn, dnList.ToArray ());

			} catch {}
		}

		public void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			DeleteEntry (tp);
			
			Populate ();
		}

		void OnExportActivate (object o, EventArgs args)
		{
			TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			try {
				LdapEntry le = conn.Data.GetEntry (GetDN(tp[0]));
				Util.ExportData (conn, parentWindow, le.DN);
			}
			catch {}
		}

		void OnPwdActivate (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();
			if (pd.UserResponse == ResponseType.Cancel)
				return;
				
			if (pd.UnixPassword == null || pd.UnixPassword == "")
				return;

			TreeModel model;
			TreePath[] tp = this.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp) {
				LdapEntry le = conn.Data.GetEntry (GetDN(path));
				ChangePassword (le, pd);
			}
		}				

		void ChangePassword (LdapEntry entry, PasswordDialog pd)
		{
			List<LdapModification> mods = new List<LdapModification> ();
			
			LdapAttribute la; 
			LdapModification lm;

			la = new LdapAttribute ("userPassword", pd.UnixPassword);
			lm = new LdapModification (LdapModification.REPLACE, la);
			mods.Add (lm);

			if (Util.CheckSamba (entry)) {
				la = new LdapAttribute ("sambaLMPassword", pd.LMPassword);
				lm = new LdapModification (LdapModification.REPLACE, la);
				mods.Add (lm);

				la = new LdapAttribute ("sambaNTPassword", pd.NTPassword);
				lm = new LdapModification (LdapModification.REPLACE, la);
				mods.Add (lm);
			}

			Util.ModifyEntry (conn, entry.DN, mods.ToArray());
		}

		public void OnRefreshActivate (object o, EventArgs args)
		{
			Populate ();
		}

		[ConnectBefore]
		void OnRightClick (object o, ButtonPressEventArgs args)
		{
			// FIXME: Find a way to not deselect on multiple selection
			if (args.Event.Button == 3)
				DoPopUp ();
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (this.dataStore.GetIter (out iter, path)) {
				
				string dn = (string) this.dataStore.GetValue (iter, dnColumn);
				try {
					viewPlugin.OnEditEntry (conn, conn.Data.GetEntry (dn));
				} catch (Exception e) {
					Log.Debug (e);

					string	msg = Mono.Unix.Catalog.GetString ("Unable to edit entry");

					HIGMessageDialog dialog = new HIGMessageDialog (
						parentWindow,
						0,
						Gtk.MessageType.Error,
						Gtk.ButtonsType.Ok,
						"Error",
						msg);

					dialog.Run ();
					dialog.Destroy ();					
				}
			} 		
		}
		
		void OnWWWActivate (object o, EventArgs args) 
		{
			string url = GetSelectedAttribute ("wWWHomePage");

			try {
			
				Gnome.Url.Show (url);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to open page: ") + url;

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Network error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}
		
		void SetViewColumns ()
		{
			if (dataStore != null)
				dataStore.Clear ();

			foreach (TreeViewColumn col in this.Columns)
				this.RemoveColumn (col);		
		
			int colLength = viewPlugin.ColumnNames.Length + 1;
			System.Type[] types = new System.Type [colLength];

			for (int i = 0; i < colLength; i++)
				types[i] = typeof (string);

			dataStore = new ListStore (types);
			this.Model = dataStore;
			
			CellRenderer crt = new CellRendererText ();

			for (int i = 0; i < viewPlugin.ColumnNames.Length; i++) {

				TreeViewColumn col = new TreeViewColumn ();
				col.Title = viewPlugin.ColumnNames[i];
				col.PackStart (crt, true);
				col.AddAttribute (crt, "text", i);
				col.SortColumnId = i;

				this.AppendColumn (col);
			}

			dnColumn = viewPlugin.ColumnNames.Length;			
			TreeViewColumn c = new TreeViewColumn ();
			c.Title = "DN";			
			c.PackStart (crt, true);
			c.AddAttribute (crt, "text", dnColumn);
			c.Visible = false;
			this.AppendColumn (c);			

			this.ShowAll ();			
		}
	}
}
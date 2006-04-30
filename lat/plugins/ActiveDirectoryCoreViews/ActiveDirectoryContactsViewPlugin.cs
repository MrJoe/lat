// 
// lat - ActiveDirectoryContactsViewPlugin.cs
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

using System;
using Gtk;
using Gdk;
using Novell.Directory.Ldap;

namespace lat {

	public class ActiveDirectoryContactsViewPlugin : ViewPlugin
	{
		public ActiveDirectoryContactsViewPlugin () : base ()
		{
			config.ColumnAttributes =  new string[] { "name", "mail", "telephoneNumber", "wWWHomePage" };
			config.ColumnNames = new string[] { "Name", "Email", "Work", "Home" };
			config.Filter = "(objectClass=contact)";	
		}
	
		public override void Init ()
		{
		}

		public override void OnAddEntry (LdapServer server)
		{
//			new NewContactsViewDialog (server, this.DefaultNewContainer);
		}		

		public override void OnEditEntry (LdapServer server, LdapEntry le)
		{
//			new EditContactsViewDialog (server, le);
		}
					
		public override void OnPopupShow (Menu popup)
		{
		}
								
		public override string[] Authors 
		{
			get {
				string[] cols = { "Loren Bandiera" };
				return cols;
			}
		}
		
		public override string Copyright 
		{ 
			get { return "MMG Security, Inc."; } 
		}
		
		public override string Description 
		{ 
			get { return "Active Directory Contact View"; } 
		}
		
		public override string Name 
		{ 
			get { return "Active Directory Contacts"; } 
		}
		
		public override string Version 
		{ 
			get { return "0.1"; } 
		}
		
		public override Gdk.Pixbuf Icon 
		{
			get { return Pixbuf.LoadFromResource ("contact-new.png"); }
		}
	}
}
